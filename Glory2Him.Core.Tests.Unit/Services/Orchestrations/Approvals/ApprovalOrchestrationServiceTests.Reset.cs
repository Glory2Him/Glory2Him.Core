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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.ContentItems;
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

            return decidedApproval;
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
        /// The dismissal runs AFTER the status write. Dismissing while the round still read
        /// terminal would re-test a round nobody may act on, and the foundation refuses a
        /// dismissal against one in any case (§8.8 regardless-rule 1).
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

            // the administrator's own review is among the ones dismissed
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
        }
    }
}
