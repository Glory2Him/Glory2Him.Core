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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    /// <summary>
    /// §7.9 rule 8 from THIS side of the move — the round closes, and retires nothing itself.
    ///
    /// <para>The retirement is a subscription on <c>ApprovalReviewerOrchestrationService</c> now
    /// (§12.5.4 business rule 4), heard off the <c>Approval-Modified</c> that closing the round
    /// publishes. What these tests pin is the half that lives here: each of the three routes to
    /// an outcome writes it through <c>ModifyApprovalAsync</c>, which is what lets ONE
    /// subscription hear all three and is why no enumeration of the sites has to be kept in step
    /// with anything.</para>
    ///
    /// <para>The three ROUTES are still the point. A rule applied on the button and forgotten on
    /// the two automatic closes would leave the commonest rounds — the ones nobody clicks —
    /// exactly as cluttered as before; and a fourth route added later still reaches the
    /// subscription for free, provided it goes through the same write.</para>
    ///
    /// <para>Each also asserts that this service reaches for NO retirement: the gathering read is
    /// never made, which is the assertion that goes red if a direct call is ever put back. The
    /// workflow-seam assertions those calls carried moved to
    /// <c>ApprovalReviewerOrchestrationServiceTests.Substrate.cs</c>, where the write now
    /// happens — this service no longer holds <c>IApprovalReviewRequestWorkflowService</c> at
    /// all, so the compiler makes half the point the assertions were making.</para>
    /// </summary>
    public partial class ApprovalOrchestrationServiceTests
    {
        /// <summary>
        /// Route one: a person pressed Approve or Reject. Both outcomes close the round — a
        /// rejection is as final for an unanswered invitation as an approval — so both publish
        /// the fact the retirement hears.
        /// </summary>
        [Theory]
        [InlineData(ApprovalDecision.Approve, ApprovalStatus.Approved)]
        [InlineData(ApprovalDecision.Reject, ApprovalStatus.Rejected)]
        public async Task ShouldCloseTheRoundWithoutRetiringAnythingWhenAPersonDecidesTheRoundAsync(
            ApprovalDecision decision,
            ApprovalStatus expectedStatus)
        {
            // given
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

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

            // when
            await this.approvalOrchestrationService.DecideApprovalAsync(
                EntityType.Link,
                entityId,
                decision,
                false,
                null,
                TestContext.Current.CancellationToken);

            // then: the outcome is written through the modify, which is what publishes
            // Approval-Modified and so what the retirement's subscription hears
            savedApprovals.Should().ContainSingle();
            savedApprovals[0].ApprovalStatus.Should().Be(expectedStatus);

            VerifyNoRetirementWasReachedForHere();
        }

        /// <summary>
        /// Route two: nobody pressed anything — a standing rejection under <c>BlockOnReject</c>
        /// ended the round. There is no caller to attribute a retirement to even in principle,
        /// which is why it runs under the system identity the workflow seam mints on the other
        /// delivery.
        /// </summary>
        [Fact]
        public async Task ShouldCloseTheRoundWithoutRetiringAnythingWhenAStandingRejectionClosesTheRoundAsync()
        {
            // given
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

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

            // when
            await this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                approvalId,
                TestContext.Current.CancellationToken);

            // then
            savedApprovals.Should().ContainSingle();
            savedApprovals[0].ApprovalStatus.Should().Be(ApprovalStatus.Rejected);

            VerifyNoRetirementWasReachedForHere();
        }

        /// <summary>
        /// Route three, and the one that made the gathering seam a requirement rather than a
        /// preference. An automatic approval runs under the identity of whoever's review or edit
        /// tipped the round — frequently somebody with no review role at all — so a caller-facing
        /// read would answer them with nothing. That argument now belongs to the handler holding
        /// the read; what belongs here is that this route closes the round the same way the other
        /// two do.
        /// </summary>
        [Fact]
        public async Task ShouldCloseTheRoundWithoutRetiringAnythingWhenTheRoundAutoApprovesAsync()
        {
            // given
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

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

            // when
            await this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                approvalId,
                TestContext.Current.CancellationToken);

            // then
            savedApprovals.Should().ContainSingle();
            savedApprovals[0].ApprovalStatus.Should().Be(ApprovalStatus.Approved);

            VerifyNoRetirementWasReachedForHere();
        }

        /// <summary>
        /// The INVERSION of the ordering this file used to pin, and §7.9 rule 8 calls it "a
        /// deliberate inversion of what this rule used to say". The retirement used to run LAST,
        /// after the entity command had gone; as a reaction to the outcome write it runs BEFORE
        /// it, because delivery is synchronous inside <c>ModifyApprovalAsync</c>.
        ///
        /// <para><b>What is observed here stands in for the retirement, and deliberately.</b>
        /// This service no longer performs it, so a unit test of this service cannot watch it
        /// happen; what it can watch is the write the retirement hangs off. The outcome write
        /// publishes <c>Approval-Modified</c> and the subscription is delivered on that
        /// publisher's own thread, so "the outcome write happened first" IS "the retirement
        /// happened first". Move the write below the entity command and the inversion is
        /// undone — which is what this goes red for.</para>
        ///
        /// <para>Nothing renders between the two, so no surface shows a different thing. The old
        /// ordering existed to justify swallowing a failure on a decision that had already
        /// committed; that justification moved to §EVN23's delivery report with the retirement
        /// itself.</para>
        /// </summary>
        [Fact]
        public async Task ShouldRetireTheInvitationsBeforeTheEntityCommandHasGoneAsync()
        {
            // given
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            int decisionStep = 0;
            int entityCommandPublishedAt = 0;
            int outcomeWrittenAt = 0;

            Approval storageApproval = CreateDecisionApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            SetupDecisionSystemEnvelopes();

            SetupDecisionApprovalRow(
                storageApproval,
                onApprovalSaved: () => outcomeWrittenAt = ++decisionStep);

            SetupDecisionLinkCommandPublish(
                onCommandPublished: () => entityCommandPublishedAt = ++decisionStep);

            // when
            await this.approvalOrchestrationService.DecideApprovalAsync(
                EntityType.Link,
                entityId,
                ApprovalDecision.Approve,
                false,
                null,
                TestContext.Current.CancellationToken);

            // then
            outcomeWrittenAt.Should().Be(1);
            entityCommandPublishedAt.Should().Be(2);

            outcomeWrittenAt.Should().BeLessThan(entityCommandPublishedAt,
                because: "the retirement is a synchronous delivery on the outcome write, so it "
                    + "lands ahead of the entity command rather than after it");
        }

        /// <summary>
        /// The decision has COMMITTED by the time the retirement would run, and a failure in it
        /// must not report that decision as a failure — the moderator would be advised to retry,
        /// and the retry is then refused because the round is no longer <c>Submitted</c>.
        ///
        /// <para><b>Re-aimed at the delivery.</b> That guarantee used to be a hand-written
        /// <c>try</c>/<c>catch</c>/log inside this service; it is structural now, because the
        /// retirement is a different delivery on a different service and cannot reach this call
        /// stack at all. So a gathering seam that faults outright leaves the decision untouched
        /// AND unlogged here — the failure is recorded on the retirement's own delivery
        /// (§EVN23), and its assertions live in
        /// <c>ApprovalReviewerOrchestrationServiceTests.Substrate.Exceptions.cs</c>.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNotFailTheDecisionWhenTheRetirementReadFailsAsync()
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

            // and it never saw the failure, because it never reached for the read. Not a
            // swallowed exception — an untouched one.
            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(retirementReadException),
                Times.Never);

            VerifyNoRetirementWasReachedForHere();
        }

        // The one assertion every test in this file makes: the gathering read §7.9 rule 8 needs
        // is not reached from this service on any path. It is what goes red the moment a direct
        // call is put back, whichever of the three routes puts it there.
        private void VerifyNoRetirementWasReachedForHere() =>
            this.accessBrokerMock.Verify(broker =>
                broker.FindRetirableApprovalReviewRequestIdsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
    }
}
