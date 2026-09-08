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
        /// once: the reviews are dismissed and KEPT, and nothing here withdraws an invitation. The
        /// removal assertion is what states the choice — swapping the modify for a remove would
        /// pass a test that only checked the flags, and would leave a re-opened round without the
        /// reviewer it had.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnACompletedAIReviewerAssignmentToPendingOnResetAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Approval decidedApproval = SetupDecidedRound(ApprovalStatus.Approved);
            SetupEntityVisibility(isEntityVisible: true);

            AIReviewerAssignment completedAssignment = CreateAIReviewerAssignment(
                approvalId: decidedApproval.Id,
                isAIReviewCompleted: true,
                isAIReviewCommentsPresent: true);

            SetupStoredAIReviewerAssignment(decidedApproval.Id, completedAssignment);
            SetupAIReviewerAssignmentWrites();

            // when
            await this.approvalOrchestrationService.ResetApprovalAsync(
                entityType: decidedApproval.EntityType,
                entityId: decidedApproval.EntityId,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.ModifyAIReviewerAssignmentAsync(
                    It.Is<AIReviewerAssignment>(assignment =>
                        assignment.Id == completedAssignment.Id
                            && assignment.IsAIReviewCompleted == false
                            && assignment.IsAIReviewCommentsPresent == false),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // and Berean is still ON the round
            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.RemoveAIReviewerAssignmentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// Silent when there is nothing to take back. A round where Berean was never asked is the
        /// common case, and one already pending has nothing stale about it — a modify there would
        /// spend a write and an <c>AIReviewerAssignment-Modified</c> fact restating what storage
        /// already says.
        /// </summary>
        [Fact]
        public async Task ShouldNotWriteAnAIReviewerAssignmentOnResetWhenNoneIsStaleAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Approval decidedApproval = SetupDecidedRound(ApprovalStatus.Approved);
            SetupEntityVisibility(isEntityVisible: true);
            SetupAIReviewerAssignmentWrites();

            // Berean was never asked. Moq's default for the round-keyed read is already null, but
            // this states it: the whole point of the case is the absence.
            SetupStoredAIReviewerAssignment(decidedApproval.Id, storageAssignment: null);

            // when
            await this.approvalOrchestrationService.ResetApprovalAsync(
                entityType: decidedApproval.EntityType,
                entityId: decidedApproval.EntityId,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.ModifyAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// And the same silence for an assignment that is ALREADY pending — a round Berean was
        /// asked about but never finished. Split from the absent case because the two reach the
        /// same non-write through different guards, and a helper that checked only for the row's
        /// existence would write here.
        /// </summary>
        [Fact]
        public async Task ShouldNotRewriteAnAlreadyPendingAIReviewerAssignmentOnResetAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Approval decidedApproval = SetupDecidedRound(ApprovalStatus.Approved);
            SetupEntityVisibility(isEntityVisible: true);

            SetupStoredAIReviewerAssignment(
                decidedApproval.Id,
                CreateAIReviewerAssignment(
                    approvalId: decidedApproval.Id,
                    isAIReviewCompleted: false,
                    isAIReviewCommentsPresent: false));

            SetupAIReviewerAssignmentWrites();

            // when
            await this.approvalOrchestrationService.ResetApprovalAsync(
                entityType: decidedApproval.EntityType,
                entityId: decidedApproval.EntityId,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.ModifyAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// Berean's half runs LAST — after the human dismissal, and after the entity sync that
        /// unpublishes the content.
        ///
        /// <para>Its position against the SYNC is the load-bearing half. The reset is a fallible
        /// write, so anything that follows it is something a failure can cost: standing between
        /// the dismissal and the command, a throw left the approval back at <c>Submitted</c> with
        /// its reviews dismissed while the entity stayed <c>Approved</c> and publicly published —
        /// the state the operation exists to prevent.</para>
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

            SetupStoredAIReviewerAssignment(
                decidedApproval.Id,
                CreateAIReviewerAssignment(
                    approvalId: decidedApproval.Id,
                    isAIReviewCompleted: true));

            this.aiReviewerAssignmentServiceMock.Setup(service =>
                service.ModifyAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((AIReviewerAssignment assignment, CancellationToken _) =>
                        {
                            order.Add("ai-reset");

                            return assignment;
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
        /// And the reason that order is the one to hold: the reset's AI step is a FALLIBLE write,
        /// and a failure in it must not cost the entity its sync.
        ///
        /// <para>The round-one placement — between the dismissal and the command — made exactly
        /// that trade. A withdrawal landing between the read and the write is refused by the
        /// foundation, and on the old order that refusal left an approval at <c>Submitted</c>
        /// with no reviews standing behind it while the entity remained <c>Approved</c> and on
        /// the public site, with nothing to reconcile the two (§9.8).</para>
        ///
        /// <para>The operation still FAILS — the caller is told, and the two flags are still
        /// stale — because a refusal nobody hears is worse than one they can answer by asking
        /// Berean again. What it no longer does is fail with the content still published.</para>
        /// </summary>
        [Fact]
        public async Task ShouldStillSyncTheEntityWhenReturningBereanToPendingFailsAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Approval decidedApproval = SetupDecidedRound(ApprovalStatus.Approved);
            SetupEntityVisibility(isEntityVisible: true);

            SetupStoredAIReviewerAssignment(
                decidedApproval.Id,
                CreateAIReviewerAssignment(
                    approvalId: decidedApproval.Id,
                    isAIReviewCompleted: true,
                    isAIReviewCommentsPresent: true));

            // The row was withdrawn between the read above and this write, which the foundation
            // refuses as a write on a removed row.
            var withdrawnRowException = new AIReviewerAssignmentValidationException(
                message: "AI reviewer assignment validation error occurred, "
                    + "fix the errors and try again.",
                innerException: new NotFoundAIReviewerAssignmentException(
                    message: "AI reviewer assignment not found."));

            this.aiReviewerAssignmentServiceMock.Setup(service =>
                service.ModifyAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(withdrawnRowException);

            // when
            ValueTask<ApprovalOutcome> resetTask =
                this.approvalOrchestrationService.ResetApprovalAsync(
                    entityType: decidedApproval.EntityType,
                    entityId: decidedApproval.EntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                resetTask.AsTask);

            // then: the entity was taken off the public site anyway
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
        }
    }
}
