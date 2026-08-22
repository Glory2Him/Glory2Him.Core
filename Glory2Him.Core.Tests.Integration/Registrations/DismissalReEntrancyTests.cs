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
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Services.Foundations.ApprovalComments;
using Glory2Him.Core.Services.Foundations.ApprovalReviews;
using Glory2Him.Core.Services.Foundations.Approvals;
using Glory2Him.Core.Services.Orchestrations.Approvals;
using Glory2Him.Core.Tests.Integration.Brokers;
using Moq;

namespace Glory2Him.Core.Tests.Integration.Registrations
{
    /// <summary>
    /// Proves the workflow does not re-test a round in the middle of tearing that round's
    /// reviews down.
    ///
    /// <para>Subscribing <c>ApprovalReview-Dismissed</c> (§10.17(a)) makes this service hear its
    /// own work: the §9.7.4 stale-review reset dismisses in a loop, each dismissal publishes to
    /// that address, and substrate delivery is synchronous. Without a guard the re-test runs
    /// INSIDE the loop — once per review, each time against a population still being torn down
    /// — and the earliest of those evaluations sees a review set that has never existed in
    /// storage as a settled state. With auto-approve on, it can approve off it.</para>
    ///
    /// <para>This needs its own orchestration instance rather than the collection fixture's:
    /// the fixture answers <c>ShouldResetStaleReviewsOnChange = false</c>, so its dismissal loop
    /// never runs and it therefore cannot tell the guard from a no-op. The fixture's REAL
    /// <see cref="EventSubstrateBroker.EventBroker"/> is reused, because the whole point is that
    /// the dismissal genuinely round-trips the substrate and comes back through the subscribed
    /// handler.</para>
    /// </summary>
    [Collection(EventSubstrateCollection.Name)]
    public sealed class DismissalReEntrancyTests
    {
        private readonly EventSubstrateBroker broker;

        public DismissalReEntrancyTests(EventSubstrateBroker broker) =>
            this.broker = broker;

        [Fact]
        public async Task ShouldNotReTestTheRoundWhileDismissingItsOwnStaleReviewsAsync()
        {
            // given: a submitted round with three stale reviews, and a policy that resets them
            // on change. Each dismissal publishes ApprovalReview-Dismissed through the REAL
            // broker, so the subscribed handler is genuinely re-entered mid-loop.
            Guid approvalId = Guid.NewGuid();
            var reTestedApprovalIds = new List<Guid>();

            ApprovalOrchestrationService orchestration = BuildOrchestrationThatPublishesDismissals(
                approvalId: approvalId,
                staleReviewCount: 3,
                shouldResetStaleReviewsOnChange: true,
                reTestedApprovalIds: reTestedApprovalIds);

            await RegisterDismissalHandlerAsync(orchestration, "OwnRound");

            // when: the owner's edit drives the reset
            await orchestration.ProcessEntityModifiedAsync(
                entityType: EntityType.Tag,
                entityId: Guid.NewGuid(),
                cancellationToken: CancellationToken.None);

            // then
            reTestedApprovalIds.Should().NotContain(approvalId,
                because: "the flow that dismisses the round already re-evaluates it once, at the " +
                    "end. A re-test fired by the loop's own dismissals would run against a " +
                    "half-dismissed review set and could auto-approve off a population still " +
                    "being torn down — §9.7.4 exactly inverted");
        }

        [Fact]
        public async Task ShouldStillReTestADifferentRoundWhileDismissingAsync()
        {
            // given: the guard is keyed on the approval being dismissed, so an unrelated round's
            // dismissal must still be heard while our loop runs. A guard that stood down for
            // everything would drop those on the floor.
            //
            // This publishes from OUTSIDE any suppression window, which no production caller
            // does any more (#295 removed the human route, and the reset loop always sets the
            // window before publishing). That is deliberate rather than stale: the property
            // under test is that the guard is SCOPED, not global, and the only way to observe
            // scoping is to arrive the way a second publisher one day would.
            Guid dismissingApprovalId = Guid.NewGuid();
            Guid unrelatedApprovalId = Guid.NewGuid();
            var reTestedApprovalIds = new List<Guid>();

            ApprovalOrchestrationService orchestration = BuildOrchestrationThatPublishesDismissals(
                approvalId: dismissingApprovalId,
                staleReviewCount: 1,
                shouldResetStaleReviewsOnChange: true,
                reTestedApprovalIds: reTestedApprovalIds,
                alsoPublishForApprovalId: unrelatedApprovalId);

            await RegisterDismissalHandlerAsync(orchestration, "OtherRound");

            // when
            await orchestration.ProcessEntityModifiedAsync(
                entityType: EntityType.Tag,
                entityId: Guid.NewGuid(),
                cancellationToken: CancellationToken.None);

            // then
            reTestedApprovalIds.Should().Contain(unrelatedApprovalId,
                because: "suppression is scoped to the round being dismissed, not to the " +
                    "handler — another round's dismissal is somebody else's act and still " +
                    "moves that round's §8.5 count");
        }

        [Fact]
        public async Task ShouldStillRefuseAnUnverifiableFactWhileSuppressedAsync()
        {
            // given: suppression must never become a way to skip verification, which is why the
            // check sits AFTER ValidateEntityFactEnvelopeAsync. An envelope signed under a name
            // this address does not carry must be refused whether or not we would have acted.
            Guid approvalId = Guid.NewGuid();

            ApprovalOrchestrationService orchestration = BuildOrchestrationThatPublishesDismissals(
                approvalId: approvalId,
                staleReviewCount: 0,
                shouldResetStaleReviewsOnChange: false,
                reTestedApprovalIds: new List<Guid>());

            // A genuine envelope for the WRONG address: signed as an add, offered to the
            // dismissal handler.
            EventEnvelope<ApprovalReview> misNamedEnvelope =
                await SignedAsAsync(approvalId, ApprovalReviewEventOperation.Added);

            // when
            Func<Task> handling = async () =>
                await orchestration.OnApprovalReviewDismissedAsync(
                    misNamedEnvelope, CancellationToken.None);

            // then
            await handling.Should().ThrowAsync<Exception>(
                because: "the dismissal handler accepts only the names its own address carries " +
                    "— the event name is inside the HMAC, so an envelope signed for another " +
                    "address must be refused rather than acted on");
        }

        private async Task<EventEnvelope<ApprovalReview>> SignedAsAsync(
            Guid approvalId,
            ApprovalReviewEventOperation operation)
        {
            EventEnvelope<ApprovalReview> captured = null;

            await this.broker.EventBroker.SubscribeToApprovalReviewEventAsync(
                subscription: new EventSubscription
                {
                    Id = Guid.NewGuid(),
                    Name = $"IntegrationProbe.CaptureSignedApprovalReviewFact.{Guid.NewGuid():N}",
                    Description = "Captures a signed envelope as the substrate delivers it."
                },
                operation: operation,
                approvalReviewEventHandler: (EventEnvelope<ApprovalReview> envelope,
                    CancellationToken _) =>
                {
                    captured = envelope;

                    return ValueTask.CompletedTask;
                },
                cancellationToken: CancellationToken.None);

            await this.broker.EventBroker.PublishApprovalReviewAsync(
                new EventEnvelope<ApprovalReview>
                {
                    Content = new ApprovalReview
                    {
                        Id = Guid.NewGuid(),
                        ApprovalId = approvalId
                    }
                },
                operation);

            return captured;
        }

        // A DISTINCT id per test. Registration is retrieve-or-register by id, so a shared one
        // would bind whichever orchestration registered first and silently ignore every later
        // test's handler — the second test would then measure the first test's instance.
        private async Task RegisterDismissalHandlerAsync(
            ApprovalOrchestrationService orchestration,
            string probeName) =>
                await this.broker.EventBroker.SubscribeToApprovalReviewEventAsync(
                    subscription: new EventSubscription
                    {
                        Id = Guid.NewGuid(),
                        Name = $"IntegrationProbe.OnApprovalReviewDismissed.{probeName}",

                        Description = "Drives the dismissal handler under test through the " +
                            "real substrate."
                    },
                    operation: ApprovalReviewEventOperation.Dismissed,
                    approvalReviewEventHandler: orchestration.OnApprovalReviewDismissedAsync,
                    cancellationToken: CancellationToken.None);

        private ApprovalOrchestrationService BuildOrchestrationThatPublishesDismissals(
            Guid approvalId,
            int staleReviewCount,
            bool shouldResetStaleReviewsOnChange,
            List<Guid> reTestedApprovalIds,
            Guid? alsoPublishForApprovalId = null)
        {
            var approvalServiceMock = new Mock<IApprovalService>();

            approvalServiceMock
                .Setup(service => service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ApprovalEntityMatch)null);

            approvalServiceMock
                .Setup(service => service.AddApprovalAsync(
                    It.IsAny<Approval>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SubmittedApproval(approvalId));

            // The observation point. Every re-test reads the round by id, so recording what was
            // read is what distinguishes a suppressed handler from one that fired.
            approvalServiceMock
                .Setup(service => service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid readApprovalId, CancellationToken _) =>
                {
                    reTestedApprovalIds.Add(readApprovalId);

                    return SubmittedApproval(readApprovalId);
                });

            var accessBrokerMock = new Mock<IAccessBroker>();
            var approvalReviewServiceMock = new Mock<IApprovalReviewWorkflowService>();

            List<ApprovalReview> staleReviews = Enumerable.Range(0, staleReviewCount)
                .Select(_ => new ApprovalReview
                {
                    Id = Guid.NewGuid(),
                    ApprovalId = approvalId,
                    IsDeleted = false,
                    StatusId = ApprovalStatus.Approved
                })
                .ToList();

            // The flow reads the round's reviews through the unfiltered gather, not the
            // caller-facing read — what a round holds is a fact about storage, not about who
            // is asking.
            accessBrokerMock
                .Setup(broker => broker.FindDismissableApprovalReviewIdsAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(staleReviews.Select(review => review.Id).ToList());

            // THE POINT OF THIS FIXTURE: the dismissal actually publishes, so the subscribed
            // handler is re-entered from inside the loop exactly as it would be in a host.
            approvalReviewServiceMock
                .Setup(service => service.DismissStaleApprovalReviewAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(async (Guid reviewId, CancellationToken token) =>
                {
                    await this.broker.EventBroker.PublishApprovalReviewAsync(
                        new EventEnvelope<ApprovalReview>
                        {
                            Content = new ApprovalReview
                            {
                                Id = reviewId,
                                ApprovalId = approvalId
                            }
                        },
                        ApprovalReviewEventOperation.Dismissed);

                    if (alsoPublishForApprovalId is Guid unrelatedApprovalId)
                    {
                        await this.broker.EventBroker.PublishApprovalReviewAsync(
                            new EventEnvelope<ApprovalReview>
                            {
                                Content = new ApprovalReview
                                {
                                    Id = Guid.NewGuid(),
                                    ApprovalId = unrelatedApprovalId
                                }
                            },
                            ApprovalReviewEventOperation.Dismissed);
                    }

                    return new ApprovalReview { Id = reviewId, ApprovalId = approvalId };
                });

            accessBrokerMock
                .Setup(accessBroker => accessBroker.EvaluateApprovalConditionsByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ApprovalConditionsVerdict
                {
                    AreConditionsMet = false,
                    ShouldResetStaleReviewsOnChange = shouldResetStaleReviewsOnChange,
                    ShouldAutoApprove = false,
                    BlockReason = AccessDenialReason.None,
                    BlockReasons = new List<AccessDenialReason>(),
                    UnresolvedApprovalCommentCount = 0,
                    ApprovalCount = 0,
                    RequiredNumberOfApprovals = 1,
                    Explanation = "Re-entrancy probe: nothing to decide."
                });

            return new ApprovalOrchestrationService(
                approvalService: approvalServiceMock.Object,
                approvalReviewWorkflowService: approvalReviewServiceMock.Object,
                approvalCommentService: new Mock<IApprovalCommentService>().Object,
                accessBroker: accessBrokerMock.Object,
                eventEnvelopeBroker: new Mock<IEventEnvelopeBroker>().Object,
                eventBroker: this.broker.EventBroker,
                envelopeIntegrityBroker: this.broker.EnvelopeIntegrityBroker,
                loggingBroker: new Mock<ILoggingBroker>().Object);
        }

        private static Approval SubmittedApproval(Guid approvalId) =>
            new Approval
            {
                Id = approvalId,
                EntityType = EntityType.Tag,
                EntityId = Guid.NewGuid(),
                ApprovalStatus = ApprovalStatus.Submitted
            };
    }
}
