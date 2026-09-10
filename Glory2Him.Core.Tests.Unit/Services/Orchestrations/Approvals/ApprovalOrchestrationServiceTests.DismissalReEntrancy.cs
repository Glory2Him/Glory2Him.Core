// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    /// <summary>
    /// The §9.7.4 reset dismisses a round's reviews in a loop, each dismissal publishes
    /// <c>ApprovalReview-Dismissed</c>, and this service subscribes to that address
    /// (§10.17(a)) — so the workflow hears its own work. Without a guard the re-test runs
    /// INSIDE the loop, once per review, each time against a population still being torn
    /// down, and the earliest of those evaluations sees a review set that has never existed
    /// in storage as a settled state. With auto-approve on, it can approve off it.
    ///
    /// <para><b>Why these are unit tests.</b> The only substrate property the guard leans on
    /// is SYNCHRONOUS, SAME-EXECUTION-CONTEXT delivery — that is what lets the
    /// <c>AsyncLocal</c> window flow from the dismissal loop into the re-entered handler. That
    /// property is pinned against the real substrate by
    /// <c>ExecutionContextFlowTests</c> and <c>HandlerFailureContainmentTests</c>, and the
    /// signed round trip of a dismissal fact by <c>WorkflowRecordFactTests</c>' Dismissed row.
    /// With those three standing, re-entering the handler directly from the dismissal seam is
    /// the same execution context the substrate would deliver on, and nothing here is
    /// pretending to prove wiring.</para>
    ///
    /// <para><b>The window is never set by hand.</b> <c>suppressedDismissalApprovalId</c> is
    /// private, static and written only inside <c>DismissStaleApprovalReviewsAsync</c>, so
    /// every test below drives <c>ProcessEntityModifiedAsync</c> and re-enters the handler
    /// from the dismissal seam. "Open the window, then call the handler" cannot be written
    /// honestly, and is not written here.</para>
    /// </summary>
    public partial class ApprovalOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldNotReTestTheRoundWhileDismissingItsOwnStaleReviewsAsync()
        {
            // given: a submitted round with three stale reviews and a policy that resets them
            // on change. Every dismissal re-enters the subscribed handler from inside the loop,
            // exactly as a synchronous delivery does in a host.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var reTestedApprovalIds = new List<Guid>();
            int handlerReEntryCount = 0;

            SetupReEntrancyRound(approvalId, entityId);
            SetupReEntrancyReTestObservation(reTestedApprovalIds);
            SetupConditions(CreateFlowConditions(shouldResetStaleReviewsOnChange: true));

            List<Guid> dismissedReviewIds = SetupDismissalThatReEntersTheHandler(
                approvalId: approvalId,
                staleReviewCount: 3,
                onDismissedAsync: async _ =>
                {
                    handlerReEntryCount++;

                    await this.approvalOrchestrationService.OnApprovalReviewDismissedAsync(
                        envelope: CreateSubstrateEnvelope(
                            new ApprovalReview
                            {
                                Id = Guid.NewGuid(),
                                ApprovalId = approvalId,
                            }),
                        cancellationToken: TestContext.Current.CancellationToken);
                });

            // when: the owner's edit drives the reset
            await this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                EntityType.Tag,
                entityId,
                TestContext.Current.CancellationToken);

            // then: the loop ran and the handler was genuinely re-entered — asserted first,
            // because a test where nothing was delivered would satisfy every line below it
            // while proving nothing at all.
            dismissedReviewIds.Should().HaveCount(3);
            handlerReEntryCount.Should().Be(3);

            reTestedApprovalIds.Should().NotContain(approvalId,
                because: "the flow that dismisses the round already re-evaluates it once, at " +
                    "the end. A re-test fired by the loop's own dismissals would run against a " +
                    "half-dismissed review set and could auto-approve off a population still " +
                    "being torn down — §9.7.4 exactly inverted");

            // Nothing re-tested AT ALL, which is stronger than the line above: the round's own
            // read is the only one this fixture can produce, so an empty list is the guard
            // standing down for every one of the three deliveries.
            reTestedApprovalIds.Should().BeEmpty();
        }

        [Fact]
        public async Task ShouldStillReTestADifferentRoundWhileDismissingAsync()
        {
            // given: the guard is keyed on the approval being dismissed, so an unrelated
            // round's dismissal must still be heard while our loop runs. A guard that stood
            // down for everything would drop those on the floor.
            //
            // The unrelated fact arrives from INSIDE our suppression window, which is the only
            // place a second publisher could arrive from — the window is open for the whole of
            // the loop. That is the point: suppression is SCOPED to one round, not to the
            // handler.
            var dismissingApprovalId = Guid.NewGuid();
            var unrelatedApprovalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var reTestedApprovalIds = new List<Guid>();

            SetupReEntrancyRound(dismissingApprovalId, entityId);
            SetupReEntrancyReTestObservation(reTestedApprovalIds);
            SetupConditions(CreateFlowConditions(shouldResetStaleReviewsOnChange: true));

            List<Guid> dismissedReviewIds = SetupDismissalThatReEntersTheHandler(
                approvalId: dismissingApprovalId,
                staleReviewCount: 1,
                onDismissedAsync: async _ =>
                {
                    await this.approvalOrchestrationService.OnApprovalReviewDismissedAsync(
                        envelope: CreateSubstrateEnvelope(
                            new ApprovalReview
                            {
                                Id = Guid.NewGuid(),
                                ApprovalId = dismissingApprovalId,
                            }),
                        cancellationToken: TestContext.Current.CancellationToken);

                    await this.approvalOrchestrationService.OnApprovalReviewDismissedAsync(
                        envelope: CreateSubstrateEnvelope(
                            new ApprovalReview
                            {
                                Id = Guid.NewGuid(),
                                ApprovalId = unrelatedApprovalId,
                            }),
                        cancellationToken: TestContext.Current.CancellationToken);
                });

            // when
            await this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                EntityType.Tag,
                entityId,
                TestContext.Current.CancellationToken);

            // then
            dismissedReviewIds.Should().HaveCount(1);

            reTestedApprovalIds.Should().Equal(new[] { unrelatedApprovalId },
                because: "suppression is scoped to the round being dismissed, not to the " +
                    "handler — another round's dismissal is somebody else's act and still " +
                    "moves that round's §8.5 count, while our own stays suppressed");
        }

        [Fact]
        public async Task ShouldStillRefuseAnUnverifiableDismissalWhileSuppressedAsync()
        {
            // given: suppression must never become a way to skip verification, which is why the
            // check sits AFTER ValidateEntityFactEnvelopeAsync. This drives the SUPPRESSED
            // round, so the window is open and the handler would have stood down — and the
            // envelope is refused anyway.
            //
            // The refusal is caught INSIDE the dismissal callback rather than allowed to
            // propagate, because that is what production does with it: the substrate contains a
            // throwing handler as a failed delivery (HandlerFailureContainmentTests) instead of
            // faulting the publisher. Letting it escape here would assert on TryCatch's mapping
            // of an orchestration exception, which is a different property and a weaker one.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var reTestedApprovalIds = new List<Guid>();
            var containedFailures = new List<Exception>();

            SetupReEntrancyRound(approvalId, entityId);
            SetupReEntrancyReTestObservation(reTestedApprovalIds);
            SetupConditions(CreateFlowConditions(shouldResetStaleReviewsOnChange: true));

            // Overrides the fixture's verifying default — every other test needs it true, and
            // this one is about what happens when it is not.
            SetupSubstrateFailingVerification();

            var expectedInvalidException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval event is invalid. Integrity verification failed.");

            List<Guid> dismissedReviewIds = SetupDismissalThatReEntersTheHandler(
                approvalId: approvalId,
                staleReviewCount: 1,
                onDismissedAsync: async _ =>
                {
                    try
                    {
                        await this.approvalOrchestrationService
                            .OnApprovalReviewDismissedAsync(
                                envelope: CreateSubstrateEnvelope(
                                    new ApprovalReview
                                    {
                                        Id = Guid.NewGuid(),
                                        ApprovalId = approvalId,
                                    }),
                                cancellationToken: TestContext.Current.CancellationToken);
                    }
                    catch (Exception deliveryException)
                    {
                        containedFailures.Add(deliveryException);
                    }
                });

            // when: the loop completes, the way it does when the substrate contains a failed
            // delivery
            await this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                EntityType.Tag,
                entityId,
                TestContext.Current.CancellationToken);

            // then
            dismissedReviewIds.Should().HaveCount(1);

            containedFailures.Should().ContainSingle(
                because: "the one delivery inside the window was refused, and the loop carried " +
                    "on rather than being faulted by it");

            InvalidApprovalOrchestrationException actualException =
                Assert.IsType<InvalidApprovalOrchestrationException>(containedFailures[0]);

            actualException.Should().BeEquivalentTo(expectedInvalidException,
                because: "the dismissal handler accepts only the names its own address carries " +
                    "— the event name is inside the HMAC, so an envelope signed for another " +
                    "address must be refused rather than acted on, suppressed or not");

            // The verification ran, which is what places the guard AFTER it: a handler that
            // tested suppression first would have returned before asking.
            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<It.IsAnyType>>(),
                    "ApprovalReviewDismissed",
                    EnvelopeDirection.Request),
                Times.Once);

            reTestedApprovalIds.Should().BeEmpty(
                because: "a refused envelope decides nothing either way — it is neither acted " +
                    "on nor quietly accepted as a suppressed delivery");
        }

        // The round the edit lands on, resolved through the ADD path: FindApprovalByEntityAsync
        // answers null, so ResolveApprovalAsync opens a row rather than reading one. That keeps
        // RetrieveApprovalByIdAsync free to be the observation point below — on the match path
        // the flow reads it itself, and a test could not tell that read from a re-test.
        private void SetupReEntrancyRound(Guid approvalId, Guid entityId)
        {
            SetupApprovalProbe(approvalMatch: null);

            this.approvalServiceMock.Setup(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new Approval
                        {
                            Id = approvalId,
                            EntityType = EntityType.Tag,
                            EntityId = entityId,
                            ApprovalStatus = ApprovalStatus.Submitted,
                        });
        }

        // THE OBSERVATION POINT. Every re-test reads its round by id, so recording what was
        // read is what distinguishes a suppressed handler from one that fired. Answered at
        // Submitted because that is the only status the re-test evaluates — a Draft short
        // circuits before the conditions are read, and a suppressed handler and a short
        // circuited one would then look alike.
        private void SetupReEntrancyReTestObservation(List<Guid> reTestedApprovalIds) =>
            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Guid readApprovalId, CancellationToken _) =>
                        {
                            reTestedApprovalIds.Add(readApprovalId);

                            return new Approval
                            {
                                Id = readApprovalId,
                                EntityType = EntityType.Tag,
                                EntityId = Guid.NewGuid(),
                                ApprovalStatus = ApprovalStatus.Submitted,
                            };
                        });

        // The dismissal seam re-enters the subscribed handler, which is what a synchronous
        // delivery does — and the suppression window is open around it because the real loop
        // opened it.
        //
        // Deliberately NOT the Flows fixture's onReviewDismissed hook: that one is a
        // synchronous Action, and re-entering the handler is awaited work. A hook that could
        // not be awaited would either run the delivery outside the loop's own await chain or
        // swallow its failures.
        private List<Guid> SetupDismissalThatReEntersTheHandler(
            Guid approvalId,
            int staleReviewCount,
            Func<Guid, ValueTask> onDismissedAsync)
        {
            var dismissedReviewIds = new List<Guid>();

            List<Guid> staleReviewIds = Enumerable.Range(0, staleReviewCount)
                .Select(_ => Guid.NewGuid())
                .ToList();

            this.accessBrokerMock.Setup(broker =>
                broker.FindDismissableApprovalReviewIdsAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(staleReviewIds);

            this.approvalReviewServiceMock.Setup(service =>
                service.DismissStaleApprovalReviewAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .Returns((Guid approvalReviewId, CancellationToken _) =>
                            DismissAndDeliverAsync(
                                approvalReviewId: approvalReviewId,
                                approvalId: approvalId,
                                dismissedReviewIds: dismissedReviewIds,
                                onDismissedAsync: onDismissedAsync));

            return dismissedReviewIds;
        }

        private static async ValueTask<ApprovalReview> DismissAndDeliverAsync(
            Guid approvalReviewId,
            Guid approvalId,
            List<Guid> dismissedReviewIds,
            Func<Guid, ValueTask> onDismissedAsync)
        {
            dismissedReviewIds.Add(approvalReviewId);
            await onDismissedAsync(approvalReviewId);

            return new ApprovalReview
            {
                Id = approvalReviewId,
                ApprovalId = approvalId,
            };
        }
    }
}
