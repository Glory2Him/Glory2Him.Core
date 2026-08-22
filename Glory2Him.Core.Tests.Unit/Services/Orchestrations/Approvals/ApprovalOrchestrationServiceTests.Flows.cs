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
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
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

            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            actualOutcome.ApprovalStatus.Should().NotBe(ApprovalStatus.Approved);
            actualOutcome.IsEntitySyncRequested.Should().BeFalse();

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
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
                    It.IsAny<CancellationToken>()))
                        .Returns((Approval approval, CancellationToken cancellationToken) =>
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
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer),
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
    }
}
