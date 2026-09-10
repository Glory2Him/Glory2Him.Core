// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    /// <summary>
    /// §8.6 HR-4's administrator override, reached from the moderation screen: an outcome applied
    /// by accident is taken back without starting a new round.
    /// </summary>
    public partial class ApprovalOrchestrationServiceTests
    {
        private Approval SetupDecidedRound(
            ApprovalStatus decidedStatus,
            bool isApprovedByBypass = false)
        {
            var decidedApproval = new Approval
            {
                Id = Guid.NewGuid(),
                EntityType = EntityType.ContentItem,
                EntityId = Guid.NewGuid(),
                ApprovalStatus = decidedStatus,
                IsApprovedByBypass = isApprovedByBypass,
                ApprovedByBypassReason = isApprovedByBypass ? "Waived for launch." : null,
            };

            SetupApprovalProbe(new ApprovalEntityMatch
            {
                Id = decidedApproval.Id,
                ApprovalStatus = decidedStatus,
                IsDeleted = false,
            });

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(
                    decidedApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(decidedApproval);

            this.approvalServiceMock.Setup(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Approval approval, WorkflowAttribution _, CancellationToken __) =>
                            approval);

            // Nothing to dismiss unless a test says so. Without this the gather answers null and
            // the dismissal loop faults, which would surface as a service exception in tests
            // about something else entirely.
            this.accessBrokerMock.Setup(broker =>
                broker.FindDismissableApprovalReviewIdsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<Guid>());

            // Nobody is sanctioned unless a test says so. The reset asks the §18.6 rule 2 veto
            // through the amend decision, and an unstubbed verdict answers null — which the
            // orchestration rightly refuses, so every test would fail on the block instead of on
            // its own subject.
            SetupAmendVerdict(PermittedVerdict());

            return decidedApproval;
        }

        private void SetupAmendVerdict(AccessVerdict verdict) =>
            this.accessBrokerMock.Setup(broker =>
                broker.MayAmendApprovalAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(verdict);

        /// <summary>
        /// §18.6 rule 2. A block is not a missing grant: it outranks every tier, and
        /// <c>Administrators</c> is a tier like any other. The reset is the most consequential
        /// write on this service — it dismisses every review and unpublishes live content — and
        /// it was the only one that never asked.
        ///
        /// <para>Nothing beneath this layer can catch it: the approval modify runs under the
        /// elevated identity and the entity command under the system identity, and both drop the
        /// caller's roles by construction, so every block test below sees an empty list.</para>
        /// </summary>
        [Fact]
        public async Task ShouldRefuseAResetToASanctionedAdministratorAsync()
        {
            // given: an administrator who also holds a ReadOnly block over this entity
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Approval decidedApproval = SetupDecidedRound(ApprovalStatus.Approved);
            SetupEntityVisibility(isEntityVisible: true);

            SetupAmendVerdict(new AccessVerdict
            {
                IsPermitted = false,
                DenialReason = AccessDenialReason.BlockedByReadOnlyRole,
                Explanation = "blocked",
                IsBypassUsed = false,
                BypassedBlockReason = AccessDenialReason.None,
            });

            // when
            ValueTask<ApprovalOutcome> resetTask =
                this.approvalOrchestrationService.ResetApprovalAsync(
                    entityType: decidedApproval.EntityType,
                    entityId: decidedApproval.EntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                resetTask.AsTask);

            // and nothing was written: no status move, no dismissal, no command
            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalReviewServiceMock.Verify(service =>
                service.DismissStaleApprovalReviewAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyBereanWasNotReturnedToPending();

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<ContentItem>()),
                Times.Never);
        }

        /// <summary>
        /// The whole operation, on both outcomes: the round goes back to <c>Submitted</c>, the
        /// active reviews are dismissed, and the entity is asked to follow.
        /// </summary>
        [Theory]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldResetADecidedRoundBackToSubmittedAsync(ApprovalStatus decidedStatus)
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Approval decidedApproval = SetupDecidedRound(decidedStatus);
            SetupEntityVisibility(isEntityVisible: true);

            var staleReviewId = Guid.NewGuid();

            this.accessBrokerMock.Setup(broker =>
                broker.FindDismissableApprovalReviewIdsAsync(
                    decidedApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<Guid> { staleReviewId });

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ResetApprovalAsync(
                    entityType: decidedApproval.EntityType,
                    entityId: decidedApproval.EntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then: the round is open again
            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            actualOutcome.IsEntitySyncRequested.Should().BeTrue();

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.Is<Approval>(approval =>
                        approval.ApprovalStatus == ApprovalStatus.Submitted),
                    WorkflowAttribution.DecidingCaller,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // and every active review was dismissed — §12.5.3 BR12's exception, which asks for
            // this regardless of RequireReapprovalOnChange
            this.approvalReviewServiceMock.Verify(service =>
                service.DismissStaleApprovalReviewAsync(
                    staleReviewId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // and the entity was asked to follow, which is what unpublishes it
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<ContentItem>()),
                Times.Once);
        }

        /// <summary>
        /// The dismissal runs AFTER the status write, so that the re-test each dismissal triggers
        /// sees the round as it now stands rather than the outcome being taken away.
        ///
        /// <para>NOT because anything refuses the other order. §8.8 regardless-rule 1 requires
        /// dismissal to work on a terminal round an administrator has moved back, and the design
        /// records that a round-window guard there "would refuse in the cases the operation
        /// exists to serve" — so no such guard exists. The foundation's own dismissal validation
        /// asks only whether the review is already dismissed or removed, never what the parent
        /// approval reads. The order is a correctness choice about what the re-test sees.</para>
        /// </summary>
        [Fact]
        public async Task ShouldOpenTheRoundBeforeDismissingItsReviewsAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Approval decidedApproval = SetupDecidedRound(ApprovalStatus.Approved);
            SetupEntityVisibility(isEntityVisible: true);

            var order = new List<string>();

            this.approvalServiceMock.Setup(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Approval approval, WorkflowAttribution _, CancellationToken __) =>
                        {
                            order.Add("modify");

                            return approval;
                        });

            this.accessBrokerMock.Setup(broker =>
                broker.FindDismissableApprovalReviewIdsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(() =>
                        {
                            order.Add("dismiss");

                            return new List<Guid>();
                        });

            // when
            await this.approvalOrchestrationService.ResetApprovalAsync(
                entityType: decidedApproval.EntityType,
                entityId: decidedApproval.EntityId,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            order.Should().ContainInOrder("modify", "dismiss");
        }

        /// <summary>
        /// The bypass pair records how THIS decision was reached (§9.7.5). A reset takes the
        /// decision away, so a round put back for review must stop claiming a waiver for an
        /// outcome it no longer holds.
        /// </summary>
        [Fact]
        public async Task ShouldClearTheBypassPairWhenTheOutcomeIsTakenBackAsync()
        {
            // given: a round approved by bypass, with the waiver recorded on it
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Approval bypassApproved = SetupDecidedRound(
                ApprovalStatus.Approved,
                isApprovedByBypass: true);

            SetupEntityVisibility(isEntityVisible: true);

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ResetApprovalAsync(
                    entityType: bypassApproved.EntityType,
                    entityId: bypassApproved.EntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualOutcome.IsApprovedByBypass.Should().BeFalse();
            actualOutcome.ApprovedByBypassReason.Should().BeNull();

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.Is<Approval>(approval =>
                        approval.IsApprovedByBypass == false
                            && approval.ApprovedByBypassReason == null),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// §8.6 HR-4. Deciding an open round belongs to the publisher tier; UNdeciding a closed
        /// one is the override, and the override has one holder. The publisher and reviewer tiers
        /// are named explicitly because both reach this panel and neither may press this control.
        /// </summary>
        [Theory]
        [InlineData(Roles.Publishers)]
        [InlineData(Roles.Reviewers)]
        [InlineData("ContentItem-Publishers")]
        public async Task ShouldRefuseAResetToAnybodyButAnAdministratorAsync(string role)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(role);

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: new UnauthorizedApprovalOrchestrationException(
                        message: "The current user is not allowed to reset this approval."));

            // when
            ValueTask<ApprovalOutcome> resetTask =
                this.approvalOrchestrationService.ResetApprovalAsync(
                    entityType: EntityType.ContentItem,
                    entityId: Guid.NewGuid(),
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    resetTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            // and nothing was read or written on the way — the tier is asked first, so an
            // unauthorised reset costs one role comparison rather than a table read
            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalReviewServiceMock.Verify(service =>
                service.DismissStaleApprovalReviewAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyBereanWasNotReturnedToPending();
        }

        /// <summary>
        /// A reset undoes an OUTCOME, so a round that never reached one has nothing to undo.
        /// Refused rather than quietly rewritten: accepting it would dismiss a live round's
        /// reviews for nothing.
        /// </summary>
        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Submitted)]
        public async Task ShouldRefuseAResetOnARoundThatWasNeverDecidedAsync(
            ApprovalStatus openStatus)
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Approval openApproval = SetupDecidedRound(openStatus);
            SetupEntityVisibility(isEntityVisible: true);

            // when
            ValueTask<ApprovalOutcome> resetTask =
                this.approvalOrchestrationService.ResetApprovalAsync(
                    entityType: openApproval.EntityType,
                    entityId: openApproval.EntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                resetTask.AsTask);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalReviewServiceMock.Verify(service =>
                service.DismissStaleApprovalReviewAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyBereanWasNotReturnedToPending();
        }

        /// <summary>
        /// §14.5 rule 3. A taken-down subject is not found for every caller, Administrators
        /// included — and an administrator is the only caller who reaches this operation at all,
        /// so without this the one tier that can reset would be the one tier that can reset a
        /// tombstone.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseAResetWhenTheSubjectHasBeenRemovedAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Approved));
            SetupEntityVisibility(isEntityVisible: false);

            // when
            ValueTask<ApprovalOutcome> resetTask =
                this.approvalOrchestrationService.ResetApprovalAsync(
                    entityType: EntityType.ContentItem,
                    entityId: Guid.NewGuid(),
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                resetTask.AsTask);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyBereanWasNotReturnedToPending();
        }

        /// <summary>
        /// §8.6 regardless-rule 1 bars anyone holding an active review from DECIDING the round. A
        /// reset decides nothing — it takes a decision away — so an administrator who reviewed
        /// may still put their own round back. Pinned because the rule sits one method away and
        /// would be easy to extend here by reflex.
        /// </summary>
        [Fact]
        public async Task ShouldAllowAnAdministratorHoldingAReviewToResetTheRoundAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Approval decidedApproval = SetupDecidedRound(ApprovalStatus.Approved);
            SetupEntityVisibility(isEntityVisible: true);

            // The administrator's own review is among the ones the round holds and dismisses.
            var ownReviewId = Guid.NewGuid();

            this.accessBrokerMock.Setup(broker =>
                broker.FindDismissableApprovalReviewIdsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<Guid> { ownReviewId });

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ResetApprovalAsync(
                    entityType: decidedApproval.EntityType,
                    entityId: decidedApproval.EntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);

            this.approvalReviewServiceMock.Verify(service =>
                service.DismissStaleApprovalReviewAsync(
                    ownReviewId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // AND THIS IS WHAT MAKES THE CLAIM CHECKABLE. §8.6 regardless-rule 1 lives in the
            // §8.6.1 DECISION function and nowhere else, so "a held review does not bar a reset"
            // is the same statement as "the reset never asks that function". Without this the
            // test would pass on a round the administrator holds no review on at all, which is
            // every round the suite arranges.
            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// §9.7.4's dismissal, for the one reviewer that is not a person (§8.6.2). Berean's
        /// assignment is keyed on the APPROVAL rather than on the round's reviews, so it survives
        /// everything the reset does untouched — and its two flags would go on reporting a
        /// completed pass, with comments, over content the override has just put back for review.
        ///
        /// <para>The ROW STAYS. That is the human posture applied to a row that is both halves at
        /// once: the reviews are dismissed and KEPT, and nothing here withdraws an invitation.
        /// Returning it to pending rather than removing it is the choice, and it is the only
        /// write this path may make on that row.</para>
        ///
        /// <para><b>What it catches.</b> Recording the wrong actor. This is a SYSTEM-identity
        /// write and the assertion is on the WORKFLOW seam, which is the only seam that mints
        /// that identity; the sibling on the edit path
        /// (ApprovalOrchestrationServiceTests.Flows.cs) runs as a plain author for the same
        /// reason. Routing it back onto the caller-facing foundation is no longer expressible
        /// here at all — that seam left with <c>IAIReviewerOrchestrationService</c>, so this
        /// service cannot reach it and the compiler holds what an assertion used to.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnACompletedAIReviewerAssignmentToPendingOnResetAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Approval decidedApproval = SetupDecidedRound(ApprovalStatus.Approved);
            SetupEntityVisibility(isEntityVisible: true);
            var staleAssignmentId = Guid.NewGuid();
            SetupResettableAIReviewerAssignment(decidedApproval.Id, staleAssignmentId);
            SetupAIReviewerAssignmentReturnToPending();

            // when
            await this.approvalOrchestrationService.ResetApprovalAsync(
                entityType: decidedApproval.EntityType,
                entityId: decidedApproval.EntityId,
                cancellationToken: TestContext.Current.CancellationToken);

            // then: the id the GATHER named, through the seam that mints the system identity
            // itself — the administrator who pressed Reset did not amend Berean's assignment, and
            // UpdatedBy must not say they did
            this.aiReviewerAssignmentWorkflowServiceMock.Verify(service =>
                service.ReturnStaleAIReviewerAssignmentToPendingAsync(
                    staleAssignmentId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // and that is the ONLY thing this path did to Berean's row. The seam carries no
            // removal verb at all, so "still ON the round" is now asserted as the absence of any
            // other call on it rather than as a Times.Never against a withdraw this service can
            // no longer reach.
            this.aiReviewerAssignmentWorkflowServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Silent when there is nothing to take back. A round where Berean was never asked is the
        /// common case, and one already pending has nothing stale about it — a write for either
        /// would spend an <c>AIReviewerAssignment-Modified</c> fact restating what storage already
        /// says.
        ///
        /// <para>Both arrive here as the same <c>null</c>, because the staleness predicate lives
        /// in the gather rather than in this layer: which row is which is pinned in
        /// AccessBrokerTests.FindResettableAIReviewerAssignmentId.Logic.cs, and the transition's
        /// own refusal to rewrite an already-pending row in
        /// AIReviewerAssignmentServiceTests.ReturnToPending.cs.</para>
        ///
        /// <para><b>What it catches.</b> A helper that called the seam unconditionally on the id
        /// it was handed — <c>Guid?</c> has a value for "nothing", and dereferencing it without
        /// the null check would put <c>Guid.Empty</c> through the transition and fault the whole
        /// reset on a round Berean was never on.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNotWriteAnAIReviewerAssignmentOnResetWhenNoneIsStaleAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Approval decidedApproval = SetupDecidedRound(ApprovalStatus.Approved);
            SetupEntityVisibility(isEntityVisible: true);
            SetupAIReviewerAssignmentReturnToPending();

            // Nothing to take back. Moq's default for ValueTask<Guid?> is already null, but this
            // states it: the whole point of the case is the absence.
            SetupResettableAIReviewerAssignment(
                decidedApproval.Id,
                aiReviewerAssignmentId: null);

            // when
            await this.approvalOrchestrationService.ResetApprovalAsync(
                entityType: decidedApproval.EntityType,
                entityId: decidedApproval.EntityId,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            this.aiReviewerAssignmentWorkflowServiceMock.Verify(service =>
                service.ReturnStaleAIReviewerAssignmentToPendingAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.aiReviewerAssignmentWorkflowServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Berean's half runs LAST — after the human dismissal, and after the entity sync that
        /// unpublishes the content.
        ///
        /// <para>Its position against the SYNC is legibility rather than damage control: the step
        /// logs its own failure and returns (see the test below), so it can no longer cost the
        /// entity its sync however it is placed. Last is where the tidy-up belongs, and where it
        /// reads as one.</para>
        ///
        /// <para>Its position against the DISMISSAL is intent rather than necessity today,
        /// because nothing subscribes to <c>AIReviewerAssignment-Modified</c> — §8.6.2's trigger
        /// event is deliberately not built. Pinned anyway: the day it exists, a re-triggered pass
        /// reading a round whose human reviews still counted would answer about content nobody
        /// had put back yet.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnBereanToPendingOnlyAfterTheEntityHasBeenUnpublishedAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Approval decidedApproval = SetupDecidedRound(ApprovalStatus.Approved);
            SetupEntityVisibility(isEntityVisible: true);

            var staleReviewId = Guid.NewGuid();
            var order = new List<string>();

            this.accessBrokerMock.Setup(broker =>
                broker.FindDismissableApprovalReviewIdsAsync(
                    decidedApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<Guid> { staleReviewId });

            this.approvalReviewServiceMock.Setup(service =>
                service.DismissStaleApprovalReviewAsync(
                    staleReviewId,
                    It.IsAny<CancellationToken>()))
                        .Callback(() => order.Add("dismiss"))
                        .ReturnsAsync((ApprovalReview)null);

            // The sync is observed where it BEGINS — the command envelope is minted immediately
            // before the publish, and the publish itself is a void-ish call this suite already
            // reads through this same seam.
            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateSystemAsync(It.IsAny<ContentItem>()))
                    .Returns((ContentItem content) =>
                    {
                        order.Add("entity-sync");

                        return new ValueTask<EventEnvelope<ContentItem>>(
                            new EventEnvelope<ContentItem>
                            {
                                Content = content,
                                SecurityContext = new SecurityContext(),
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            });
                    });

            SetupResettableAIReviewerAssignment(decidedApproval.Id, Guid.NewGuid());

            this.aiReviewerAssignmentWorkflowServiceMock.Setup(service =>
                service.ReturnStaleAIReviewerAssignmentToPendingAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Guid aiReviewerAssignmentId, CancellationToken _) =>
                        {
                            order.Add("ai-reset");

                            return new AIReviewerAssignment { Id = aiReviewerAssignmentId };
                        });

            // when
            await this.approvalOrchestrationService.ResetApprovalAsync(
                entityType: decidedApproval.EntityType,
                entityId: decidedApproval.EntityId,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            order.Should().Equal("dismiss", "entity-sync", "ai-reset");
        }

        /// <summary>
        /// THE AI STEP CANNOT FAULT THE RESET. It is a fallible write reached LAST, by which time
        /// the reset has already succeeded and committed — the status is <c>Submitted</c>, the
        /// reviews are dismissed, the entity is unpublished — and none of it can be taken back.
        ///
        /// <para>So a failure here is logged and swallowed rather than propagated. Letting it
        /// through reported a SUCCESSFUL reset to the moderator as a 424 telling them to try
        /// again, and the retry that advice invites is then refused by
        /// <c>ValidateStorageApprovalIsDecided</c> — the round is Submitted rather than decided —
        /// producing a second, unrelated error on a round that was never broken.</para>
        ///
        /// <para>What the failure costs instead: the two flags stay stale until a moderator asks
        /// Berean again, and the failure is in the error log. Nothing is swallowed silently, which
        /// is why the log call is asserted and not merely the absence of a throw.</para>
        /// </summary>
        [Fact]
        public async Task ShouldStillCompleteTheResetWhenReturningBereanToPendingFailsAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Approval decidedApproval = SetupDecidedRound(ApprovalStatus.Approved);
            SetupEntityVisibility(isEntityVisible: true);
            SetupResettableAIReviewerAssignment(decidedApproval.Id, Guid.NewGuid());

            // Storage failed under the transition. The seam reads a row and writes it back, so
            // this is the failure it actually has — a row withdrawn between the gather and the
            // write is answered unchanged rather than refused.
            var storageFailure = new Exception("storage is unreachable");

            var failedStorageException = new AIReviewerAssignmentDependencyException(
                message: "AI reviewer assignment dependency error occurred, contact support.",
                innerException: new FailedStorageAIReviewerAssignmentException(
                    message: "Failed AI reviewer assignment storage error occurred, "
                        + "contact support.",
                    innerException: storageFailure,
                    data: storageFailure.Data));

            this.aiReviewerAssignmentWorkflowServiceMock.Setup(service =>
                service.ReturnStaleAIReviewerAssignmentToPendingAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(failedStorageException);

            // when: no throw — the operation answers its caller normally
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ResetApprovalAsync(
                    entityType: decidedApproval.EntityType,
                    entityId: decidedApproval.EntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualOutcome.Should().NotBeNull();

            // the entity was taken off the public site
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<ContentItem>()),
                Times.Once);

            // and so was every step ahead of it, so the failure costs only the two flags
            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.Is<Approval>(approval =>
                        approval.ApprovalStatus == ApprovalStatus.Submitted),
                    WorkflowAttribution.DecidingCaller,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // AND THE FAILURE WAS RECORDED. Swallowing it without a log entry would leave a stale
            // panel with nothing anywhere to explain it — the exception the seam raised is what
            // reaches the error log, unwrapped, because this step has no chain of its own.
            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(failedStorageException),
                Times.Once);
        }
        /// <summary>
        /// THE REFUSAL TESTS' PIN ON THE AI HALF, and it is not decoration. Returning Berean to
        /// pending is a SYSTEM-identity write: the foundation transition asks only
        /// <c>IsSystemIdentity</c>, so no gate beneath the orchestration will refuse it. The
        /// administrator gate and the §18.6 ReadOnly veto in <c>ResetApprovalAsync</c> are the
        /// only things standing between a caller and that write, and both sit ABOVE it purely by
        /// statement order.
        ///
        /// <para>Without this, hoisting the call above those gates leaves every refusal test
        /// green while letting a non-administrator — or an administrator under a block — clear
        /// the two flags on any round they can name. The human dismissal is pinned against the
        /// same mistake by its own <c>Times.Never</c>; this is that pin for the half added later.
        /// Both halves are named, because the read alone reaching storage is already the
        /// ordering error, whether or not the write follows.</para>
        /// </summary>
        private void VerifyBereanWasNotReturnedToPending()
        {
            this.accessBrokerMock.Verify(broker =>
                broker.FindResettableAIReviewerAssignmentIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.aiReviewerAssignmentWorkflowServiceMock.Verify(service =>
                service.ReturnStaleAIReviewerAssignmentToPendingAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

    }
}
