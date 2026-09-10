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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.AIReviewers;
using Glory2Him.Core.Models.Orchestrations.AIReviewers.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.AIReviewers
{
    /// <summary>
    /// <c>ResolveAIReviewerApprovalAsync</c> — the shared opening move of all three operations,
    /// and the reason this file exists separately from the ones about what they then DO.
    ///
    /// <para><b>It is a duplicate, and duplicates drift.</b> The visibility gate, the unfiltered
    /// probe and the read-triggered repair are the approval orchestration's, copied narrow rather
    /// than shared, because two orchestrations must not depend on one another. The compiler holds
    /// nothing here — only these tests do, and their opposite numbers in
    /// <c>ApprovalOrchestrationServiceTests.RetrieveVerdict.Validations.cs</c>. Every case below
    /// is asked of THIS service, against the same inputs, for the same answer.</para>
    /// </summary>
    public partial class AIReviewerOrchestrationServiceTests
    {
        /// <summary>
        /// §14.5 rule 3, asked BEFORE the approval is looked up. A takedown happens to an item
        /// that has already been through the flow, so the ordinary taken-down entity HAS a round
        /// — and gating only the repair would leave exactly that case answering with the round's
        /// AI status while an id that never existed answered 404. Two answers a caller can tell
        /// apart are one refusal and one oracle.
        ///
        /// <para>The sentence is pinned, not merely the type: it is character-for-character the
        /// one a missing approval gives, for the reason <c>ValidateStorageEntityIsVisible</c>
        /// writes down.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReportNotFoundWhenTheEntityBehindTheRoundIsNotVisibleAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            EntityType inputEntityType = EntityType.BibleReference;
            Guid inputEntityId = Guid.NewGuid();

            // Stated rather than left to the mock: the fixture defaults the subject to VISIBLE,
            // so a test about an entity that is not there has to say so.
            SetupEntityVisibility(isEntityVisible: false);
            SetupResolvedRound(approvalId: Guid.NewGuid());
            SetupAIReviewerOffer(isOffered: true);

            // when
            ValueTask<AIReviewerStatus> statusTask =
                this.aiReviewerOrchestrationService.RetrieveAIReviewerStatusAsync(
                    inputEntityType,
                    inputEntityId,
                    TestContext.Current.CancellationToken);

            AIReviewerOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerOrchestrationValidationException>(
                    statusTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<NotFoundAIReviewerOrchestrationException>();

            actualException.InnerException.Message.Should()
                .Be($"Approval not found for {inputEntityType} with id: {inputEntityId}.");

            // and the round was never read, so the refusal costs no lookup and leaks nothing
            // about whether one exists
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.aiReviewerAssignmentServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The honest not-found: no round occupies the key and the repair cannot open one, because
        /// the entity has already been DECIDED. A round is opened at the entity's own status (§9.2
        /// rules 1-2) and the foundation admits only Draft or Submitted, so a decided entity has
        /// no status a repair could legally use — and opening one at Draft beneath an Approved row
        /// would write the divergence §9.8 exists to forbid.
        /// </summary>
        [Fact]
        public async Task ShouldReportNotFoundWithoutOpeningARoundForADecidedEntityAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            EntityType inputEntityType = EntityType.ContentItem;
            Guid inputEntityId = Guid.NewGuid();

            SetupApprovalProbe(null);
            SetupEntityApprovalStatus(ApprovalStatus.Approved);

            // when
            ValueTask<AIReviewerStatus> statusTask =
                this.aiReviewerOrchestrationService.RetrieveAIReviewerStatusAsync(
                    inputEntityType,
                    inputEntityId,
                    TestContext.Current.CancellationToken);

            AIReviewerOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerOrchestrationValidationException>(
                    statusTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<NotFoundAIReviewerOrchestrationException>();

            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// §9.7.2 rule 1's read-triggered repair. Two ways in leave an entity without a round:
        /// seed data written straight to the storage broker, which publishes no fact at all, and a
        /// fact that does not land. Either way this panel keys on the approval, so a moderator who
        /// can do nothing about it would be told not-found — and the repair is what stops that.
        ///
        /// <para>OPENED AT THE ENTITY'S OWN STATUS, which is the half worth asserting: a Submitted
        /// entity opens a Submitted round, and anything else would leave the round and the entity
        /// disagreeing on the very first read.</para>
        /// </summary>
        [Fact]
        public async Task ShouldOpenTheMissingRoundAndCarryOnWithItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            EntityType inputEntityType = EntityType.ContentItem;
            Guid inputEntityId = Guid.NewGuid();
            Guid repairedApprovalId = Guid.NewGuid();

            // Three probes, in the order the resolver makes them: the opening one that finds
            // nothing, the repair's own pre-insert re-check, and the re-probe that reads back what
            // the repair opened.
            this.approvalServiceMock.SetupSequence(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalEntityMatch)null)
                        .ReturnsAsync((ApprovalEntityMatch)null)
                        .ReturnsAsync(new ApprovalEntityMatch
                        {
                            Id = repairedApprovalId,
                            ApprovalStatus = ApprovalStatus.Submitted,
                            IsDeleted = false,
                        });

            SetupEntityApprovalStatus(ApprovalStatus.Submitted);
            SetupAIReviewerOffer(isOffered: true);
            SetupStoredAIReviewerAssignment(repairedApprovalId, storageAssignment: null);

            this.approvalServiceMock.Setup(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Approval approval, CancellationToken _) => approval);

            // when
            AIReviewerStatus actualStatus =
                await this.aiReviewerOrchestrationService.RetrieveAIReviewerStatusAsync(
                    inputEntityType,
                    inputEntityId,
                    TestContext.Current.CancellationToken);

            // then: the read answered rather than refusing
            actualStatus.IsOffered.Should().BeTrue();
            actualStatus.IsRequested.Should().BeFalse();

            // and the round was OPENED at the entity's own status, carrying a minted id — the
            // foundation stamps the audit values itself but refuses an empty Id
            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.Is<Approval>(approval =>
                        approval.EntityType == inputEntityType
                            && approval.EntityId == inputEntityId
                            && approval.Id != Guid.Empty
                            && approval.ApprovalStatus == ApprovalStatus.Submitted),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// And a DRAFT entity opens a Draft round, which is the other half of §9.2 rules 1-2:
        /// nothing enters review on a status nobody offered. The read still ends in the honest
        /// not-found here, because the re-probe is arranged to find nothing — what this asserts is
        /// the status the repair chose, not the answer the caller got.
        /// </summary>
        [Fact]
        public async Task ShouldOpenTheRepairedRoundAtDraftWhenTheEntityIsStillADraftAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            EntityType inputEntityType = EntityType.ContentItem;
            Guid inputEntityId = Guid.NewGuid();

            SetupApprovalProbe(null);
            SetupEntityApprovalStatus(ApprovalStatus.Draft);

            this.approvalServiceMock.Setup(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Approval approval, CancellationToken _) => approval);

            // when
            ValueTask<AIReviewerStatus> statusTask =
                this.aiReviewerOrchestrationService.RetrieveAIReviewerStatusAsync(
                    inputEntityType,
                    inputEntityId,
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<AIReviewerOrchestrationValidationException>(
                statusTask.AsTask);

            // then
            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.Is<Approval>(approval =>
                        approval.ApprovalStatus == ApprovalStatus.Draft),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// THE PRE-INSERT RE-PROBE, and why it is kept. The repair re-checks immediately before
        /// writing, so a round that appeared while its two gates were running is FOUND rather than
        /// collided with — the same point the approval orchestration's own retrieve-or-create
        /// re-probes at. Dropping it would widen the collision window to span two extra broker
        /// calls and turn a race the round's own repair absorbs into a refusal.
        /// </summary>
        [Fact]
        public async Task ShouldNotOpenARoundWhenAConcurrentRepairAlreadyOpenedItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid winningApprovalId = Guid.NewGuid();

            var winningMatch = new ApprovalEntityMatch
            {
                Id = winningApprovalId,
                ApprovalStatus = ApprovalStatus.Submitted,
                IsDeleted = false,
            };

            // Nothing on the opening probe; the winner's round has landed by the time the repair
            // re-checks, and is still there on the re-probe afterwards.
            this.approvalServiceMock.SetupSequence(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalEntityMatch)null)
                        .ReturnsAsync(winningMatch)
                        .ReturnsAsync(winningMatch);

            SetupEntityApprovalStatus(ApprovalStatus.Submitted);
            SetupAIReviewerOffer(isOffered: true);
            SetupStoredAIReviewerAssignment(winningApprovalId, storageAssignment: null);

            // when
            AIReviewerStatus actualStatus =
                await this.aiReviewerOrchestrationService.RetrieveAIReviewerStatusAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then: the winner's round answered, and nothing was written
            actualStatus.IsOffered.Should().BeTrue();

            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// And the collision that OUTLIVES that re-probe is NOT swallowed. It reaches the caller
        /// as a dependency validation failure — a <c>400</c> at the exposer — which is exactly what
        /// the approval round's own repair does with the same race on
        /// <c>UX_Approvals_EntityType_EntityId</c>, and what
        /// <c>ApprovalsController.GetApprovalVerdictAsync</c> already documents. Retrying finds the
        /// round the winner opened.
        ///
        /// <para>Pinned because the alternative is tempting and wrong here: dissolving the
        /// collision into the winner's row would be a BEHAVIOUR CHANGE relative to the sibling this
        /// resolver was copied from, and the two answers to one race must not differ by which
        /// endpoint asked.</para>
        /// </summary>
        [Fact]
        public async Task ShouldSurfaceTheCollisionWhenTwoRepairsRaceToOpenTheSameRoundAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            string randomMessage = GetRandomString();

            var collisionException =
                new Glory2Him.Core.Models.Foundations.Approvals.Exceptions
                    .ApprovalDependencyValidationException(
                        message: randomMessage,
                        innerException: new Xeptions.Xeption(message: randomMessage));

            SetupApprovalProbe(null);
            SetupEntityApprovalStatus(ApprovalStatus.Submitted);

            this.approvalServiceMock.Setup(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(collisionException);

            // when
            ValueTask<AIReviewerStatus> statusTask =
                this.aiReviewerOrchestrationService.RetrieveAIReviewerStatusAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            AIReviewerOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerOrchestrationDependencyValidationException>(
                    statusTask.AsTask);

            // then: dependency VALIDATION, so the exposer answers 400 rather than 424
            actualException.InnerException.Should()
                .BeSameAs(collisionException.InnerException);

            this.aiReviewerAssignmentServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The same gate on the WRITE path, and the reason it is asserted twice: a taken-down
        /// entity keeps its <c>ApprovalStatus</c>, because removal deliberately leaves the approval
        /// alone (§9.7.6), so every ingredient the repair needs is still sitting there. The
        /// resolver's refusal is what stops a round being minted for a tombstone and an assignment
        /// being hung off it — nothing further down would.
        ///
        /// <para>The repair carries its own copy of this gate as well, for the same reason its
        /// sibling does: it mints a row for an id that came straight off a route. That copy is
        /// unreachable from this service today precisely because the resolver refuses first, which
        /// is why the assertion here is on the outcome — nothing opened, nothing assigned — rather
        /// than on which of the two gates spoke.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNotOpenARoundForAnEntityThatCannotBeSeenAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            SetupEntityVisibility(isEntityVisible: false);
            SetupApprovalProbe(null);
            SetupEntityApprovalStatus(ApprovalStatus.Submitted);
            SetupAIReviewerOffer(isOffered: true);
            SetupAIReviewerAssignmentWrites();

            // when
            ValueTask<AIReviewerAssignment> requestTask =
                this.aiReviewerOrchestrationService.RequestAIReviewerAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            AIReviewerOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerOrchestrationValidationException>(
                    requestTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<NotFoundAIReviewerOrchestrationException>();

            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.aiReviewerAssignmentServiceMock.VerifyNoOtherCalls();
        }
    }
}
