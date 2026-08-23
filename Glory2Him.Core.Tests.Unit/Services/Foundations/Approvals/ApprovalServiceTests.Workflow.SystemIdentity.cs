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
using Force.DeepCloner;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Approvals
{
    /// <summary>
    /// The workflow's own identity on the <c>Approval</c> record (#287).
    /// </summary>
    /// <remarks>
    /// Each test here pairs the workflow path against the PUBLIC path with the same caller, so
    /// what is proven is the difference rather than the absence of a gate. A test that only drove
    /// the workflow path would pass just as well if the gate had been deleted for everyone.
    /// </remarks>
    public partial class ApprovalServiceTests
    {
        // The actor class #287 exists for: authenticated, contributing, and holding NO review
        // role — the state a commenter is in, because commenting deliberately carries no tier.
        private static SecurityContext CreateContributorSecurityContext(string subjectId) =>
            new SecurityContext
            {
                IsAuthenticated = true,
                SubjectId = subjectId,
                Roles = []
            };

        [Fact]
        public async Task ShouldReadTheRoundUnfilteredOnWorkflowRetrieveAsync()
        {
            // given: a round owned by SOMEBODY ELSE, read by a contributor with no review role.
            // This is the ordinary case — a contributor speaks on an approval they can see, the
            // workflow re-tests the round, and before #287 that re-test threw.
            string contributorUserId = GetRandomString();
            this.ambientSecurityContext = CreateContributorSecurityContext(contributorUserId);

            Approval storageApproval = CreateRandomApproval();
            storageApproval.IsDeleted = false;
            storageApproval.CreatedBy = GetRandomString();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(contributorUserId);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    storageApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            // when
            Approval actualApproval =
                await this.approvalWorkflowService.RetrieveApprovalByIdAsync(
                    storageApproval.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApproval.Should().BeEquivalentTo(storageApproval,
                because: "what a round IS is a fact about storage, not about who is asking — "
                    + "an identity-filtered read must never be the input to the workflow's own "
                    + "evaluation");

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<Approval>()),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Approval>()),
                Times.Never,
                failMessage: "minting from the ambient caller is exactly what made the re-test "
                    + "depend on which actor happened to trigger it");
        }

        [Fact]
        public async Task ShouldStillRefuseTheSameCallerOnThePublicRetrieveAsync()
        {
            // given: the SAME round and the SAME caller as above. The public read must be
            // unchanged — #287 widened the workflow's view, not everyone's.
            string contributorUserId = GetRandomString();
            this.ambientSecurityContext = CreateContributorSecurityContext(contributorUserId);

            Approval storageApproval = CreateRandomApproval();
            storageApproval.IsDeleted = false;
            storageApproval.CreatedBy = GetRandomString();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(contributorUserId);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    storageApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            // when
            ValueTask<Approval> retrieveTask =
                this.approvalService.RetrieveApprovalByIdAsync(
                    storageApproval.Id,
                    TestContext.Current.CancellationToken);

            // then: not-found rather than unauthorized, per §14.5 — the caller must not learn
            // the round exists
            await Assert.ThrowsAsync<ApprovalValidationException>(retrieveTask.AsTask);
        }

        /// <summary>
        /// The round-open precondition is NOT one of the tiers the workflow skips.
        /// </summary>
        /// <remarks>
        /// <para>It lived inside the §8.6.1 decision function, which answers
        /// <c>ApprovalNotOpenForReview</c> for any state but <c>Submitted</c>. Skipping that
        /// function for the system identity skipped this with it, and
        /// <c>ProcessEntityModifiedAsync</c> — the one flow of three with no round-open check of
        /// its own — could then drive a <c>Draft</c> round to <c>Approved</c> through
        /// <c>EvaluateApprovalAsync</c>, whose only condition is that the approval conditions are
        /// met. A Draft's conditions can be met.</para>
        ///
        /// <para>That is a transition NO human can make, <c>Admin</c> included, on the row §9.8
        /// calls the source of truth. The guard is therefore unconditional: "is this round open"
        /// is a fact about STORAGE, which is the same argument that justifies the workflow's
        /// unfiltered read.</para>
        /// </remarks>
        [Theory]
        [InlineData(ApprovalStatus.Draft, ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Draft, ApprovalStatus.Rejected)]
        [InlineData(ApprovalStatus.Approved, ApprovalStatus.Rejected)]
        [InlineData(ApprovalStatus.Rejected, ApprovalStatus.Approved)]
        public async Task ShouldRefuseAnOutcomeOnARoundThatIsNotOpenOnWorkflowModifyAsync(
            ApprovalStatus storedStatus,
            ApprovalStatus attemptedOutcome)
        {
            // given
            string contributorUserId = GetRandomString();
            this.ambientSecurityContext = CreateContributorSecurityContext(contributorUserId);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Approval inputApproval =
                CreateRandomModifyApproval(randomDateTimeOffset, contributorUserId);

            inputApproval.ApprovalStatus = attemptedOutcome;
            inputApproval.IsApprovedByBypass = false;
            inputApproval.ApprovedByBypassReason = null;

            Approval storageApproval = inputApproval.DeepClone();
            storageApproval.UpdatedWhen =
                storageApproval.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            storageApproval.IsDeleted = false;

            // NOT Submitted — there is no open round to decide
            storageApproval.ApprovalStatus = storedStatus;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(contributorUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(inputApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<Approval>()))
                        .ReturnsAsync(inputApproval);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    inputApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            // when
            ValueTask<Approval> modifyTask =
                this.approvalWorkflowService.ModifyApprovalAsync(
                    inputApproval,
                    TestContext.Current.CancellationToken);

            // then
            await Assert.ThrowsAsync<ApprovalValidationException>(modifyTask.AsTask);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateApprovalAsync(
                        It.IsAny<Approval>(),
                        It.IsAny<CancellationToken>()),
                Times.Never,
                failMessage: "the workflow must not be able to decide a round nobody opened — "
                    + "the system identity replaces the caller TIERS, not the state invariants");
        }

        /// <summary>
        /// A bypass survives the seam. The seam is not only the AUTOMATIC path.
        /// </summary>
        /// <remarks>
        /// <para><c>RecordApprovalDecisionAsync</c> relays a deliberate human decision through
        /// this same interface and sets <c>IsApprovedByBypass</c> from that decision's own
        /// verdict — true whenever an <c>Admin</c> legitimately bypass-approves. A guard keyed on
        /// the system identity would refuse exactly the case a bypass exists for.</para>
        ///
        /// <para>One was written and removed. This test exists so the next person who reasons
        /// "the workflow takes no verdict, so it may not claim a waiver" finds out here rather
        /// than in production — the orchestration tests mock this seam, so nothing above the
        /// foundation would catch it.</para>
        /// </remarks>
        [Fact]
        public async Task ShouldCarryABypassThroughTheSeamOnWorkflowModifyAsync()
        {
            // given: the shape RecordApprovalDecisionAsync produces for an Admin bypass —
            // an outcome applied to an open round, carrying a verdict-derived waiver
            string adminUserId = GetRandomString();
            this.ambientSecurityContext = CreateContributorSecurityContext(adminUserId);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Approval inputApproval =
                CreateRandomModifyApproval(randomDateTimeOffset, adminUserId);

            inputApproval.ApprovalStatus = ApprovalStatus.Approved;
            inputApproval.IsApprovedByBypass = true;
            inputApproval.ApprovedByBypassReason = GetRandomString();

            Approval auditAppliedApproval = inputApproval.DeepClone();

            Approval storageApproval = auditAppliedApproval.DeepClone();
            storageApproval.UpdatedWhen =
                storageApproval.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            storageApproval.IsDeleted = false;
            storageApproval.ApprovalStatus = ApprovalStatus.Submitted;
            storageApproval.IsApprovedByBypass = false;
            storageApproval.ApprovedByBypassReason = null;

            Approval updatedApproval = auditAppliedApproval.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(adminUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<Approval>()))
                        .ReturnsAsync(auditAppliedApproval);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    inputApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedApproval);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalAsync(
                    It.IsAny<EventEnvelope<Approval>>(),
                    It.IsAny<ApprovalEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Approval>>(
                            new EventPublishResult<Approval>()));

            // when
            Approval actualApproval =
                await this.approvalWorkflowService.ModifyApprovalAsync(
                    inputApproval,
                    TestContext.Current.CancellationToken);

            // then: the waiver reaches storage rather than being refused
            actualApproval.Should().NotBeNull();

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateApprovalAsync(
                        It.Is<Approval>(approval => approval.IsApprovedByBypass),
                        It.IsAny<CancellationToken>()),
                Times.Once,
                failMessage: "an Admin bypass-approval is relayed through this seam, so the "
                    + "system identity must not be treated as proof that no waiver applies");
        }

        [Fact]
        public async Task ShouldWriteTheDecisionWithoutTheCallerTiersOnWorkflowModifyAsync()
        {
            // given: the workflow applying an outcome. The acting identity holds no publisher
            // tier and never will — an automatic approval is fired by the last reviewer's own
            // review, and §8.6 regardless-rule 1 forbids that reviewer from applying it.
            string contributorUserId = GetRandomString();
            this.ambientSecurityContext = CreateContributorSecurityContext(contributorUserId);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Approval inputApproval =
                CreateRandomModifyApproval(randomDateTimeOffset, contributorUserId);

            inputApproval.ApprovalStatus = ApprovalStatus.Approved;
            inputApproval.IsApprovedByBypass = false;
            inputApproval.ApprovedByBypassReason = null;

            Approval auditAppliedApproval = inputApproval.DeepClone();

            Approval storageApproval = auditAppliedApproval.DeepClone();
            storageApproval.UpdatedWhen =
                storageApproval.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            storageApproval.IsDeleted = false;
            storageApproval.ApprovalStatus = ApprovalStatus.Submitted;

            Approval updatedApproval = auditAppliedApproval.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(contributorUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<Approval>()))
                        .ReturnsAsync(auditAppliedApproval);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    inputApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedApproval);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalAsync(
                    It.IsAny<EventEnvelope<Approval>>(),
                    It.IsAny<ApprovalEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Approval>>(
                            new EventPublishResult<Approval>()));

            // when
            Approval actualApproval =
                await this.approvalWorkflowService.ModifyApprovalAsync(
                    inputApproval,
                    TestContext.Current.CancellationToken);

            // then
            actualApproval.Should().NotBeNull();

            // The decision function is NOT consulted. Asking it under a roleless system context
            // would refuse deterministically — consequence 3 of #287 — and would replace the
            // orchestration's derivation, made with the deciding context, by one made without it.
            // MayDecideApprovalByIdAsync, which is the member ApprovalService actually calls.
            // An earlier version of this asserted Times.Never on MayDecideApprovalAsync — a
            // member this service never calls on ANY path, so it held identically on the public
            // path where the gate IS consulted, and proved nothing.
            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // Nor the entity-narrowed amend tier, for the same reason: it asks whether a PERSON
            // may amend, and there is no person.
            this.accessBrokerMock.Verify(broker =>
                broker.MayAmendApprovalAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<Approval>()),
                Times.Once);

            // The MINTED context is what stamps the row, carrying the deciding human forward
            // with no roles — the audit answer to "who caused this" is still a person.
            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Approval>(),
                    It.Is<SecurityContext>(securityContext =>
                        securityContext.IsSystemIdentity
                            && securityContext.SubjectId == contributorUserId
                            && securityContext.Roles.Count == 0)),
                Times.Once);
        }
    }
}
