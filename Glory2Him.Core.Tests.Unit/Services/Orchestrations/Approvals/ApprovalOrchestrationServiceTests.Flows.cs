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
using Force.DeepCloner;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    public partial class ApprovalOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldEvaluateOnTheConditionsAlreadyReadWhenTheStaleReviewResetIsOffAsync()
        {
            // given: RequireReapprovalOnChange is off, so the reviews already recorded still
            // stand and the verdict just read is the one to decide on (§9.7.4). A second read
            // would be the flow measuring a review set nothing changed.
            //
            // The second answer is ARMED to auto-approve, so a flow that re-read would approve a
            // row the first verdict said was blocked — and fail loudly rather than silently
            // agreeing with a helper that returned the same verdict twice.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupFlowApprovalRow(storageApproval);

            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    shouldResetStaleReviewsOnChange: false,
                    blockReasons: new List<AccessDenialReason>
                    {
                        AccessDenialReason.ApprovalThresholdNotMet,
                    }),
                secondConditions: CreateFlowConditions(
                    areConditionsMet: true,
                    shouldAutoApprove: true));

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    EntityType.Link,
                    entityId,
                    TestContext.Current.CancellationToken);

            // then: read ONCE, and evaluated against that one read.
            this.accessBrokerMock.Verify(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);


            this.approvalReviewServiceMock.Verify(service =>
                service.DismissStaleApprovalReviewAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalReviewServiceMock.VerifyNoOtherCalls();

            // Not the LISTING either, which VerifyNoOtherCalls above cannot cover — it now lives
            // on the access broker, whose other members this flow legitimately calls. With the
            // reset off there is nothing to dismiss, so reading the whole round unfiltered is
            // work done to throw away.
            this.accessBrokerMock.Verify(broker =>
                broker.FindDismissableApprovalReviewIdsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            actualOutcome.ApprovalStatus.Should().NotBe(ApprovalStatus.Approved);
            actualOutcome.IsEntitySyncRequested.Should().BeFalse();

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldDismissTheStaleReviewsAndReReadTheConditionsWhenTheResetIsOnAsync()
        {
            // given: RequireReapprovalOnChange is on, so every active review for this approval is
            // dismissed and the conditions are RE-READ before anything is decided (§9.7.4). The
            // first verdict was measured against reviews that no longer count — evaluating on it
            // would auto-approve using approvals the flow has just discarded, exactly inverting
            // what the setting asked for.
            //
            // The two answers therefore differ: the first auto-approves, the second does not. A
            // flow that skipped the re-read approves, and this fails.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            int flowStep = 0;
            var conditionsReadSteps = new List<int>();
            var reviewDismissedSteps = new List<int>();

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            // Two of them, and with different verdicts, because dismissal is entity-scoped
            // rather than verdict-selective — a rejection stops counting on an edit the same way
            // an approval does.
            var firstActiveReviewId = Guid.NewGuid();
            var secondActiveReviewId = Guid.NewGuid();

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupFlowApprovalRow(storageApproval);

            List<Guid> dismissedReviewIds = SetupFlowApprovalReviews(
                approvalReviews: new List<ApprovalReview>
                {
                    CreateFlowApprovalReview(
                        approvalReviewId: firstActiveReviewId,
                        approvalId: approvalId,
                        statusId: ApprovalStatus.Approved),

                    CreateFlowApprovalReview(
                        approvalReviewId: secondActiveReviewId,
                        approvalId: approvalId,
                        statusId: ApprovalStatus.Rejected),
                },
                onReviewDismissed: () => reviewDismissedSteps.Add(++flowStep));

            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    areConditionsMet: true,
                    shouldAutoApprove: true,
                    shouldResetStaleReviewsOnChange: true,
                    approvalCount: 2,
                    requiredNumberOfApprovals: 2),

                secondConditions: CreateFlowConditions(
                    shouldResetStaleReviewsOnChange: true,
                    blockReasons: new List<AccessDenialReason>
                    {
                        AccessDenialReason.ApprovalThresholdNotMet,
                    },
                    approvalCount: 0,
                    requiredNumberOfApprovals: 2),

                onConditionsRead: () => conditionsReadSteps.Add(++flowStep));

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    EntityType.Link,
                    entityId,
                    TestContext.Current.CancellationToken);

            // then: TWICE, addressed by the approval's own id both times.
            this.accessBrokerMock.Verify(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            dismissedReviewIds.Should()
                .BeEquivalentTo(new[] { firstActiveReviewId, secondActiveReviewId });

            // The order is what the correctness rests on, so it is observed rather than inferred
            // from the calls merely having happened: read, dismiss both, read again.
            conditionsReadSteps.Should().Equal(1, 4);
            reviewDismissedSteps.Should().Equal(2, 3);

            // and the SECOND verdict is the one decided on. The first would have approved.
            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            actualOutcome.ApprovalStatus.Should().NotBe(ApprovalStatus.Approved);
            actualOutcome.IsEntitySyncRequested.Should().BeFalse();

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldDismissOnlyTheActiveReviewsBelongingToThisApprovalAsync()
        {
            // given: the listing is every review in the store, so the filter is the whole of the
            // protection. Three rows are seeded that must survive it, and each fails differently
            // if it does not — another approval's review would be dismissed by an edit to an
            // entity it says nothing about; an already-dismissed one would be dismissed twice,
            // which the transition refuses outright; and a soft-deleted one would be resurrected
            // into the workflow to be written again.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var otherApprovalId = Guid.NewGuid();
            var activeReviewId = Guid.NewGuid();
            var otherApprovalReviewId = Guid.NewGuid();
            var alreadyDismissedReviewId = Guid.NewGuid();
            var deletedReviewId = Guid.NewGuid();

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupFlowApprovalRow(storageApproval);

            List<Guid> dismissedReviewIds = SetupFlowApprovalReviews(
                approvalReviews: new List<ApprovalReview>
                {
                    CreateFlowApprovalReview(
                        approvalReviewId: activeReviewId,
                        approvalId: approvalId,
                        statusId: ApprovalStatus.Approved),

                    CreateFlowApprovalReview(
                        approvalReviewId: otherApprovalReviewId,
                        approvalId: otherApprovalId,
                        statusId: ApprovalStatus.Approved),

                    CreateFlowApprovalReview(
                        approvalReviewId: alreadyDismissedReviewId,
                        approvalId: approvalId,
                        statusId: ApprovalStatus.Dismissed),

                    CreateFlowApprovalReview(
                        approvalReviewId: deletedReviewId,
                        approvalId: approvalId,
                        statusId: ApprovalStatus.Approved,
                        isDeleted: true),
                });

            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    shouldResetStaleReviewsOnChange: true),

                secondConditions: CreateFlowConditions(
                    shouldResetStaleReviewsOnChange: true));

            // when
            await this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                EntityType.Link,
                entityId,
                TestContext.Current.CancellationToken);

            // then
            dismissedReviewIds.Should().ContainSingle();
            dismissedReviewIds.Single().Should().Be(activeReviewId);

            this.approvalReviewServiceMock.Verify(service =>
                service.DismissStaleApprovalReviewAsync(
                    otherApprovalReviewId,
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalReviewServiceMock.Verify(service =>
                service.DismissStaleApprovalReviewAsync(
                    alreadyDismissedReviewId,
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalReviewServiceMock.Verify(service =>
                service.DismissStaleApprovalReviewAsync(
                    deletedReviewId,
                    It.IsAny<CancellationToken>()),
                Times.Never);


        }

        [Theory]
        [InlineData(ApprovalStatus.Draft, false)]
        [InlineData(ApprovalStatus.Draft, true)]
        [InlineData(ApprovalStatus.Submitted, false)]
        [InlineData(ApprovalStatus.Submitted, true)]
        public async Task ShouldNeverMoveTheApprovalStatusOnTheModifiedFlowAsync(
            ApprovalStatus approvalStatus,
            bool shouldResetStaleReviews)
        {
            // given: an edit is not a submission and not a withdrawal. A Draft stays Draft —
            // offering content for review is somebody's decision rather than a side effect of
            // editing it (§9.2) — and a Submitted row stays Submitted, because the edit re-opens
            // the round rather than closing it (§9.7.4). The invariant holds in BOTH arms of the
            // reset, so the flag is a dimension of the theory rather than a separate case.
            //
            // THE BRANCH IS ARMED, which the earlier version of this test was not. It asked for
            // conditions that write nothing, so the flow had no reachable write at all and the
            // Times.Never below was true of the ARRANGEMENT rather than of the code: a status
            // mover would have had to invent its own save for the assertion to notice.
            //
            // Here the approval arrives CLOSED, so the resolution genuinely reinstates it in place
            // (§9.7.2 rule 2) — one real write, on this flow, whose payload is captured. The
            // status carried into it is the assertion. What each arm now catches:
            //
            //  * a flow that submits a Draft on edit, or withdraws a Submitted row to Draft: the
            //    moved status rides into the reinstate write and the SNAPSHOT names it, and a
            //    mover that added a save of its own breaks Times.Once.
            //  * a flow that branches on the PROBE's projected status instead of the stored row's:
            //    the projection carries a decoy Approved that the stored row never had, so reading
            //    the wrong one changes the outcome.
            //  * a flow that skips the re-read after dismissing: the two verdicts differ, so the
            //    second read cannot be satisfied by repeating the first.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var staleReviewId = Guid.NewGuid();

            // The status the flow must never write on THIS row — the other end of the submit /
            // withdraw pair the §9.2 and §9.7.4 rules forbid.
            ApprovalStatus forbiddenStatus = approvalStatus == ApprovalStatus.Draft
                ? ApprovalStatus.Submitted
                : ApprovalStatus.Draft;

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: approvalStatus);

            storageApproval.IsDeleted = true;

            // The projection deliberately DISAGREES with the row it points at. Only IsDeleted is
            // the resolution's business here; a flow reading the status off the probe would be
            // reading Approved for a row that is nothing of the kind.
            SetupApprovalProbe(new ApprovalEntityMatch
            {
                Id = approvalId,
                ApprovalStatus = ApprovalStatus.Approved,
                IsDeleted = true,
            });

            List<Approval> savedApprovals = SetupFlowApprovalRow(storageApproval);

            List<Guid> dismissedReviewIds = SetupFlowApprovalReviews(
                approvalReviews: new List<ApprovalReview>
                {
                    CreateFlowApprovalReview(
                        approvalReviewId: staleReviewId,
                        approvalId: approvalId,
                        statusId: ApprovalStatus.Approved),
                });

            // Neither verdict auto-approves — §9.7.7's own write is a different rule and is
            // covered elsewhere — and the two differ so a skipped re-read is visible.
            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    areConditionsMet: true,
                    shouldResetStaleReviewsOnChange: shouldResetStaleReviews,
                    approvalCount: 2,
                    requiredNumberOfApprovals: 5),

                secondConditions: CreateFlowConditions(
                    shouldResetStaleReviewsOnChange: shouldResetStaleReviews,
                    blockReasons: new List<AccessDenialReason>
                    {
                        AccessDenialReason.ApprovalThresholdNotMet,
                    },
                    approvalCount: 0,
                    requiredNumberOfApprovals: 5));

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    EntityType.Link,
                    entityId,
                    TestContext.Current.CancellationToken);

            // then: EXACTLY ONE write happened, and it is the reinstatement — which is what makes
            // the status claim below a claim about the code rather than about the arrangement.
            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            savedApprovals.Should().ContainSingle();
            Approval savedApproval = savedApprovals.Single();

            savedApproval.IsDeleted.Should().BeFalse();
            savedApproval.Id.Should().Be(approvalId);

            // The status went into that write untouched. This is the whole invariant.
            savedApproval.ApprovalStatus.Should().Be(approvalStatus);
            savedApproval.ApprovalStatus.Should().NotBe(forbiddenStatus);
            savedApproval.ApprovalStatus.Should().NotBe(ApprovalStatus.Approved);

            actualOutcome.ApprovalStatus.Should().Be(approvalStatus);
            actualOutcome.ApprovalStatus.Should().NotBe(forbiddenStatus);
            actualOutcome.IsEntitySyncRequested.Should().BeFalse();

            // The reset flag drives the review reads and the second conditions read together, so
            // both are counted: an arm that dismissed without re-reading, or re-read without
            // dismissing, is a different bug and each shows up here.
            dismissedReviewIds.Should()
                .HaveCount(shouldResetStaleReviews ? 1 : 0);

            this.accessBrokerMock.Verify(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                shouldResetStaleReviews ? Times.Exactly(2) : Times.Once());

            // and no command reaches the entity, whose own ApprovalStatus must not move either.
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<Link>()),
                Times.Never);

            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRejectImmediatelyOnAStandingRejectionWithoutEvaluatingAsync()
        {
            // given: a rejection under BlockOnReject ends the round IMMEDIATELY — independent of
            // the approval threshold, and even where approvals have already been recorded (§9.7.5
            // rule 2). The verdict therefore carries a MET threshold and auto-approve ON alongside
            // the block: a flow that fell through to the evaluation would approve the very round
            // a reviewer just rejected, which is the failure this pins.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            List<Approval> savedApprovals = SetupFlowApprovalRow(storageApproval);
            SetupFlowSystemEnvelope<Link>();
            List<EventEnvelope<Link>> publishedCommands = SetupFlowLinkCommandPublish();

            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    areConditionsMet: true,
                    shouldAutoApprove: true,
                    blockReasons: new List<AccessDenialReason>
                    {
                        AccessDenialReason.BlockedByRejection,
                    },
                    approvalCount: 3,
                    requiredNumberOfApprovals: 2));

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                    approvalId,
                    TestContext.Current.CancellationToken);

            // then: a SNAPSHOT of the saved row, because the service mutates the instance it
            // retrieved and hands that same object to the save.
            savedApprovals.Should().ContainSingle();
            Approval savedApproval = savedApprovals.Single();

            savedApproval.Id.Should().Be(approvalId);
            savedApproval.ApprovalStatus.Should().Be(ApprovalStatus.Rejected);
            savedApproval.ApprovalStatus.Should().NotBe(ApprovalStatus.Approved);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // ONE read, and no second one: the rejection branch decides without evaluating.
            this.accessBrokerMock.Verify(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // The entity is told, so §9.8's two records do not diverge — and it is told Rejected.
            publishedCommands.Should().ContainSingle();
            Link publishedLink = publishedCommands.Single().Content;

            publishedLink.Id.Should().Be(entityId);
            publishedLink.Id.Should().NotBe(approvalId);
            publishedLink.ApprovalStatus.Should().Be(ApprovalStatus.Rejected);

            // Rejection leaves any previously published version of the group exactly where it
            // was; visibility is gated by ApprovalStatus rather than by unpublishing (§14.1).
            publishedLink.IsPublished.Should().BeFalse();
            publishedLink.PublishDate.Should().BeNull();

            this.eventBrokerMock.Verify(broker =>
                broker.PublishLinkProcessingAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    LinkProcessingEventOperation.Approving),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();

            actualOutcome.ApprovalId.Should().Be(approvalId);
            actualOutcome.EntityId.Should().Be(entityId);
            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Rejected);

            // REQUESTED, not confirmed — the command travels as an event (§16.7.1).
            actualOutcome.IsEntitySyncRequested.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldClearTheBypassPairWhenTheStandingRejectionEndsTheRoundAsync()
        {
            // given: the row arrives bypass-approved from an earlier round that was re-opened, so
            // clearing the pair is observed rather than coinciding with fields that were already
            // empty. A rejection withholds approval rather than granting it, so nothing is waived
            // and the pair is CLEARED — a row rejected while still claiming a waiver would answer
            // "what was approved without meeting its conditions" with a row that was not approved
            // at all (§9.7.5 rule 2).
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted,
                isApprovedByBypass: true,
                approvedByBypassReason: "stale bypass reason");

            List<Approval> savedApprovals = SetupFlowApprovalRow(storageApproval);
            SetupFlowSystemEnvelope<Link>();
            List<EventEnvelope<Link>> publishedCommands = SetupFlowLinkCommandPublish();

            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    blockReasons: new List<AccessDenialReason>
                    {
                        AccessDenialReason.BlockedByRejection,
                    }));

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                    approvalId,
                    TestContext.Current.CancellationToken);

            // then
            Approval savedApproval = savedApprovals.Single();

            savedApproval.IsApprovedByBypass.Should().BeFalse();
            savedApproval.ApprovedByBypassReason.Should().BeNull();

            // The reason must be CLEARED alongside the flag. A row reading "not bypassed" beside
            // a bypass reason is the record contradicting itself.
            savedApproval.ApprovedByBypassReason.Should().NotBe("stale bypass reason");

            // and the stale waiver does not travel to the entity either.
            Link publishedLink = publishedCommands.Single().Content;
            publishedLink.IsApprovedByBypass.Should().BeFalse();
            publishedLink.ApprovedByBypassReason.Should().BeNull();

            actualOutcome.IsApprovedByBypass.Should().BeFalse();
            actualOutcome.ApprovedByBypassReason.Should().BeNull();
        }

        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldEndTheReviewFlowWithoutWritesWhenTheApprovalIsNotSubmittedAsync(
            ApprovalStatus approvalStatus)
        {
            // given: a review recorded against a round that is not open decides nothing. A Draft
            // has not entered one and a terminal row has left it — deciding either would re-decide
            // an approval already decided, or resolve policy for a round nobody is running. The
            // gates on recording the review are the review service's; this flow only reacts.
            //
            // The conditions are armed to auto-approve, so a short-circuit that leaked would
            // approve and be caught.
            //
            // ARMED ON THE OTHER SIDE TOO, which it was not before: the row arrives carrying a
            // WAIVER PAIR that the flow has no business changing, and the sync path is stubbed so
            // a leaked gate publishes a command this test can name rather than dying on an
            // unstubbed broker. What each now catches:
            //
            //  * a flow that describes the outcome from a freshly built Approval rather than the
            //    stored row: the pinned bypass reason is the only place that value exists, so a
            //    default-constructed answer reads null and fails.
            //  * a flow that clears or rewrites the waiver on a round it is not deciding: same
            //    assertion, from the other direction — the pair must arrive unchanged.
            //  * a gate that leaks: the published-command list is empty rather than merely
            //    un-thrown, so the failure names what was announced.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            // Captured BEFORE the act. The flow hands its own row onward, so an assertion made
            // against that object afterwards would be comparing it with itself.
            const string expectedBypassReason = "waiver recorded on an earlier round";

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: approvalStatus,
                isApprovedByBypass: true,
                approvedByBypassReason: expectedBypassReason);

            SetupFlowApprovalRow(storageApproval);
            SetupFlowSystemEnvelope<Link>();
            List<EventEnvelope<Link>> publishedCommands = SetupFlowLinkCommandPublish();

            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    areConditionsMet: true,
                    shouldAutoApprove: true));

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                    approvalId,
                    TestContext.Current.CancellationToken);

            // then
            actualOutcome.ApprovalId.Should().Be(approvalId);
            actualOutcome.EntityId.Should().Be(entityId);
            actualOutcome.EntityType.Should().Be(EntityType.Link);
            actualOutcome.ApprovalStatus.Should().Be(approvalStatus);
            actualOutcome.ApprovalStatus.Should().NotBe(ApprovalStatus.Submitted);

            // The waiver is reported exactly as stored — neither invented nor discarded by a flow
            // that decided nothing.
            actualOutcome.IsApprovedByBypass.Should().BeTrue();
            actualOutcome.ApprovedByBypassReason.Should().Be(expectedBypassReason);

            // Nothing was asked of the entity, so nothing is claimed to have been asked — and
            // nothing was in fact sent.
            actualOutcome.IsEntitySyncRequested.Should().BeFalse();
            publishedCommands.Should().BeEmpty();

            // No policy is resolved at all — the status question is answered off the stored row.
            this.accessBrokerMock.Verify(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.VerifyNoOtherCalls();

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<Link>()),
                Times.Never);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldApproveOnTheReviewFlowWhenTheConditionsAreMetAndAutoApproveIsOnAsync()
        {
            // given: the review just recorded carried the round over the line, nothing blocks, and
            // the policy asks for the click to be skipped — so Approved is applied without a human
            // (§9.7.7 rule 4). The waiver pair reads false/null because an automatic approval
            // fires precisely BECAUSE the conditions were met.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            List<Approval> savedApprovals = SetupFlowApprovalRow(storageApproval);
            SetupFlowSystemEnvelope<Link>();
            List<EventEnvelope<Link>> publishedCommands = SetupFlowLinkCommandPublish();

            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    areConditionsMet: true,
                    shouldAutoApprove: true,
                    approvalCount: 2,
                    requiredNumberOfApprovals: 2));

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                    approvalId,
                    TestContext.Current.CancellationToken);

            // then
            Approval savedApproval = savedApprovals.Single();

            savedApproval.Id.Should().Be(approvalId);
            savedApproval.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            savedApproval.IsApprovedByBypass.Should().BeFalse();
            savedApproval.ApprovedByBypassReason.Should().BeNull();

            Link publishedLink = publishedCommands.Single().Content;
            publishedLink.Id.Should().Be(entityId);
            publishedLink.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            publishedLink.IsPublished.Should().BeTrue();

            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            actualOutcome.IsEntitySyncRequested.Should().BeTrue();
        }

        private static Approval CreateFlowApproval(
            Guid approvalId,
            Guid entityId,
            EntityType entityType,
            ApprovalStatus approvalStatus,
            bool isApprovedByBypass = false,
            string approvedByBypassReason = null) =>
            new Approval
            {
                Id = approvalId,
                EntityType = entityType,
                EntityId = entityId,
                ApprovalStatus = approvalStatus,
                IsApprovedByBypass = isApprovedByBypass,
                ApprovedByBypassReason = approvedByBypassReason,
                IsDeleted = false,
            };

        private static ApprovalReview CreateFlowApprovalReview(
            Guid approvalReviewId,
            Guid approvalId,
            ApprovalStatus statusId,
            bool isDeleted = false) =>
            new ApprovalReview
            {
                Id = approvalReviewId,
                ApprovalId = approvalId,
                StatusId = statusId,
                IsDeleted = isDeleted,
            };

        // Every field of the §8.5 answer is under the test's control, because the two flows here
        // turn on combinations the shared makers cannot express — a met threshold sitting beside
        // a standing rejection, or a reset flag on a verdict that also auto-approves.
        /// <summary>
        /// A Draft round is never auto-approved, even with the auto-approve branch ARMED.
        /// </summary>
        /// <remarks>
        /// <para>The sibling theory above proves the flow does not MOVE a status; this proves it
        /// does not APPROVE one. They are different failures: that one arms a reinstate write,
        /// this one arms the auto-approve branch, which is the only place this flow writes
        /// <c>Approved</c>.</para>
        ///
        /// <para><c>ProcessEntityModifiedAsync</c> is the one caller of
        /// <c>EvaluateApprovalAsync</c> with no round-open check of its own, and a Draft round's
        /// conditions can be met — so before the guard, editing a draft with a permissive policy
        /// composed an approval the foundation then had to refuse. The foundation still refuses
        /// it; this stops the flow asking.</para>
        /// </remarks>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ShouldNeverAutoApproveADraftRoundOnTheModifiedFlowAsync(
            bool shouldResetStaleReviews)
        {
            // given: conditions MET and auto-approve ON — the branch is armed, so only the
            // round-open test stands between this flow and an Approved write
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var staleReviewId = Guid.NewGuid();

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Draft);

            SetupApprovalProbe(new ApprovalEntityMatch
            {
                Id = approvalId,
                ApprovalStatus = ApprovalStatus.Draft,
                IsDeleted = false,
            });

            List<Approval> savedApprovals = SetupFlowApprovalRow(storageApproval);

            SetupFlowApprovalReviews(
                approvalReviews: new List<ApprovalReview>
                {
                    CreateFlowApprovalReview(
                        approvalReviewId: staleReviewId,
                        approvalId: approvalId,
                        statusId: ApprovalStatus.Approved),
                });

            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    areConditionsMet: true,
                    shouldAutoApprove: true,
                    shouldResetStaleReviewsOnChange: shouldResetStaleReviews),
                secondConditions: CreateFlowConditions(
                    areConditionsMet: true,
                    shouldAutoApprove: true));

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    EntityType.Link,
                    entityId,
                    TestContext.Current.CancellationToken);

            // then
            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Draft);
            actualOutcome.ApprovalStatus.Should().NotBe(ApprovalStatus.Approved);
            actualOutcome.IsEntitySyncRequested.Should().BeFalse();

            savedApprovals.Should().NotContain(
                approval => approval.ApprovalStatus == ApprovalStatus.Approved,
                because: "a draft round has not been offered for review, so there is no "
                    + "decision to make on it — however permissive the policy");
        }

        /// <summary>
        /// §9.2 rules 3 and 6, §9.8: the carve-out moves the ENTITY between Draft and Submitted
        /// through a modify, and the round follows it in the same branch — in either direction.
        /// A submission is then evaluated as one; a withdrawal is not evaluated at all.
        /// </summary>
        [Theory]
        [InlineData(ApprovalStatus.Draft, ApprovalStatus.Submitted)]
        [InlineData(ApprovalStatus.Submitted, ApprovalStatus.Draft)]
        public async Task ShouldMoveTheRoundWithTheEntityWhenTheCarveOutMovedItAsync(
            ApprovalStatus storedApprovalStatus,
            ApprovalStatus entityApprovalStatus)
        {
            // given: a LIVE round at one end of the pair, and an entity the owner has just moved
            // to the other end through the general modify
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: storedApprovalStatus);

            SetupApprovalProbe(CreateApprovalMatch(storedApprovalStatus, approvalId));
            List<Approval> savedApprovals = SetupFlowApprovalRow(storageApproval);
            SetupEntityApprovalStatus(EntityType.Link, entityId, entityApprovalStatus);

            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    shouldResetStaleReviewsOnChange: false,
                    blockReasons: new List<AccessDenialReason>
                    {
                        AccessDenialReason.ApprovalThresholdNotMet,
                    }),
                secondConditions: CreateFlowConditions());

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    EntityType.Link,
                    entityId,
                    TestContext.Current.CancellationToken);

            // then: ONE write, the follow, carrying the entity's status — as the workflow
            savedApprovals.Should().ContainSingle().Subject
                .ApprovalStatus.Should().Be(entityApprovalStatus);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.Is<Approval>(approval =>
                        approval.Id == approvalId
                            && approval.ApprovalStatus == entityApprovalStatus),
                    WorkflowAttribution.System,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            actualOutcome.ApprovalStatus.Should().Be(entityApprovalStatus);
        }

        /// <summary>
        /// Only the Draft ↔ Submitted pair follows, and only when the entity actually sits at one
        /// of them: a status that could not be read, one that agrees with the round already, or
        /// one no modify may carry (a terminal status, §9.7.1) moves nothing.
        /// </summary>
        [Theory]
        [InlineData(ApprovalStatus.Draft, null)]
        [InlineData(ApprovalStatus.Draft, ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Draft, ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Submitted, ApprovalStatus.Submitted)]
        [InlineData(ApprovalStatus.Submitted, ApprovalStatus.Rejected)]
        public async Task ShouldNotMoveTheRoundWhenTheEntityDidNotCrossThePairAsync(
            ApprovalStatus storedApprovalStatus,
            ApprovalStatus? entityApprovalStatus)
        {
            // given
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: storedApprovalStatus);

            SetupApprovalProbe(CreateApprovalMatch(storedApprovalStatus, approvalId));
            List<Approval> savedApprovals = SetupFlowApprovalRow(storageApproval);
            SetupEntityApprovalStatus(EntityType.Link, entityId, entityApprovalStatus);

            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    shouldResetStaleReviewsOnChange: false,
                    blockReasons: new List<AccessDenialReason>
                    {
                        AccessDenialReason.ApprovalThresholdNotMet,
                    }),
                secondConditions: CreateFlowConditions());

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    EntityType.Link,
                    entityId,
                    TestContext.Current.CancellationToken);

            // then
            savedApprovals.Should().BeEmpty();
            actualOutcome.ApprovalStatus.Should().Be(storedApprovalStatus);
        }

        private static ApprovalConditionsVerdict CreateFlowConditions(
            bool areConditionsMet = false,
            bool shouldAutoApprove = false,
            bool shouldResetStaleReviewsOnChange = false,
            IReadOnlyList<AccessDenialReason> blockReasons = null,
            int approvalCount = 1,
            int requiredNumberOfApprovals = 4,
            int unresolvedApprovalCommentCount = 0)
        {
            IReadOnlyList<AccessDenialReason> resolvedBlockReasons =
                blockReasons ?? new List<AccessDenialReason>();

            return new ApprovalConditionsVerdict
            {
                AreConditionsMet = areConditionsMet,
                ShouldAutoApprove = shouldAutoApprove,
                ShouldResetStaleReviewsOnChange = shouldResetStaleReviewsOnChange,

                BlockReason = resolvedBlockReasons.Count == 0
                    ? AccessDenialReason.None
                    : resolvedBlockReasons[0],

                BlockReasons = resolvedBlockReasons,
                ApprovalCount = approvalCount,
                RequiredNumberOfApprovals = requiredNumberOfApprovals,
                UnresolvedApprovalCommentCount = unresolvedApprovalCommentCount,
                Explanation = GetRandomString(),
            };
        }

        // The two reads answer DIFFERENTLY on purpose. A helper handing the same verdict to both
        // could not tell a flow that re-read from one that evaluated on the reading it already
        // had — which is the whole of what §9.7.4's dismissal branch turns on. A read past the
        // supplied answers is left to surface as not-found rather than quietly repeating the last
        // one, so an unexpected third read is loud.
        private void SetupFlowConditionsReads(
            ApprovalConditionsVerdict firstConditions,
            ApprovalConditionsVerdict secondConditions = null,
            Action onConditionsRead = null)
        {
            var pendingConditions = new Queue<ApprovalConditionsVerdict>();
            pendingConditions.Enqueue(firstConditions);

            if (secondConditions is not null)
            {
                pendingConditions.Enqueue(secondConditions);
            }

            this.accessBrokerMock.Setup(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .Returns((Guid approvalId, CancellationToken cancellationToken) =>
                        {
                            onConditionsRead?.Invoke();

                            return new ValueTask<ApprovalConditionsVerdict>(
                                pendingConditions.Count > 0
                                    ? pendingConditions.Dequeue()
                                    : null);
                        });
        }

        // The save is captured as a SNAPSHOT rather than as the instance handed to it. The service
        // mutates the row it retrieved and passes that same object on, so a test holding the
        // original would be reading whatever the service wrote into it and asserting against
        // itself. The clone is also what travels onward, matching a real store returning its own
        // copy of the row.
        private List<Approval> SetupFlowApprovalRow(Approval storageApproval)
        {
            var savedApprovals = new List<Approval>();

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            this.approvalServiceMock.Setup(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()))
                        .Returns((Approval approval, WorkflowAttribution attribution, CancellationToken cancellationToken) =>
                        {
                            Approval savedApproval = approval.DeepClone();
                            savedApprovals.Add(savedApproval);

                            return new ValueTask<Approval>(savedApproval);
                        });

            return savedApprovals;
        }

        // The listing answers with EVERY review in the store, as the real one does, and the
        // fixture then MIRRORS the broker's dismissability predicate to derive what the flow
        // would be told.
        //
        // That mirror is a copy, so nothing here can catch the predicate drifting — these tests
        // prove the flow ASKS the right seam and acts on the answer, not that the answer is
        // right. The predicate itself is pinned where it lives, against a real AccessBroker over
        // a seeded storage broker: AccessBrokerTests.FindDismissableApprovalReviewIds.Logic.cs.
        [Fact]
        public async Task ShouldDismissStaleReviewsTheEditorCannotSeeOnEntityModifiedAsync()
        {
            // given: an author revising their own submitted content — the ordinary case, and
            // the one every other test here misses.
            //
            // The workflow runs under the editor's identity, and the caller-facing review read
            // is identity-filtered: an actor holding no review role sees only reviews they
            // wrote themselves. HR-1 forbids reviewing your own content, so an author sees
            // NOTHING — the round's real approvals are invisible to them.
            //
            // The evaluation that follows reads storage UNFILTERED. So if the dismissal half
            // reads the filtered view, the two halves of one decision disagree: nothing is
            // dismissed, nothing throws, and the round is then approved on the strength of a
            // review of the text the author just replaced. That is §9.7.4 inverted, and it
            // fails OPEN.
            var entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            var reviewTheEditorCannotSeeId = Guid.NewGuid();

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Tag,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupFlowApprovalRow(storageApproval);

            // The editor's view: empty. Not because the round has no reviews, but because this
            // caller may not see the ones it has.
            List<Guid> dismissedReviewIds = SetupFlowApprovalReviews(
                approvalReviews: new List<ApprovalReview>());

            // What storage actually holds, which is what the decision must be made against.
            SetupDismissableReviews(approvalId, reviewTheEditorCannotSeeId);

            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    shouldResetStaleReviewsOnChange: true),

                secondConditions: CreateFlowConditions(
                    shouldResetStaleReviewsOnChange: true));

            // when
            await this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                EntityType.Tag,
                entityId,
                TestContext.Current.CancellationToken);

            // then
            dismissedReviewIds.Should().Equal(new[] { reviewTheEditorCannotSeeId },
                because: "what a round's reviews ARE is a fact about storage, not about who is " +
                    "asking. An identity-filtered read must never decide an invariant — the " +
                    "evaluation that follows reads unfiltered, so a filtered dismissal lets " +
                    "content be approved on reviews of text it no longer matches");
        }

        /// <summary>
        /// THE EDIT PATH TAKES BEREAN'S VERDICT BACK TOO. §8.8 rule 1's
        /// <c>RequireReapprovalOnChange</c> dismisses every human review, and Berean's assignment
        /// is keyed on the APPROVAL rather than on those reviews — so without this it survives the
        /// edit untouched and goes on reporting a finished pass, with comments, over text it never
        /// saw. §8.6.2's re-trigger event is not built, so nothing else would correct it.
        ///
        /// <para><b>THE POINT OF THE WHOLE CHANGE, and what this test catches:</b> the editor
        /// holds NO review role. That is the ordinary case — an author revising their own
        /// submission, who cannot hold one, because HR-1 forbids reviewing your own content. Route
        /// either half of this through the caller-facing <c>AIReviewerAssignment</c> foundation
        /// and it silently does nothing for this caller: the round-keyed read answers null and
        /// logs a denial, and the modify gate refuses outright. A version of this test that ran as
        /// an administrator would pass against exactly that broken code, which is why the identity
        /// below is arranged and not left at the fixture's default publisher.</para>
        ///
        /// <para>The unfiltered gather and the system-identity workflow seam are the same pair the
        /// human dismissal already uses, and the last assertion is what holds them there: the
        /// caller-facing foundation is not touched at all on this path.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnBereansAssignmentToPendingWhenAnEditDismissesTheReviewsAsync()
        {
            // given: a plain author revising their own submitted content, holding no review role
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var staleReviewId = Guid.NewGuid();
            var staleAssignmentId = Guid.NewGuid();

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupFlowApprovalRow(storageApproval);
            SetupDismissableReviews(approvalId, staleReviewId);
            SetupResettableAIReviewerAssignment(approvalId, staleAssignmentId);
            SetupAIReviewerAssignmentReturnToPending();

            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    shouldResetStaleReviewsOnChange: true),

                secondConditions: CreateFlowConditions(
                    shouldResetStaleReviewsOnChange: true));

            // when
            await this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                EntityType.Link,
                entityId,
                TestContext.Current.CancellationToken);

            // then: the human half ran
            this.approvalReviewServiceMock.Verify(service =>
                service.DismissStaleApprovalReviewAsync(
                    staleReviewId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // and so did the AI half — the round was asked what it holds, unfiltered
            this.accessBrokerMock.Verify(broker =>
                broker.FindResettableAIReviewerAssignmentIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // and the id it named went back through the seam that mints the system identity
            // itself, which is the only identity that can perform this write
            this.aiReviewerAssignmentWorkflowServiceMock.Verify(service =>
                service.ReturnStaleAIReviewerAssignmentToPendingAsync(
                    staleAssignmentId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // AND NOTHING ELSE ON THAT SEAM. The caller-facing foundation — whose read is
            // identity-filtered and whose write is review-tier gated, so for this caller both
            // would fail quietly — is no longer reachable from this service at all: it left with
            // IAIReviewerOrchestrationService, and the constructor no longer takes it. What used
            // to need an assertion the compiler now refuses outright.
            this.aiReviewerAssignmentWorkflowServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Silent when there is nothing to take back — a round Berean was never asked about, which
        /// is the overwhelmingly common case, and one whose assignment is already pending.
        ///
        /// <para>Both arrive here as the same <c>null</c>, because the staleness predicate lives
        /// in the gather rather than in this flow. Which row is which is pinned where that
        /// predicate lives (AccessBrokerTests.FindResettableAIReviewerAssignmentId.Logic.cs), and
        /// the transition's own refusal to rewrite an already-pending row in
        /// AIReviewerAssignmentServiceTests.ReturnToPending.cs.</para>
        ///
        /// <para><b>What it catches.</b> Calling the seam on the <c>Guid?</c> without checking it:
        /// <c>Guid.Empty</c> would go through the transition and fault the edit flow of every
        /// entity in the system that has no AI reviewer — which is nearly all of them.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNotTouchBereansAssignmentOnEditWhenNothingIsStaleAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var staleReviewId = Guid.NewGuid();

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupFlowApprovalRow(storageApproval);
            SetupDismissableReviews(approvalId, staleReviewId);
            SetupAIReviewerAssignmentReturnToPending();

            // Nothing to take back. Moq's default for ValueTask<Guid?> is already null, but this
            // states it: the whole point of the case is the absence.
            SetupResettableAIReviewerAssignment(approvalId, aiReviewerAssignmentId: null);

            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    shouldResetStaleReviewsOnChange: true),

                secondConditions: CreateFlowConditions(
                    shouldResetStaleReviewsOnChange: true));

            // when
            await this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                EntityType.Link,
                entityId,
                TestContext.Current.CancellationToken);

            // then: the human half still ran — the silence is about Berean alone
            this.approvalReviewServiceMock.Verify(service =>
                service.DismissStaleApprovalReviewAsync(
                    staleReviewId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.aiReviewerAssignmentWorkflowServiceMock.Verify(service =>
                service.ReturnStaleAIReviewerAssignmentToPendingAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.aiReviewerAssignmentWorkflowServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// THE RESET IS OFF, so nothing is dismissed and there is nothing stale — and the round is
        /// not asked about Berean either.
        ///
        /// <para><b>What it catches.</b> Hoisting the AI half out of the
        /// <c>RequireReapprovalOnChange</c> branch. The reviews still stand when the setting is
        /// off (§9.7.4), so returning Berean to pending there would discard a finished pass on an
        /// edit the round has decided does not invalidate anything — and would do it on every
        /// edit, silently, since nothing else on this path would notice.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNotTouchBereansAssignmentOnEditWhenTheStaleReviewResetIsOffAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupFlowApprovalRow(storageApproval);

            // A stale assignment IS standing, so a flow that reached for it would have something
            // to write — the absence below is about the branch, not about an empty round.
            SetupResettableAIReviewerAssignment(approvalId, Guid.NewGuid());
            SetupAIReviewerAssignmentReturnToPending();

            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    shouldResetStaleReviewsOnChange: false));

            // when
            await this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                EntityType.Link,
                entityId,
                TestContext.Current.CancellationToken);

            // then: not even the gather, which is work done to throw away when nothing is being
            // taken back
            this.accessBrokerMock.Verify(broker =>
                broker.FindResettableAIReviewerAssignmentIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.aiReviewerAssignmentWorkflowServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Berean's half runs LAST — after the human dismissal, and after the RE-EVALUATION the
        /// dismissal makes necessary.
        ///
        /// <para>Nothing orders the two by data: the evaluation reads the round's reviews and
        /// comments and never the assignment row, and §8.6.2's re-trigger event is not built, so
        /// nothing subscribes in the other direction either. It is last because it is the
        /// tidy-up, and because the evaluation the dismissal makes necessary must always
        /// run.</para>
        ///
        /// <para><b>What it catches.</b> Moving the reset ahead of the evaluation. The dismissal
        /// facts are swallowed by the suppression window, so nothing else re-tests the round until
        /// its next input change — anything that could stop the evaluation running leaves the
        /// round sitting unevaluated on reviews that no longer count. The AI step can no longer
        /// throw (see the test below), but it can still return early, and ahead of the evaluation
        /// its position alone would be doing the ordering.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnBereanToPendingOnlyAfterTheEditsReEvaluationAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var staleReviewId = Guid.NewGuid();
            var order = new List<string>();

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupFlowApprovalRow(storageApproval);
            SetupDismissableReviews(approvalId, staleReviewId);
            SetupResettableAIReviewerAssignment(approvalId, Guid.NewGuid());

            this.approvalReviewServiceMock.Setup(service =>
                service.DismissStaleApprovalReviewAsync(
                    staleReviewId,
                    It.IsAny<CancellationToken>()))
                        .Callback(() => order.Add("dismiss"))
                        .ReturnsAsync((ApprovalReview)null);

            this.aiReviewerAssignmentWorkflowServiceMock.Setup(service =>
                service.ReturnStaleAIReviewerAssignmentToPendingAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Guid aiReviewerAssignmentId, CancellationToken _) =>
                        {
                            order.Add("ai-reset");

                            return new AIReviewerAssignment { Id = aiReviewerAssignmentId };
                        });

            // The evaluation is observed where it BEGINS — it re-reads the conditions, which is
            // the whole reason it exists on this path.
            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    shouldResetStaleReviewsOnChange: true),

                secondConditions: CreateFlowConditions(
                    shouldResetStaleReviewsOnChange: true),

                onConditionsRead: () => order.Add("conditions-read"));

            // when
            await this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                EntityType.Link,
                entityId,
                TestContext.Current.CancellationToken);

            // then
            order.Should().Equal(
                "conditions-read", "dismiss", "conditions-read", "ai-reset");
        }

        /// <summary>
        /// THE AI STEP CANNOT FAULT THE EDIT FLOW, and here that matters more than it does on the
        /// reset. <c>ProcessEntityModifiedAsync</c> runs on a delivered fact: by the time this
        /// line is reached the re-evaluation has COMMITTED and may already have auto-approved the
        /// round and published the entity. A throw would fault the delivery on work that
        /// succeeded, the substrate would record it as failed and REDELIVER it, and the whole
        /// flow — dismissal and evaluation — would run again over committed work.
        ///
        /// <para>So the failure is logged and swallowed. The two flags stay stale until a
        /// moderator asks Berean again; nothing is swallowed silently, which is why the log call
        /// is asserted rather than only the absence of a throw.</para>
        ///
        /// <para>The same helper serves this site and the reset's, so the two cannot drift on
        /// what a failure here costs — but they are pinned separately because only one of them
        /// runs under a substrate delivery, and that is what makes the swallow load-bearing rather
        /// than merely tidy.</para>
        /// </summary>
        [Fact]
        public async Task ShouldStillCompleteTheEditFlowWhenReturningBereanToPendingFailsAsync()
        {
            // given: a plain author revising their own submitted content, as on the edit path's
            // other tests — the identity is not what this one is about, but running it as anyone
            // else would be testing a caller the flow never has.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var staleReviewId = Guid.NewGuid();

            Approval storageApproval = CreateFlowApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupFlowApprovalRow(storageApproval);
            SetupDismissableReviews(approvalId, staleReviewId);
            SetupResettableAIReviewerAssignment(approvalId, Guid.NewGuid());

            // Storage failed under the transition. The seam reads a row and writes it back, so
            // this is the failure it actually has.
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

            SetupFlowConditionsReads(
                firstConditions: CreateFlowConditions(
                    shouldResetStaleReviewsOnChange: true),

                secondConditions: CreateFlowConditions(
                    shouldResetStaleReviewsOnChange: true));

            // when: no throw — the delivery completes and is recorded as handled
            await this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                EntityType.Link,
                entityId,
                TestContext.Current.CancellationToken);

            // then: everything ahead of the AI step still ran
            this.approvalReviewServiceMock.Verify(service =>
                service.DismissStaleApprovalReviewAsync(
                    staleReviewId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // and the failure reached the error log, unwrapped — this step has no chain of its
            // own, so the exception the seam raised is the one recorded
            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(failedStorageException),
                Times.Once);
        }

        // The unfiltered view: what storage holds for the round, regardless of who is asking.
        private void SetupDismissableReviews(Guid approvalId, params Guid[] approvalReviewIds) =>
            this.accessBrokerMock.Setup(broker =>
                broker.FindDismissableApprovalReviewIdsAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(approvalReviewIds.ToList());

        private List<Guid> SetupFlowApprovalReviews(
            List<ApprovalReview> approvalReviews,
            Action onReviewDismissed = null)
        {
            var dismissedReviewIds = new List<Guid>();


            // The same rows through the seam the flow actually reads. The caller-facing read
            // above is identity-filtered and cannot answer "what does this round hold" — a test
            // that supplied only that view would be describing one caller's slice as if it were
            // the round, which is exactly the bug this seam exists to close.
            this.accessBrokerMock.Setup(broker =>
                broker.FindDismissableApprovalReviewIdsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Guid approvalId, CancellationToken _) =>
                            approvalReviews
                                .Where(approvalReview =>
                                    approvalReview.ApprovalId == approvalId
                                        && approvalReview.IsDeleted == false
                                        && approvalReview.StatusId != ApprovalStatus.Dismissed)
                                .Select(approvalReview => approvalReview.Id)
                                .ToList());

            this.approvalReviewServiceMock.Setup(service =>
                service.DismissStaleApprovalReviewAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .Returns((Guid approvalReviewId, CancellationToken cancellationToken) =>
                        {
                            dismissedReviewIds.Add(approvalReviewId);
                            onReviewDismissed?.Invoke();

                            return new ValueTask<ApprovalReview>(
                                new ApprovalReview { Id = approvalReviewId });
                        });

            return dismissedReviewIds;
        }

        private List<EventEnvelope<Link>> SetupFlowLinkCommandPublish()
        {
            var publishedCommands = new List<EventEnvelope<Link>>();

            this.eventBrokerMock.Setup(broker =>
                broker.PublishLinkProcessingAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<LinkProcessingEventOperation>()))
                        .Returns((EventEnvelope<Link> envelope,
                            LinkProcessingEventOperation operation) =>
                        {
                            publishedCommands.Add(envelope);

                            return new ValueTask<EventPublishResult<Link>>(
                                new EventPublishResult<Link>());
                        });

            return publishedCommands;
        }

        private void SetupFlowSystemEnvelope<TEntity>() =>
            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateSystemAsync(It.IsAny<TEntity>()))
                    .Returns((TEntity content) =>
                        new ValueTask<EventEnvelope<TEntity>>(
                            new EventEnvelope<TEntity>
                            {
                                Content = content,
                                SecurityContext = new SecurityContext(),
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

        [Fact]
        public async Task ShouldEvaluateTheRoundAReviewWasRecordedAgainstOnReviewAddedAsync()
        {
            // given: the review fact names its round directly through ApprovalId, so the handler
            // hands THAT across rather than reaching for the entity — which the flow resolves
            // anyway. A handler keyed on the wrong id would evaluate somebody else's round.
            var approvalId = Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111");
            var otherApprovalId = Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222");

            var reviewEnvelope = new EventEnvelope<ApprovalReview>
            {
                Content = new ApprovalReview { Id = otherApprovalId, ApprovalId = approvalId },
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new Approval
                        {
                            Id = approvalId,
                            EntityType = EntityType.Link,
                            EntityId = Guid.NewGuid(),
                            ApprovalStatus = ApprovalStatus.Draft,
                        });

            // when
            EventEnvelope<ApprovalReview>? actualEnvelope =
                await this.approvalOrchestrationService.OnApprovalReviewAddedAsync(
                    reviewEnvelope,
                    TestContext.Current.CancellationToken);

            // then: keyed on ApprovalId, not on the review's own id
            this.approvalServiceMock.Verify(service =>
                service.RetrieveApprovalByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // a fact is a notification: replying would put this service's name on a fact another
            // service published
            actualEnvelope.Should().BeNull();
        }

        // ── A reader's changed reaction (§APR9.7.4, #727) ──────────────────────────────────
        //
        // The flow is reached through the Association-Repointed ear, the only caller that hands
        // it the change's UpdatedWhen, so these tests drive the ear and read the flow's work.

        [Fact]
        public async Task ShouldReturnARoundDecidedBeforeTheChangeAndEvaluateItAsync()
        {
            // given: the reader's reaction was approved a day before they changed it, so the
            // round is the old reaction's and the change has to be decided afresh (§APR9.7.4, the
            // round "was decided before the change"). The seeded personal tier answers every read:
            // nothing is required and the met conditions apply themselves, so the evaluation
            // approves the round again in the same act (§DOM4.5 rule 4).
            //
            // The round arrives BYPASS-APPROVED. Returning it withdraws that outcome, and a round
            // back at Submitted must not still claim a waiver for a decision it no longer holds
            // (§APR9.7.5), so the return write carries the pair cleared.
            var entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            DateTimeOffset changedWhen = RepointedChangeTime;
            var flowSteps = new List<string>();

            Approval storageApproval = CreateRepointedRound(
                approvalId: approvalId,
                entityId: entityId,
                approvalStatus: ApprovalStatus.Approved,
                updatedWhen: changedWhen.AddDays(-1),
                updatedBy: SystemIdentity.UserId);

            storageApproval.IsApprovedByBypass = true;
            storageApproval.ApprovedByBypassReason = "approved before the reader changed it";

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Approved, approvalId));
            SetupRepointedReviews(approvalId);
            SetupFlowSystemEnvelope<Association>();

            List<(Approval Approval, WorkflowAttribution Attribution)> roundWrites =
                SetupRepointedRoundWrites(storageApproval, flowSteps);

            List<Association> associationCommands = SetupRepointedAssociationCommands(flowSteps);

            SetupRepointedConditions(
                CreateFlowConditions(areConditionsMet: true, shouldAutoApprove: true),
                flowSteps);

            // when
            await this.approvalOrchestrationService.OnAssociationRepointedAsync(
                envelope: CreateRepointedEnvelope(
                    CreateRepointedAssociation(entityId, changedWhen)),
                cancellationToken: TestContext.Current.CancellationToken);

            // then: returned first, as the workflow, with the waiver it no longer holds cleared
            roundWrites.Should().HaveCount(2);

            roundWrites[0].Approval.Id.Should().Be(approvalId);
            roundWrites[0].Approval.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            roundWrites[0].Approval.IsApprovedByBypass.Should().BeFalse();
            roundWrites[0].Approval.ApprovedByBypassReason.Should().BeNull();
            roundWrites[0].Attribution.Should().Be(WorkflowAttribution.System);

            // the association followed its round to Submitted and was unpublished (§APR9.8)
            associationCommands.Should().HaveCount(2);

            associationCommands[0].Id.Should().Be(entityId);
            associationCommands[0].ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            associationCommands[0].IsPublished.Should().BeFalse();
            associationCommands[0].PublishDate.Should().BeNull();
            associationCommands[0].IsApprovedByBypass.Should().BeFalse();

            // and the round was evaluated only after that: approved again, and published again
            roundWrites[1].Approval.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            roundWrites[1].Attribution.Should().Be(WorkflowAttribution.System);
            associationCommands[1].ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            associationCommands[1].IsPublished.Should().BeTrue();

            flowSteps.Should().Equal(
                "write:Submitted",
                "command:Submitted",
                "conditions-read",
                "write:Approved",
                "command:Approved");
        }

        [Fact]
        public async Task ShouldReturnARoundApprovedAfterTheChangeOnAnOldReviewAsync()
        {
            // given: the round was approved a minute AFTER the reader changed their reaction,
            // inside the window before this fact was heard, while a review written for the old
            // reaction still stood. Nothing records which reviews an approval counted, so that
            // approval may have counted the old pair's, and it is returned whatever reached it
            // (§APR9.7.4, the second case). A review written since the change stands beside it.
            //
            // The tier asks for two approvals and the read after the return finds them unmet, so
            // the round is evaluated and stays open for fresh reviews.
            var entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            DateTimeOffset changedWhen = RepointedChangeTime;
            var flowSteps = new List<string>();

            Approval storageApproval = CreateRepointedRound(
                approvalId: approvalId,
                entityId: entityId,
                approvalStatus: ApprovalStatus.Approved,
                updatedWhen: changedWhen.AddMinutes(1),
                updatedBy: SystemIdentity.UserId);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Approved, approvalId));
            SetupFlowSystemEnvelope<Association>();

            SetupRepointedReviews(
                approvalId,
                CreateRepointedReview(Guid.NewGuid(), createdWhen: changedWhen.AddHours(-1)),
                CreateRepointedReview(Guid.NewGuid(), createdWhen: changedWhen.AddSeconds(30)));

            List<(Approval Approval, WorkflowAttribution Attribution)> roundWrites =
                SetupRepointedRoundWrites(storageApproval, flowSteps);

            List<Association> associationCommands = SetupRepointedAssociationCommands(flowSteps);

            SetupRepointedConditions(
                CreateFlowConditions(
                    blockReasons: new List<AccessDenialReason>
                    {
                        AccessDenialReason.ApprovalThresholdNotMet,
                    },
                    approvalCount: 1,
                    requiredNumberOfApprovals: 2),
                flowSteps);

            // when
            await this.approvalOrchestrationService.OnAssociationRepointedAsync(
                envelope: CreateRepointedEnvelope(
                    CreateRepointedAssociation(entityId, changedWhen)),
                cancellationToken: TestContext.Current.CancellationToken);

            // then: returned as the workflow, and the association followed it off the public site
            roundWrites.Should().ContainSingle();
            roundWrites[0].Approval.Id.Should().Be(approvalId);
            roundWrites[0].Approval.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            roundWrites[0].Attribution.Should().Be(WorkflowAttribution.System);

            associationCommands.Should().ContainSingle();
            associationCommands[0].Id.Should().Be(entityId);
            associationCommands[0].ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            associationCommands[0].IsPublished.Should().BeFalse();

            // and evaluated once it was back: unmet, so it stays open
            flowSteps.Should().Equal("write:Submitted", "command:Submitted", "conditions-read");
        }

        [Fact]
        public async Task ShouldReturnARoundAnOldRejectionRejectedAfterTheChangeAsync()
        {
            // given: the workflow rejected the round a minute AFTER the change, on a standing
            // rejection, while a rejection written for the old reaction still stood and no review
            // written since rejects. Then an old review is what blocks the round, so it is
            // returned (§APR9.7.4, the third case).
            //
            // The review written since the change APPROVES. It is the new pair's own, and only a
            // new pair's REJECTION would keep the round where it is.
            var entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            DateTimeOffset changedWhen = RepointedChangeTime;
            var flowSteps = new List<string>();

            Approval storageApproval = CreateRepointedRound(
                approvalId: approvalId,
                entityId: entityId,
                approvalStatus: ApprovalStatus.Rejected,
                updatedWhen: changedWhen.AddMinutes(1),
                updatedBy: SystemIdentity.UserId);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Rejected, approvalId));
            SetupFlowSystemEnvelope<Association>();

            SetupRepointedReviews(
                approvalId,
                CreateRepointedReview(
                    Guid.NewGuid(),
                    createdWhen: changedWhen.AddHours(-1),
                    isRejection: true),

                CreateRepointedReview(
                    Guid.NewGuid(),
                    createdWhen: changedWhen.AddSeconds(30),
                    isRejection: false));

            List<(Approval Approval, WorkflowAttribution Attribution)> roundWrites =
                SetupRepointedRoundWrites(storageApproval, flowSteps);

            List<Association> associationCommands = SetupRepointedAssociationCommands(flowSteps);

            SetupRepointedConditions(
                CreateFlowConditions(
                    blockReasons: new List<AccessDenialReason>
                    {
                        AccessDenialReason.ApprovalThresholdNotMet,
                    },
                    approvalCount: 1,
                    requiredNumberOfApprovals: 2),
                flowSteps);

            // when
            await this.approvalOrchestrationService.OnAssociationRepointedAsync(
                envelope: CreateRepointedEnvelope(
                    CreateRepointedAssociation(entityId, changedWhen)),
                cancellationToken: TestContext.Current.CancellationToken);

            // then: returned as the workflow, and the association followed it
            roundWrites.Should().ContainSingle();
            roundWrites[0].Approval.Id.Should().Be(approvalId);
            roundWrites[0].Approval.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            roundWrites[0].Attribution.Should().Be(WorkflowAttribution.System);

            associationCommands.Should().ContainSingle();
            associationCommands[0].Id.Should().Be(entityId);
            associationCommands[0].ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            associationCommands[0].IsPublished.Should().BeFalse();

            // and evaluated once it was back
            flowSteps.Should().Equal("write:Submitted", "command:Submitted", "conditions-read");
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(-1440, false)]
        [InlineData(-1440, true)]
        public async Task ShouldNeverReturnADirectRejectionAsync(
            int minutesFromTheChangeToTheRejection,
            bool isAnOldRejectionStanding)
        {
            // given: a publisher rejected the round directly, so the row records the person who
            // took the decision and not the workflow (WorkflowAttribution, §APR9.7.5). A direct
            // rejection counts no review, so no review can be what decided it, and returning it
            // would erase a verdict nothing re-takes: the evaluation that follows can only approve
            // or leave open (§APR9.7.4).
            //
            // Each row is a shape a STANDING rejection would be returned in: rejected after the
            // change while an old rejection still stands, or rejected before the change, with or
            // without its rejection still standing. The evaluation is armed to approve, so a round
            // returned by mistake is written twice and published.
            var entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            DateTimeOffset changedWhen = RepointedChangeTime;
            var flowSteps = new List<string>();

            Approval storageApproval = CreateRepointedRound(
                approvalId: approvalId,
                entityId: entityId,
                approvalStatus: ApprovalStatus.Rejected,
                updatedWhen: changedWhen.AddMinutes(minutesFromTheChangeToTheRejection),
                updatedBy: Guid.NewGuid().ToString());

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Rejected, approvalId));
            SetupFlowSystemEnvelope<Association>();

            DismissableApprovalReview[] standingReviews = isAnOldRejectionStanding
                ? new[]
                {
                    CreateRepointedReview(
                        Guid.NewGuid(),
                        createdWhen: changedWhen.AddDays(-2),
                        isRejection: true),
                }
                : Array.Empty<DismissableApprovalReview>();

            SetupRepointedReviews(approvalId, standingReviews);

            List<(Approval Approval, WorkflowAttribution Attribution)> roundWrites =
                SetupRepointedRoundWrites(storageApproval, flowSteps);

            List<Association> associationCommands = SetupRepointedAssociationCommands(flowSteps);

            SetupRepointedConditions(
                CreateFlowConditions(areConditionsMet: true, shouldAutoApprove: true),
                flowSteps);

            // when
            await this.approvalOrchestrationService.OnAssociationRepointedAsync(
                envelope: CreateRepointedEnvelope(
                    CreateRepointedAssociation(entityId, changedWhen)),
                cancellationToken: TestContext.Current.CancellationToken);

            // then: the round stays Rejected, and the association is not told anything
            roundWrites.Should().BeEmpty();
            associationCommands.Should().BeEmpty();

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<Association>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldDismissTheOldPairsReviewsWhateverTheSettingSaysAsync()
        {
            // given: RequireReapprovalOnChange is OFF, which keeps every review standing on any
            // other change (§APR8.8). A changed reaction's round starts with no reviews, so the old
            // pair's are dismissed whatever the setting says — every active review written before
            // the change, approvals and rejections alike — and Berean's finished pass goes back to
            // pending with them (§APR8.8 regardless-rule 1, §APR9.7.4).
            //
            // The new pair's reviews stand: one written a minute after the change, and one stamped
            // in the change's own tick, which does not precede it and so is not the old pair's.
            var entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            var oldApprovingReviewId = Guid.NewGuid();
            var oldRejectingReviewId = Guid.NewGuid();
            var staleAssignmentId = Guid.NewGuid();
            DateTimeOffset changedWhen = RepointedChangeTime;
            var flowSteps = new List<string>();

            Approval storageApproval = CreateRepointedRound(
                approvalId: approvalId,
                entityId: entityId,
                approvalStatus: ApprovalStatus.Submitted,
                updatedWhen: changedWhen.AddDays(-1),
                updatedBy: SystemIdentity.UserId);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupRepointedRoundWrites(storageApproval, flowSteps);

            List<Guid> dismissedReviewIds = SetupRepointedReviews(
                approvalId,
                flowSteps,
                CreateRepointedReview(oldApprovingReviewId, createdWhen: changedWhen.AddDays(-1)),

                CreateRepointedReview(
                    oldRejectingReviewId,
                    createdWhen: changedWhen.AddTicks(-1),
                    isRejection: true),

                CreateRepointedReview(Guid.NewGuid(), createdWhen: changedWhen),
                CreateRepointedReview(Guid.NewGuid(), createdWhen: changedWhen.AddMinutes(1)));

            SetupResettableAIReviewerAssignment(approvalId, staleAssignmentId);

            this.aiReviewerAssignmentWorkflowServiceMock.Setup(service =>
                service.ReturnStaleAIReviewerAssignmentToPendingAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Guid aiReviewerAssignmentId, CancellationToken _) =>
                        {
                            flowSteps.Add("ai-reset");

                            return new AIReviewerAssignment { Id = aiReviewerAssignmentId };
                        });

            SetupRepointedConditions(
                CreateFlowConditions(shouldResetStaleReviewsOnChange: false),
                flowSteps);

            // when
            await this.approvalOrchestrationService.OnAssociationRepointedAsync(
                envelope: CreateRepointedEnvelope(
                    CreateRepointedAssociation(entityId, changedWhen)),
                cancellationToken: TestContext.Current.CancellationToken);

            // then: the old pair's reviews went, and the new pair's stood
            dismissedReviewIds.Should().BeEquivalentTo(
                new[] { oldApprovingReviewId, oldRejectingReviewId });

            // Berean's pass on the old reaction went back to pending with them
            this.aiReviewerAssignmentWorkflowServiceMock.Verify(service =>
                service.ReturnStaleAIReviewerAssignmentToPendingAsync(
                    staleAssignmentId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // and the round was evaluated on the reviews that were left: the dismissals come
            // before the one conditions read, and Berean is the tidy-up after it
            flowSteps.Should().Equal("dismiss", "dismiss", "conditions-read", "ai-reset");
        }

        [Fact]
        public async Task ShouldDismissAndEvaluateAnOpenRoundWithoutReturningItAsync()
        {
            // given: the round is still OPEN — the tier wants a second approval — and was last
            // written a day before the change. Only a DECIDED round goes back to Submitted, so an
            // open one stays where it is, whenever it was last written; its old pair's review is
            // still dismissed, and it is evaluated on what is left (§APR9.7.4). The read after
            // the dismissal finds the threshold unmet, so nothing is written.
            var entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            var oldPairReviewId = Guid.NewGuid();
            DateTimeOffset changedWhen = RepointedChangeTime;
            var flowSteps = new List<string>();

            Approval storageApproval = CreateRepointedRound(
                approvalId: approvalId,
                entityId: entityId,
                approvalStatus: ApprovalStatus.Submitted,
                updatedWhen: changedWhen.AddDays(-1),
                updatedBy: SystemIdentity.UserId);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupFlowSystemEnvelope<Association>();

            List<Guid> dismissedReviewIds = SetupRepointedReviews(
                approvalId,
                flowSteps,
                CreateRepointedReview(oldPairReviewId, createdWhen: changedWhen.AddHours(-2)),
                CreateRepointedReview(Guid.NewGuid(), createdWhen: changedWhen.AddMinutes(5)));

            List<(Approval Approval, WorkflowAttribution Attribution)> roundWrites =
                SetupRepointedRoundWrites(storageApproval, flowSteps);

            List<Association> associationCommands = SetupRepointedAssociationCommands(flowSteps);

            SetupRepointedConditions(
                CreateFlowConditions(
                    blockReasons: new List<AccessDenialReason>
                    {
                        AccessDenialReason.ApprovalThresholdNotMet,
                    },
                    approvalCount: 1,
                    requiredNumberOfApprovals: 2),
                flowSteps);

            // when
            await this.approvalOrchestrationService.OnAssociationRepointedAsync(
                envelope: CreateRepointedEnvelope(
                    CreateRepointedAssociation(entityId, changedWhen)),
                cancellationToken: TestContext.Current.CancellationToken);

            // then: it stayed Submitted — nothing written to the round, nothing sent to the
            // association
            roundWrites.Should().BeEmpty();
            associationCommands.Should().BeEmpty();

            // the old pair's review was dismissed, and the round evaluated after it
            dismissedReviewIds.Should().Equal(new[] { oldPairReviewId });
            flowSteps.Should().Equal("dismiss", "conditions-read");
        }

        [Fact]
        public async Task ShouldFindNothingLeftToDoOnARedeliveredChangeAsync()
        {
            // given: the round as the FIRST delivery of this fact left it — returned, its old
            // pair's reviews dismissed, and approved again seconds after the change — and a
            // reviewer and Berean have both looked at the NEW reaction since. The substrate
            // delivers the same fact again (§EVN20 rule 10).
            //
            // The return and the dismissal are deltas, so they carry the redelivery check
            // §EVN20 rule 4 requires: no old-pair review stands and no round decided before the
            // change is left decided, so there is nothing to take back (§APR9.7.4). Berean's
            // return to pending rides with the dismissal, and its pass on the new reaction must
            // survive the redelivery like the reviewer's review.
            var entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            DateTimeOffset changedWhen = RepointedChangeTime;
            var flowSteps = new List<string>();

            Approval storageApproval = CreateRepointedRound(
                approvalId: approvalId,
                entityId: entityId,
                approvalStatus: ApprovalStatus.Approved,
                updatedWhen: changedWhen.AddSeconds(2),
                updatedBy: SystemIdentity.UserId);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Approved, approvalId));
            SetupFlowSystemEnvelope<Association>();

            List<Guid> dismissedReviewIds = SetupRepointedReviews(
                approvalId,
                flowSteps,
                CreateRepointedReview(Guid.NewGuid(), createdWhen: changedWhen.AddMinutes(5)));

            List<(Approval Approval, WorkflowAttribution Attribution)> roundWrites =
                SetupRepointedRoundWrites(storageApproval, flowSteps);

            List<Association> associationCommands = SetupRepointedAssociationCommands(flowSteps);

            // Berean's pass on the new reaction: finished, so a reset would have something to
            // take back.
            SetupResettableAIReviewerAssignment(approvalId, Guid.NewGuid());
            SetupAIReviewerAssignmentReturnToPending();

            SetupRepointedConditions(
                CreateFlowConditions(areConditionsMet: true, shouldAutoApprove: true),
                flowSteps);

            EventEnvelope<Association> redeliveredEnvelope =
                CreateRepointedEnvelope(CreateRepointedAssociation(entityId, changedWhen));

            // when
            await this.approvalOrchestrationService.OnAssociationRepointedAsync(
                envelope: redeliveredEnvelope,
                cancellationToken: TestContext.Current.CancellationToken);

            // then: no round returned, no review dismissed, and nothing sent to the association
            roundWrites.Should().BeEmpty();
            associationCommands.Should().BeEmpty();
            dismissedReviewIds.Should().BeEmpty();

            // Berean is not asked about, let alone taken back
            this.accessBrokerMock.Verify(broker =>
                broker.FindResettableAIReviewerAssignmentIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.aiReviewerAssignmentWorkflowServiceMock.VerifyNoOtherCalls();

            // only the evaluation ran again
            flowSteps.Should().Equal("conditions-read");
        }

        [Fact]
        public async Task ShouldReturnARoundRejectedBeforeTheChangeAndEvaluateItAsync()
        {
            // given: the workflow rejected the round on a standing rejection a day BEFORE the
            // reader changed their reaction, so the round is the old reaction's, decided on the
            // old pair's terms, and goes back like any round decided before the change
            // (§APR9.7.4, the first case). The rejection that blocked it has since been withdrawn,
            // so no review stands behind it: what returns it is when it was decided, not which
            // reviews are left.
            //
            // The tier asks for an approval, so the round is evaluated and stays open.
            var entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            DateTimeOffset changedWhen = RepointedChangeTime;
            var flowSteps = new List<string>();

            Approval storageApproval = CreateRepointedRound(
                approvalId: approvalId,
                entityId: entityId,
                approvalStatus: ApprovalStatus.Rejected,
                updatedWhen: changedWhen.AddDays(-1),
                updatedBy: SystemIdentity.UserId);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Rejected, approvalId));
            SetupFlowSystemEnvelope<Association>();
            SetupRepointedReviews(approvalId);

            List<(Approval Approval, WorkflowAttribution Attribution)> roundWrites =
                SetupRepointedRoundWrites(storageApproval, flowSteps);

            List<Association> associationCommands = SetupRepointedAssociationCommands(flowSteps);

            SetupRepointedConditions(
                CreateFlowConditions(
                    blockReasons: new List<AccessDenialReason>
                    {
                        AccessDenialReason.ApprovalThresholdNotMet,
                    },
                    approvalCount: 0,
                    requiredNumberOfApprovals: 1),
                flowSteps);

            // when
            await this.approvalOrchestrationService.OnAssociationRepointedAsync(
                envelope: CreateRepointedEnvelope(
                    CreateRepointedAssociation(entityId, changedWhen)),
                cancellationToken: TestContext.Current.CancellationToken);

            // then: returned as the workflow
            roundWrites.Should().ContainSingle();
            roundWrites[0].Approval.Id.Should().Be(approvalId);
            roundWrites[0].Approval.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            roundWrites[0].Attribution.Should().Be(WorkflowAttribution.System);

            // the association followed it to Submitted and is unpublished
            associationCommands.Should().ContainSingle();
            associationCommands[0].Id.Should().Be(entityId);
            associationCommands[0].ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            associationCommands[0].IsPublished.Should().BeFalse();
            associationCommands[0].PublishDate.Should().BeNull();

            // and the round was evaluated once it was back
            flowSteps.Should().Equal("write:Submitted", "command:Submitted", "conditions-read");
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(300_000_000L)]
        public async Task ShouldKeepARejectionTheNewPairAlsoStandsBehindAsync(
            long ticksFromTheChangeToTheNewRejection)
        {
            // given: the workflow rejected the round a minute after the change on a standing
            // rejection, and a rejection of the old reaction and one of the new reaction both
            // still stand. A new review's rejection is the new pair's own verdict, so the round is
            // not returned: the evaluation that would follow can only approve or leave it open,
            // and returning it would erase a verdict nothing re-takes (§APR9.7.4, the third case).
            //
            // The new pair's rejection is written thirty seconds after the change, or in the
            // change's own tick, which does not precede it. The evaluation is armed to approve, so
            // a round returned by mistake is written and published.
            var entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            DateTimeOffset changedWhen = RepointedChangeTime;
            var flowSteps = new List<string>();

            Approval storageApproval = CreateRepointedRound(
                approvalId: approvalId,
                entityId: entityId,
                approvalStatus: ApprovalStatus.Rejected,
                updatedWhen: changedWhen.AddMinutes(1),
                updatedBy: SystemIdentity.UserId);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Rejected, approvalId));
            SetupFlowSystemEnvelope<Association>();

            SetupRepointedReviews(
                approvalId,
                CreateRepointedReview(
                    Guid.NewGuid(),
                    createdWhen: changedWhen.AddHours(-1),
                    isRejection: true),

                CreateRepointedReview(
                    Guid.NewGuid(),
                    createdWhen: changedWhen.AddTicks(ticksFromTheChangeToTheNewRejection),
                    isRejection: true));

            List<(Approval Approval, WorkflowAttribution Attribution)> roundWrites =
                SetupRepointedRoundWrites(storageApproval, flowSteps);

            List<Association> associationCommands = SetupRepointedAssociationCommands(flowSteps);

            SetupRepointedConditions(
                CreateFlowConditions(areConditionsMet: true, shouldAutoApprove: true),
                flowSteps);

            // when
            await this.approvalOrchestrationService.OnAssociationRepointedAsync(
                envelope: CreateRepointedEnvelope(
                    CreateRepointedAssociation(entityId, changedWhen)),
                cancellationToken: TestContext.Current.CancellationToken);

            // then: the round stays Rejected, and the association is not told anything
            roundWrites.Should().BeEmpty();
            associationCommands.Should().BeEmpty();
        }

        private static Approval CreateRepointedRound(
            Guid approvalId,
            Guid entityId,
            ApprovalStatus approvalStatus,
            DateTimeOffset updatedWhen,
            string updatedBy) =>
            new Approval
            {
                Id = approvalId,
                EntityType = EntityType.Association,
                EntityId = entityId,
                ApprovalStatus = approvalStatus,
                UpdatedWhen = updatedWhen,
                UpdatedBy = updatedBy,
                IsDeleted = false,
            };

        // The round as the resolution reads it, and every write to it captured as a SNAPSHOT with
        // the attribution it was written under: the flow mutates the row it holds and hands the
        // same object on, so the instance alone would show only its last state.
        private List<(Approval Approval, WorkflowAttribution Attribution)> SetupRepointedRoundWrites(
            Approval storageApproval,
            List<string> flowSteps)
        {
            var roundWrites = new List<(Approval Approval, WorkflowAttribution Attribution)>();

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(
                    storageApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            this.approvalServiceMock.Setup(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()))
                        .Returns((Approval approval,
                            WorkflowAttribution attribution,
                            CancellationToken cancellationToken) =>
                        {
                            Approval savedApproval = approval.DeepClone();
                            roundWrites.Add((savedApproval, attribution));
                            flowSteps.Add($"write:{savedApproval.ApprovalStatus}");

                            return new ValueTask<Approval>(savedApproval.DeepClone());
                        });

            return roundWrites;
        }

        // Every entity command the flow sends the association, in order.
        private List<Association> SetupRepointedAssociationCommands(List<string> flowSteps)
        {
            var associationCommands = new List<Association>();

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Approving))
                        .Returns((EventEnvelope<Association> envelope,
                            AssociationEventOperation operation) =>
                        {
                            associationCommands.Add(envelope.Content);
                            flowSteps.Add($"command:{envelope.Content.ApprovalStatus}");

                            return new ValueTask<EventPublishResult<Association>>(
                                new EventPublishResult<Association>());
                        });

            return associationCommands;
        }

        // The same verdict for every read, each one recorded where it happened.
        private void SetupRepointedConditions(
            ApprovalConditionsVerdict conditionsVerdict,
            List<string> flowSteps) =>
            this.accessBrokerMock.Setup(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .Returns((Guid approvalId, CancellationToken cancellationToken) =>
                        {
                            flowSteps.Add("conditions-read");

                            return new ValueTask<ApprovalConditionsVerdict>(conditionsVerdict);
                        });
    }
}
