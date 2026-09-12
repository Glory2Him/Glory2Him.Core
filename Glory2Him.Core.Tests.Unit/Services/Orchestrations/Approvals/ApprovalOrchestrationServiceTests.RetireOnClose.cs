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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    /// <summary>
    /// §7.9 rule 8 — a round that has closed on an outcome retires the invitations it never
    /// answered. They gate nothing, which is why the rule was once "nothing needs to clean it
    /// up"; what they DO is render, and an outstanding ask beside a settled outcome says the
    /// round is waiting for a vote that can no longer be cast at all.
    ///
    /// <para>The three tests that matter most here are the three ROUTES. A rule applied on the
    /// button and forgotten on the two automatic closes would leave the commonest rounds — the
    /// ones nobody clicks — exactly as cluttered as before.</para>
    /// </summary>
    public partial class ApprovalOrchestrationServiceTests
    {
        /// <summary>
        /// Route one: a person pressed Approve or Reject. Both outcomes retire, because both
        /// close the round — a rejection is as final for an unanswered invitation as an approval.
        /// </summary>
        [Theory]
        [InlineData(ApprovalDecision.Approve)]
        [InlineData(ApprovalDecision.Reject)]
        public async Task ShouldRetireTheOutstandingInvitationsWhenAPersonDecidesTheRoundAsync(
            ApprovalDecision decision)
        {
            // given: two people were asked and neither answered before the round was decided
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var firstRequestId = Guid.NewGuid();
            var secondRequestId = Guid.NewGuid();

            Approval storageApproval = CreateDecisionApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            SetupDecisionSystemEnvelopes();
            SetupDecisionLinkCommandPublish();
            SetupDecisionApprovalRow(storageApproval);
            SetupRetirableApprovalReviewRequests(approvalId, firstRequestId, secondRequestId);
            SetupClosedRoundRetirement();

            // when
            await this.approvalOrchestrationService.DecideApprovalAsync(
                EntityType.Link,
                entityId,
                decision,
                false,
                null,
                TestContext.Current.CancellationToken);

            // then: EVERY outstanding row, not just the first one found
            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireClosedRoundApprovalReviewRequestAsync(
                    firstRequestId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireClosedRoundApprovalReviewRequestAsync(
                    secondRequestId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // Through the WORKFLOW seam, which mints the system identity itself. The caller
            // pressed Approve or Reject; they did not withdraw anybody's invitation, and
            // DeletedBy must not say they did.
            this.approvalReviewRequestServiceMock.Verify(service =>
                service.RemoveApprovalReviewRequestByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // And the ANSWERED retirement is not the verb reached: the two carry different
            // sentences, and a round closing on somebody is not the same as them answering.
            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireAnsweredApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // The UNFILTERED read, keyed on this round. The caller-facing read applies §14.7
            // posture D, and the other two routes have no moderator on them at all.
            this.accessBrokerMock.Verify(broker =>
                broker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.approvalReviewRequestServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Route two: nobody pressed anything — a standing rejection under
        /// <c>BlockOnReject</c> ended the round. There is no caller to attribute the retirement
        /// to even in principle, which is the clearest case for the system identity.
        /// </summary>
        [Fact]
        public async Task ShouldRetireTheOutstandingInvitationsWhenAStandingRejectionClosesTheRoundAsync()
        {
            // given
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var requestId = Guid.NewGuid();

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            List<Approval> savedApprovals = SetupFlowApprovalRow(storageApproval);
            SetupFlowSystemEnvelope<Link>();
            SetupFlowLinkCommandPublish();

            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    areConditionsMet: true,
                    shouldAutoApprove: true,
                    blockReasons: new List<AccessDenialReason>
                    {
                        AccessDenialReason.BlockedByRejection,
                    }));

            SetupRetirableApprovalReviewRequests(approvalId, requestId);
            SetupClosedRoundRetirement();

            // when
            await this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                approvalId,
                TestContext.Current.CancellationToken);

            // then: the round really did close on Rejected, so the retirement is a consequence of
            // an outcome rather than of the flow merely having run
            savedApprovals.Should().ContainSingle();
            savedApprovals[0].ApprovalStatus.Should().Be(ApprovalStatus.Rejected);

            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireClosedRoundApprovalReviewRequestAsync(
                    requestId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// Route three, and the one the gathering seam exists for. An automatic approval runs
        /// under the identity of whoever's review or edit tipped the round — frequently somebody
        /// with no review role at all — so a caller-facing read would answer them with nothing
        /// and the panel would keep its stale rows with no error anywhere to notice.
        /// </summary>
        [Fact]
        public async Task ShouldRetireTheOutstandingInvitationsWhenTheRoundAutoApprovesAsync()
        {
            // given
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var requestId = Guid.NewGuid();

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            List<Approval> savedApprovals = SetupFlowApprovalRow(storageApproval);
            SetupFlowSystemEnvelope<Link>();
            SetupFlowLinkCommandPublish();

            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    areConditionsMet: true,
                    shouldAutoApprove: true));

            SetupRetirableApprovalReviewRequests(approvalId, requestId);
            SetupClosedRoundRetirement();

            // when
            await this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                approvalId,
                TestContext.Current.CancellationToken);

            // then
            savedApprovals.Should().ContainSingle();
            savedApprovals[0].ApprovalStatus.Should().Be(ApprovalStatus.Approved);

            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireClosedRoundApprovalReviewRequestAsync(
                    requestId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// The round is still OPEN — the conditions were not met — so the invitations are exactly
        /// what they were asked for. Retiring here would cancel a round's reviewers for the crime
        /// of somebody having looked at it.
        /// </summary>
        [Fact]
        public async Task ShouldNotRetireAnythingWhileTheRoundIsStillOpenAsync()
        {
            // given
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            SetupFlowApprovalRow(storageApproval);
            SetupFlowSystemEnvelope<Link>();
            SetupFlowLinkCommandPublish();

            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    areConditionsMet: false,
                    shouldAutoApprove: true));

            SetupRetirableApprovalReviewRequests(approvalId, Guid.NewGuid());
            SetupClosedRoundRetirement();

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                    approvalId,
                    TestContext.Current.CancellationToken);

            // then
            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);

            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireClosedRoundApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // Not even the READ: the status gate is asked before the gather, so an open round
            // costs nothing at all.
            this.accessBrokerMock.Verify(broker =>
                broker.FindRetirableApprovalReviewRequestIdsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// §8.6 HR-4's override moves a decided round back to <c>Submitted</c>, which is the
        /// opposite of closing it — and it reaches the entity command through the same publish
        /// seam the three closing routes do. The status gate is what keeps it a no-op, so this is
        /// the test that would catch the retirement being hidden inside that shared seam.
        ///
        /// <para>§7.9 rule 8 also rules that a reset does NOT bring retired invitations back: a
        /// moderator asks again. Nothing here resurrects a row, and there is deliberately no verb
        /// that could.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNotRetireAnythingWhenAnAdministratorResetsTheRoundAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Approval decidedApproval = SetupDecidedRound(ApprovalStatus.Approved);
            SetupEntityVisibility(isEntityVisible: true);
            SetupRetirableApprovalReviewRequests(decidedApproval.Id, Guid.NewGuid());
            SetupClosedRoundRetirement();

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ResetApprovalAsync(
                    decidedApproval.EntityType,
                    decidedApproval.EntityId,
                    TestContext.Current.CancellationToken);

            // then
            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);

            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireClosedRoundApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.Verify(broker =>
                broker.FindRetirableApprovalReviewRequestIdsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The decision has COMMITTED by the time the retirement runs, and the entity command has
        /// gone with it. A failure on the tidy-up must not report that decision as a failure: the
        /// moderator would be advised to retry, and the retry is then refused because the round is
        /// no longer <c>Submitted</c> — a second, unrelated error on a round that was never
        /// broken.
        /// </summary>
        [Fact]
        public async Task ShouldLogAndNotFailTheDecisionWhenTheRetirementReadFailsAsync()
        {
            // given
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var retirementReadException = new Exception("storage is unhappy");

            Approval storageApproval = CreateDecisionApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            SetupDecisionSystemEnvelopes();
            List<EventEnvelope<Link>> publishedCommands = SetupDecisionLinkCommandPublish();
            List<Approval> savedApprovals = SetupDecisionApprovalRow(storageApproval);

            this.accessBrokerMock.Setup(broker =>
                broker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(retirementReadException);

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.DecideApprovalAsync(
                    EntityType.Link,
                    entityId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    TestContext.Current.CancellationToken);

            // then: the decision stands and reports itself honestly
            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            actualOutcome.IsEntitySyncRequested.Should().BeTrue();
            savedApprovals.Should().ContainSingle();
            publishedCommands.Should().ContainSingle();

            // Nothing is swallowed silently — the failure reaches the error log, which is the
            // whole of what this step may cost.
            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(retirementReadException),
                Times.Once);
        }

        /// <summary>
        /// The same posture one layer in: the read answered, and the WRITE failed. The rows stay
        /// outstanding and a moderator can still withdraw them by hand, which is a smaller cost
        /// than faulting a decision that fully worked.
        /// </summary>
        [Fact]
        public async Task ShouldLogAndNotFailTheDecisionWhenARetirementWriteFailsAsync()
        {
            // given
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var requestId = Guid.NewGuid();

            var retirementWriteException =
                new ApprovalReviewRequestDependencyException(
                    message: "Approval review request dependency error occurred, "
                        + "contact support.",
                    innerException: new Xeption(message: "storage is unhappy"));

            Approval storageApproval = CreateDecisionApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            SetupDecisionSystemEnvelopes();
            SetupDecisionLinkCommandPublish();
            List<Approval> savedApprovals = SetupDecisionApprovalRow(storageApproval);
            SetupRetirableApprovalReviewRequests(approvalId, requestId);

            this.approvalReviewRequestWorkflowServiceMock.Setup(service =>
                service.RetireClosedRoundApprovalReviewRequestAsync(
                    requestId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(retirementWriteException);

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.DecideApprovalAsync(
                    EntityType.Link,
                    entityId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    TestContext.Current.CancellationToken);

            // then
            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            savedApprovals.Should().ContainSingle();

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(retirementWriteException),
                Times.Once);
        }

        /// <summary>
        /// Cancellation is the one thing that passes through. A cancelled operation is not a
        /// failed one, and swallowing it here would leave the caller's own TryCatch unable to
        /// report the timeout it exists to report.
        /// </summary>
        [Fact]
        public async Task ShouldNotSwallowCancellationRaisedByTheRetirementAsync()
        {
            // given
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var operationCanceledException = new OperationCanceledException();

            Approval storageApproval = CreateDecisionApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            SetupDecisionSystemEnvelopes();
            SetupDecisionLinkCommandPublish();
            SetupDecisionApprovalRow(storageApproval);

            this.accessBrokerMock.Setup(broker =>
                broker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ApprovalOutcome> decideTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    EntityType.Link,
                    entityId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    TestContext.Current.CancellationToken);

            // then: it reaches the caller's TryCatch rather than the helper's catch, and is
            // reported as the timeout that it is
            await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                decideTask.AsTask);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(operationCanceledException),
                Times.Never);
        }

        // The workflow seam echoes back a retired row, so a test can assert on the argument and
        // on what came back and know they are the same row.
        private void SetupClosedRoundRetirement() =>
            this.approvalReviewRequestWorkflowServiceMock.Setup(service =>
                service.RetireClosedRoundApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Guid approvalReviewRequestId, CancellationToken _) =>
                            new ApprovalReviewRequest
                            {
                                Id = approvalReviewRequestId,
                                IsDeleted = true,
                            });
    }
}
