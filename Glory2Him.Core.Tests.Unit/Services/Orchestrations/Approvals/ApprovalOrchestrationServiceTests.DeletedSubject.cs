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
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    /// <summary>
    /// §9.7.6 rule 3 and §14.5 rule 3 on the approval side: what a taken-down subject may still
    /// have done to its round, which is nothing.
    ///
    /// <para>The gap these close was reachable because removal deliberately leaves the approval
    /// alone (§9.7.6): a taken-down entity keeps its <c>ApprovalStatus</c>, so its round still
    /// reads <c>Submitted</c> and every status-shaped gate lets it through.</para>
    /// </summary>
    public partial class ApprovalOrchestrationServiceTests
    {
        /// <summary>
        /// The repair minted a round for a tombstone. The gate it passed asked for the entity's
        /// AUTHOR, and a soft-deleted row answers with its author exactly as it did before the
        /// takedown — the arms behind the probe are raw by-id reads and this repository has no
        /// EF global query filters. Every entity type, because the flaw was in every arm.
        /// </summary>
        [Theory]
        [InlineData(EntityType.ContentItem)]
        [InlineData(EntityType.Tag)]
        [InlineData(EntityType.Reaction)]
        [InlineData(EntityType.BibleReference)]
        [InlineData(EntityType.Comment)]
        [InlineData(EntityType.Link)]
        [InlineData(EntityType.Attachment)]
        [InlineData(EntityType.Association)]
        public async Task ShouldRefuseToRepairARoundForARemovedEntityAsync(EntityType entityType)
        {
            // given: no approval occupies the key, and the entity behind it has been taken down
            // while keeping the Submitted status removal never touches
            Guid inputEntityId = Guid.NewGuid();

            SetupApprovalProbe(null);
            SetupEntityVisibility(isEntityVisible: false);
            SetupEntityApprovalStatus(entityType, inputEntityId, ApprovalStatus.Submitted);

            // when
            ValueTask<ApprovalVerdict> retrieveVerdictTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    entityType,
                    inputEntityId,
                    TestContext.Current.CancellationToken);

            // then: the read answers not-found, which is what §14.5 rule 3 requires — a
            // soft-deleted entity is not found for every caller, Administrators included
            await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                retrieveVerdictTask.AsTask);

            // and no round was opened for it
            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // The status gate is never even reached: visibility is asked first, so a taken-down
            // entity is refused on being invisible rather than on the status it happens to keep.
            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveEntityApprovalStatusAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// #427. The repair OPENS the round and stops. Re-running the whole added flow made a GET
        /// a write that could also decide: under a policy with <c>RequireApprovals = false</c> and
        /// <c>AutoApproveIfAllApprovalRequirementsMet = true</c> — the shape the seed writes for
        /// the personal tier — the evaluation drove the fresh round to <c>Approved</c> and
        /// published the approving command under the workflow identity.
        ///
        /// <para>The entity stands at <c>Submitted</c> and the conditions are met and
        /// auto-approving, which is exactly the arrangement that used to approve on a read.</para>
        /// </summary>
        [Fact]
        public async Task ShouldOpenTheRoundOnAReadWithoutEvaluatingOrPublishingItAsync()
        {
            // given: no approval, a live entity already at Submitted, and a policy that would
            // auto-approve the moment anything evaluated it
            EntityType inputEntityType = EntityType.ContentItem;
            Guid inputEntityId = Guid.NewGuid();

            SetupApprovalProbe(null);
            SetupEntityVisibility(isEntityVisible: true);

            SetupEntityApprovalStatus(
                inputEntityType,
                inputEntityId,
                ApprovalStatus.Submitted);

            SetupConditions(CreateMetConditions(shouldAutoApprove: true));

            var openedApproval = new Approval
            {
                Id = Guid.NewGuid(),
                EntityType = inputEntityType,
                EntityId = inputEntityId,
                ApprovalStatus = ApprovalStatus.Submitted,
            };

            this.approvalServiceMock.Setup(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(openedApproval);

            // when
            ValueTask<ApprovalVerdict> retrieveVerdictTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    inputEntityType,
                    inputEntityId,
                    TestContext.Current.CancellationToken);

            // then: the probe is still mocked empty, so the read ends honestly — what matters is
            // WHAT it did on the way, not the answer it reaches
            await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                retrieveVerdictTask.AsTask);

            // The round IS opened: that is the whole point of repairing, because the panel
            // needs a row to report on
            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.Is<Approval>(approval =>
                        approval.EntityType == inputEntityType
                            && approval.EntityId == inputEntityId
                            && approval.ApprovalStatus == ApprovalStatus.Submitted),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // and NOTHING is decided on it. No transition onto the approval row, and no
            // approving command — a read may create the record it needs to answer and may never
            // approve or publish (§16.7.2).
            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<ContentItem>()),
                Times.Never);
        }

        /// <summary>
        /// §9.7.6 rule 3 at the orchestration, the outer half of the pair §14.6 rule 2 asks for.
        /// Reported as NOT FOUND rather than as a refusal, matching the read posture the entity's
        /// own transitions already keep: a takedown must not be distinguishable from an id that
        /// never existed.
        /// </summary>
        [Theory]
        [InlineData(ApprovalDecision.Approve)]
        [InlineData(ApprovalDecision.Reject)]
        public async Task ShouldRefuseDecidingARoundWhoseSubjectHasBeenRemovedAsync(
            ApprovalDecision decision)
        {
            // given: the round is real and open, its subject has been taken down
            EntityType inputEntityType = EntityType.ContentItem;
            Guid inputEntityId = Guid.NewGuid();

            SetupApprovalProbe(CreateApprovalMatch());
            SetupEntityVisibility(isEntityVisible: false);

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: new NotFoundApprovalOrchestrationException(
                        message: $"{inputEntityType} not found with id: {inputEntityId}."));

            // when
            ValueTask<ApprovalOutcome> decideTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType: inputEntityType,
                    entityId: inputEntityId,
                    decision: decision,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    decideTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            // The authorisation is never even asked: the subject is gone, so there is no
            // decision for any caller to be authorised for
            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The automatic half of the same rule. A round whose subject has been taken down still
        /// reads <c>Submitted</c> and its conditions can still be met — a comment resolved or a
        /// review withdrawn after the takedown reaches the evaluation — and approving it would
        /// write the divergence §9.8 forbids: the entity transition refuses a deleted row, so the
        /// approval would reach <c>Approved</c> while the entity stayed where it was.
        /// </summary>
        [Fact]
        public async Task ShouldNotAutoApproveARoundWhoseSubjectHasBeenRemovedAsync()
        {
            // given: an open round, met and auto-approving conditions, and a removed subject
            EntityType inputEntityType = EntityType.ContentItem;
            Guid inputEntityId = Guid.NewGuid();

            var openApproval = new Approval
            {
                Id = Guid.NewGuid(),
                EntityType = inputEntityType,
                EntityId = inputEntityId,
                ApprovalStatus = ApprovalStatus.Submitted,
            };

            SetupApprovalProbe(new ApprovalEntityMatch
            {
                Id = openApproval.Id,
                IsDeleted = false,
            });

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(
                    openApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(openApproval);

            SetupConditions(CreateMetConditions(shouldAutoApprove: true));

            SetupEntityVisibility(isEntityVisible: false);

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType: inputEntityType,
                    entityId: inputEntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then: the round is left OPEN, which is what a restore resumes from (§9.7.6)
            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            actualOutcome.IsEntitySyncRequested.Should().BeFalse();

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<ContentItem>()),
                Times.Never);
        }
    }
}
