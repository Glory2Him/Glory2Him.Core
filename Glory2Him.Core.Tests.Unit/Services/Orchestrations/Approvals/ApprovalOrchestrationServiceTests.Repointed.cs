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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    // THE ASSOCIATION-REPOINTED EAR (§ARC16.2.2, #727). A reader changing their reaction is the one
    // in-place change a decided single-row round admits (§APR7.5.1 rule 3), and it goes back
    // through the same approval process as any modification, in a round that starts with no
    // reviews (§DOM4.5 rule 4). The ear runs the Modified flow and hands it one thing every other
    // ear does not: the change's UpdatedWhen, read off the verified fact (§APR9.7.4).
    public partial class ApprovalOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldRunTheModifiedFlowWithTheChangesTimeOnRepointedAsync()
        {
            // given: a reader's changed reaction on an OPEN round, so nothing is returned and what
            // this test reads is the bound alone. RequireReapprovalOnChange is ON, so an ear that
            // handed the flow no bound would dismiss every active review, as every other ear's
            // flow does — the new pair's review included.
            //
            // The two reviews sit a minute either side of the change's UpdatedWhen, and the fact's
            // CreatedWhen is a day earlier than both: the old pair's review is dismissed alone only
            // when the flow is bounded by the UpdatedWhen on the fact's content and nothing else.
            var entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            var oldPairReviewId = Guid.NewGuid();
            var newPairReviewId = Guid.NewGuid();
            DateTimeOffset changedWhen = RepointedChangeTime;
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Association,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupFlowApprovalRow(storageApproval);
            SetupConditions(CreateFlowConditions(shouldResetStaleReviewsOnChange: true));

            List<Guid> dismissedReviewIds = SetupRepointedReviews(
                approvalId,
                cancellationToken,
                CreateRepointedReview(oldPairReviewId, createdWhen: changedWhen.AddMinutes(-1)),
                CreateRepointedReview(newPairReviewId, createdWhen: changedWhen.AddMinutes(1)));

            EventEnvelope<Association> inputEnvelope =
                CreateRepointedEnvelope(CreateRepointedAssociation(entityId, changedWhen));

            // when
            EventEnvelope<Association> actualReply =
                await this.approvalOrchestrationService.OnAssociationRepointedAsync(
                    envelope: inputEnvelope,
                    cancellationToken: cancellationToken);

            // then: THIS envelope was verified, as the fact this ear serves, and as a request
            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inputEnvelope,
                    "AssociationRepointed",
                    EnvelopeDirection.Request),
                Times.Once);

            // the Modified flow ran for THIS association, under the type the ear names rather
            // than one read off the payload, and on the delivery's own token: a flow handed any
            // other could not be cancelled with the delivery it is serving
            this.approvalServiceMock.Verify(service =>
                service.FindApprovalByEntityAsync(
                    EntityType.Association,
                    entityId,
                    cancellationToken),
                Times.Once);

            // and bounded by the change's time: the round's reviews were read with when each was
            // written, and only the one written before the change was dismissed
            this.accessBrokerMock.Verify(broker =>
                broker.FindDismissableApprovalReviewsAsync(
                    approvalId,
                    cancellationToken),
                Times.Once);

            dismissedReviewIds.Should().Equal(new[] { oldPairReviewId });

            // never through the ids-only read the other ears' dismissal makes
            this.accessBrokerMock.Verify(broker =>
                broker.FindDismissableApprovalReviewIdsAsync(
                    approvalId,
                    cancellationToken),
                Times.Never);

            // A fact is a notification, so nothing is replied with.
            actualReply.Should().BeNull();
        }

        [Fact]
        public async Task ShouldRefuseATamperedRepointedEnvelopeAsync()
        {
            // given: an Association-Repointed envelope whose signature does not verify. The
            // change's time is read off the fact's content, and a caller who could put a time
            // there would choose which reviews a changed reaction dismisses, so an unverifiable
            // envelope is refused before any round is read (§APR9.7.4, §SEC14.6 rule 4).
            //
            // Everything downstream is armed as if the fact were genuine — a round decided before
            // the change, and an old pair's review standing — so an ear that acted before
            // verifying would return the round and dismiss the review.
            var entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            DateTimeOffset changedWhen = RepointedChangeTime;
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Association,
                approvalStatus: ApprovalStatus.Approved);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Approved, approvalId));
            SetupFlowApprovalRow(storageApproval);
            SetupConditions(CreateFlowConditions(areConditionsMet: true, shouldAutoApprove: true));

            SetupRepointedReviews(
                approvalId,
                cancellationToken,
                CreateRepointedReview(Guid.NewGuid(), createdWhen: changedWhen.AddDays(-1)));

            SetupSubstrateFailingVerification();

            EventEnvelope<Association> inputEnvelope =
                CreateRepointedEnvelope(CreateRepointedAssociation(entityId, changedWhen));

            var expectedInvalidException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval event is invalid. Integrity verification failed.");

            // when
            ValueTask<EventEnvelope<Association>> repointedTask =
                this.approvalOrchestrationService.OnAssociationRepointedAsync(
                    envelope: inputEnvelope,
                    cancellationToken: cancellationToken);

            InvalidApprovalOrchestrationException actualException =
                await Assert.ThrowsAsync<InvalidApprovalOrchestrationException>(
                    repointedTask.AsTask);

            // then: refused, after asking only whether THIS envelope's signature holds
            actualException.Should().BeEquivalentTo(expectedInvalidException);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inputEnvelope,
                    "AssociationRepointed",
                    EnvelopeDirection.Request),
                Times.Once);

            // no round was read, so nothing was returned, dismissed, evaluated or announced
            this.approvalServiceMock.Verify(service =>
                service.FindApprovalByEntityAsync(
                    EntityType.Association,
                    entityId,
                    cancellationToken),
                Times.Never);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldDropARepointedFactCarryingTheSystemIdentityAsync()
        {
            // given: a genuine Association-Repointed fact that carries the SYSTEM identity — one
            // this service's own work caused. Every entity fact this service hears describes
            // something a person did, and re-entering the flow on the workflow's own write is a
            // loop rather than a reaction, so every ear drops it once it has verified it.
            //
            // Armed as on a reader's change — a round decided before the change, an old pair's
            // review standing — so an ear that did not drop it would return and dismiss.
            var entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            DateTimeOffset changedWhen = RepointedChangeTime;
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Association,
                approvalStatus: ApprovalStatus.Approved);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Approved, approvalId));
            SetupFlowApprovalRow(storageApproval);
            SetupConditions(CreateFlowConditions(areConditionsMet: true, shouldAutoApprove: true));

            SetupRepointedReviews(
                approvalId,
                cancellationToken,
                CreateRepointedReview(Guid.NewGuid(), createdWhen: changedWhen.AddDays(-1)));

            var systemContext = new SecurityContext
            {
                SubjectId = SystemIdentity.UserId,
                IsAuthenticated = true,
                IsSystemIdentity = true,
            };

            EventEnvelope<Association> inputEnvelope =
                CreateRepointedEnvelope(
                    CreateRepointedAssociation(entityId, changedWhen),
                    securityContext: systemContext);

            // when
            EventEnvelope<Association> actualReply =
                await this.approvalOrchestrationService.OnAssociationRepointedAsync(
                    envelope: inputEnvelope,
                    cancellationToken: cancellationToken);

            // then: verified first — the flag is believed only on a verified envelope — and then
            // dropped, with nothing replied
            actualReply.Should().BeNull();

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inputEnvelope,
                    "AssociationRepointed",
                    EnvelopeDirection.Request),
                Times.Once);

            // no round was read, so nothing was returned, dismissed, evaluated or announced
            this.approvalServiceMock.Verify(service =>
                service.FindApprovalByEntityAsync(
                    EntityType.Association,
                    entityId,
                    cancellationToken),
                Times.Never);

            this.approvalServiceMock.VerifyNoOtherCalls();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRefuseARepointedEnvelopeWithNoContentAsync()
        {
            // given: an Association-Repointed envelope carrying no content. The content is where
            // the association's identity AND the change's time come from, so there is no round it
            // could be about and no bound it could hand the flow. It is refused as every ear
            // refuses one — before the signature is asked about, since there is nothing signed to
            // read — and never dereferenced for the time.
            var expectedInvalidException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval is invalid, fix the errors and try again.");

            // when
            ValueTask<EventEnvelope<Association>> repointedTask =
                this.approvalOrchestrationService.OnAssociationRepointedAsync(
                    envelope: CreateRepointedEnvelope(content: null),
                    cancellationToken: TestContext.Current.CancellationToken);

            InvalidApprovalOrchestrationException actualException =
                await Assert.ThrowsAsync<InvalidApprovalOrchestrationException>(
                    repointedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedInvalidException);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldRethrowAFailureOfTheFlowOnRepointedAndLogItAsync(
            Xeption foundationException)
        {
            // given: the round was decided before the change, so the flow's first write is its
            // return to Submitted, and the Approval foundation fails it. A failure beneath an ear
            // is mapped by the flow's own chain, logged, and rethrown OUT of the ear, as every
            // ear's is: the substrate then records the delivery as failed and redelivers it. An
            // ear that swallowed it would leave the change unprocessed, with nothing to retry it.
            var entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            DateTimeOffset changedWhen = RepointedChangeTime;

            Approval storageApproval = CreateRepointedRound(
                approvalId: approvalId,
                entityId: entityId,
                approvalStatus: ApprovalStatus.Approved,
                updatedWhen: changedWhen.AddDays(-1),
                updatedBy: SystemIdentity.UserId);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Approved, approvalId));
            SetupSubstrateApprovalRow(storageApproval);

            SetupRepointedReviews(
                approvalId,
                TestContext.Current.CancellationToken,
                CreateRepointedReview(Guid.NewGuid(), createdWhen: changedWhen.AddDays(-1)));

            this.approvalServiceMock.Setup(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            var expectedDependencyException =
                new ApprovalOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

            // when
            ValueTask<EventEnvelope<Association>> repointedTask =
                this.approvalOrchestrationService.OnAssociationRepointedAsync(
                    envelope: CreateRepointedEnvelope(
                        CreateRepointedAssociation(entityId, changedWhen)),
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    repointedTask.AsTask);

            // then: mapped to this service's dependency category, logged once, and rethrown
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            // and the flow stopped where it failed: the association was not told, and no review
            // was dismissed
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRethrowADependencyTimeoutOnRepointedAndLogItAsync()
        {
            // given: an open round, so the first read the changed reaction makes of its own is the
            // round's reviews with when each was written — and that read is cancelled although
            // the caller never asked. The exception's own token was never cancelled, so it is a
            // dependency that gave up, i.e. a timeout: this service's dependency exception,
            // wrapping the timeout, is logged and rethrown, and the substrate retries the delivery.
            var entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            DateTimeOffset changedWhen = RepointedChangeTime;

            Approval storageApproval = CreateRepointedRound(
                approvalId: approvalId,
                entityId: entityId,
                approvalStatus: ApprovalStatus.Submitted,
                updatedWhen: changedWhen.AddDays(-1),
                updatedBy: SystemIdentity.UserId);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupSubstrateApprovalRow(storageApproval);
            SetupConditions(CreateFlowConditions());

            // The default constructor leaves CancellationToken at None, whose
            // IsCancellationRequested is false — the timeout half of the filter.
            var operationCanceledException = new OperationCanceledException();

            this.accessBrokerMock.Setup(broker =>
                broker.FindDismissableApprovalReviewsAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutApprovalOrchestrationException =
                new TimeoutApprovalOrchestrationException(
                    message: "Failed content item association orchestration timeout error occurred, " +
                        "contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedDependencyException =
                new ApprovalOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: timeoutApprovalOrchestrationException);

            // when: a live token is handed in, so only the thrown exception's own token can decide
            // the branch
            ValueTask<EventEnvelope<Association>> repointedTask =
                this.approvalOrchestrationService.OnAssociationRepointedAsync(
                    envelope: CreateRepointedEnvelope(
                        CreateRepointedAssociation(entityId, changedWhen)),
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    repointedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            // nothing was dismissed, written or announced past the read that gave up
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // The change's own moment, pinned rather than drawn. Every review and every round in these
        // tests is placed against it, and a drawn time can land in year 0001, where subtracting
        // from it throws.
        private static readonly DateTimeOffset RepointedChangeTime =
            new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

        private static Association CreateRepointedAssociation(
            Guid entityId,
            DateTimeOffset changedWhen) =>
            new Association
            {
                Id = entityId,
                UpdatedWhen = changedWhen,

                // A day before every review in these tests, so an ear that read the wrong audit
                // field would bound the flow by it and dismiss nothing.
                CreatedWhen = changedWhen.AddDays(-1),
            };

        // The reader's own context unless a test says otherwise.
        private static EventEnvelope<Association> CreateRepointedEnvelope(
            Association content,
            SecurityContext securityContext = null) =>
            new EventEnvelope<Association>
            {
                Content = content,
                SecurityContext = securityContext ?? CreateRepointedReaderContext(),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() },
            };

        // The reader who changed their reaction: signed in, holding no review role, and not the
        // workflow, whose own facts every ear drops.
        private static SecurityContext CreateRepointedReaderContext() =>
            new SecurityContext
            {
                SubjectId = Guid.NewGuid().ToString(),
                IsAuthenticated = true,
                IsSystemIdentity = false,
            };

        private static DismissableApprovalReview CreateRepointedReview(
            Guid approvalReviewId,
            DateTimeOffset createdWhen,
            bool isRejection = false) =>
            new DismissableApprovalReview
            {
                Id = approvalReviewId,
                CreatedWhen = createdWhen,
                IsRejection = isRejection,
            };

        private List<Guid> SetupRepointedReviews(
            Guid approvalId,
            CancellationToken cancellationToken,
            params DismissableApprovalReview[] approvalReviews) =>
            SetupRepointedReviews(approvalId, cancellationToken, flowSteps: null, approvalReviews);

        // The round's active reviews as the access broker gathers them, unfiltered (§APR9.7.4).
        // The ids-only read answers the same set, so a flow that ignored the bound and dismissed
        // through it is caught by what it dismissed rather than by an unstubbed call. Every
        // dismissal is captured, in order, and recorded among the flow's steps when asked.
        //
        // Each is answered only on the delivery's own token. The dismissed id is taken whatever
        // it is, because every test asserts the ids it captured.
        private List<Guid> SetupRepointedReviews(
            Guid approvalId,
            CancellationToken cancellationToken,
            List<string> flowSteps,
            params DismissableApprovalReview[] approvalReviews)
        {
            var dismissedReviewIds = new List<Guid>();

            IReadOnlyList<DismissableApprovalReview> activeReviews = approvalReviews.ToList();

            this.accessBrokerMock.Setup(broker =>
                broker.FindDismissableApprovalReviewsAsync(
                    approvalId,
                    cancellationToken))
                        .ReturnsAsync(activeReviews);

            this.accessBrokerMock.Setup(broker =>
                broker.FindDismissableApprovalReviewIdsAsync(
                    approvalId,
                    cancellationToken))
                        .ReturnsAsync(activeReviews.Select(review => review.Id).ToList());

            this.approvalReviewServiceMock.Setup(service =>
                service.DismissStaleApprovalReviewAsync(
                    It.IsAny<Guid>(),
                    cancellationToken))
                        .Returns((Guid approvalReviewId, CancellationToken cancellationToken) =>
                        {
                            dismissedReviewIds.Add(approvalReviewId);
                            flowSteps?.Add("dismiss");

                            return new ValueTask<ApprovalReview>(
                                new ApprovalReview { Id = approvalReviewId });
                        });

            return dismissedReviewIds;
        }
    }
}
