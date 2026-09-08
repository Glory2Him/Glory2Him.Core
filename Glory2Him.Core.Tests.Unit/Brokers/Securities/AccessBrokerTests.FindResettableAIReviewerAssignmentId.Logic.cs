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
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Moq;
using Xunit;

namespace Glory2Him.Core.Tests.Unit.Brokers.Securities
{
    public partial class AccessBrokerTests
    {
        // The predicate that decides whether a round's AI reviewer assignment still reports
        // something a dismissal has to take back. It lives here for the same reason
        // FindDismissableApprovalReviewIds does: the caller-facing read is identity-filtered.
        // IAIReviewerAssignmentService.RetrieveAIReviewerAssignmentByApprovalIdAsync answers null
        // — and logs a denial — for anyone outside the review tier, and the edit path that needs
        // this answer runs under the EDITOR's identity, which for the ordinary case (an author
        // revising their own submission) carries no review role at all (HR-1).
        //
        // This is the only place the predicate is exercised. The orchestration's tests mock
        // IAccessBroker, so they can only assert that the flow ASKS — what the answer should be
        // is settled here, against a real broker over a mocked storage broker.
        private void SetupAIReviewerAssignmentByApprovalId(
            Guid approvalId,
            AIReviewerAssignment aiReviewerAssignment) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    approvalId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(aiReviewerAssignment);

        private static AIReviewerAssignment CreateAIReviewerAssignment(
            Guid approvalId,
            bool isAIReviewCompleted,
            bool isAIReviewCommentsPresent) =>
            new AIReviewerAssignment
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                IsAIReviewCompleted = isAIReviewCompleted,
                IsAIReviewCommentsPresent = isAIReviewCommentsPresent,
            };

        // Every pairing of the two flags, with the answer each one owes. The comments-only row is
        // the case a narrower predicate would strand: the pairing invariant says it cannot be
        // written today, but "still reports something Berean left behind" is what needs taking
        // back, and a row written before that invariant existed is exactly the one nobody would
        // notice going unreset.
        public static TheoryData<bool, bool, bool> AIReviewerAssignmentFlagPairs() =>
            new TheoryData<bool, bool, bool>
            {
                { true, true, true },
                { true, false, true },
                { false, true, true },
                { false, false, false },
            };

        /// <summary>
        /// <b>What it catches.</b> Narrowing the predicate to <c>IsAIReviewCompleted</c> alone
        /// (the comments-only arm reds), and widening it to "any row at all" (the pending arm
        /// reds). The pending arm is the load-bearing one: without it every ordinary edit on a
        /// round Berean is still working through would spend a write and an
        /// <c>AIReviewerAssignment-Modified</c> fact restating what storage already says.
        /// </summary>
        [Theory]
        [MemberData(nameof(AIReviewerAssignmentFlagPairs))]
        public async Task ShouldFindTheAssignmentOnlyWhileItStillReportsAFinishedPassAsync(
            bool isAIReviewCompleted,
            bool isAIReviewCommentsPresent,
            bool isExpectedToBeResettable)
        {
            // given
            Guid approvalId = Guid.NewGuid();

            AIReviewerAssignment aiReviewerAssignment = CreateAIReviewerAssignment(
                approvalId: approvalId,
                isAIReviewCompleted: isAIReviewCompleted,
                isAIReviewCommentsPresent: isAIReviewCommentsPresent);

            SetupAIReviewerAssignmentByApprovalId(approvalId, aiReviewerAssignment);

            // when
            Guid? actualResettableAssignmentId =
                await this.accessBroker.FindResettableAIReviewerAssignmentIdAsync(
                    approvalId: approvalId,
                    cancellationToken: default);

            // then
            actualResettableAssignmentId.Should().Be(
                isExpectedToBeResettable ? aiReviewerAssignment.Id : (Guid?)null,
                because: "either flag makes the row stale — and neither of them set means there " +
                    "is nothing to take back, which is what buys the skipped write");
        }

        /// <summary>
        /// A round Berean was never asked about — the common case. The caller must be able to
        /// tell "nothing to reset" from a failure, because the flow proceeds either way.
        /// </summary>
        [Fact]
        public async Task ShouldFindNoResettableAIReviewerAssignmentWhenTheRoundHasNoneAsync()
        {
            // given: nothing stubs the round-keyed read, so it answers null the way storage does
            // for a round with no live assignment
            Guid approvalId = Guid.NewGuid();

            // when
            Guid? actualResettableAssignmentId =
                await this.accessBroker.FindResettableAIReviewerAssignmentIdAsync(
                    approvalId: approvalId,
                    cancellationToken: default);

            // then
            actualResettableAssignmentId.Should().BeNull(
                because: "a round nobody asked Berean about has nothing to take back, and that " +
                    "is an empty answer rather than an error");
        }

        /// <summary>
        /// THE WHOLE POINT OF THE SEAM: the answer does not depend on who is asking. Nothing here
        /// is handed a <c>SecurityContext</c>, and nothing is asked of the decision function or
        /// the audit surface — what a round's AI reviewer has recorded is a property of the
        /// approval.
        ///
        /// <para><b>What it catches.</b> Any later attempt to route this through the caller-facing
        /// read or to gate it on a role. Either would reach one of the two clients below, and
        /// either would answer null for the ordinary editor — leaving an invariant decided on an
        /// identity-filtered view.</para>
        /// </summary>
        [Fact]
        public async Task ShouldFindTheResettableAIReviewerAssignmentWithoutConsultingTheCallerAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid otherApprovalId = Guid.NewGuid();

            AIReviewerAssignment completedAssignment = CreateAIReviewerAssignment(
                approvalId: approvalId,
                isAIReviewCompleted: true,
                isAIReviewCommentsPresent: true);

            SetupAIReviewerAssignmentByApprovalId(approvalId, completedAssignment);

            // ANOTHER round's assignment, standing behind its own key. Without the round keying,
            // one author's edit would take back whichever assignment the read happened to reach.
            SetupAIReviewerAssignmentByApprovalId(
                otherApprovalId,
                CreateAIReviewerAssignment(
                    approvalId: otherApprovalId,
                    isAIReviewCompleted: true,
                    isAIReviewCommentsPresent: false));

            // when
            Guid? actualResettableAssignmentId =
                await this.accessBroker.FindResettableAIReviewerAssignmentIdAsync(
                    approvalId: approvalId,
                    cancellationToken: default);

            // then
            actualResettableAssignmentId.Should().Be(completedAssignment.Id);

            // The ROUND-KEYED live-row read, which is already narrowed to the one row that is not
            // withdrawn (IsDeleted == false, backed by UX_AIReviewerAssignments_ApprovalId) — so a
            // withdrawn assignment is never offered up for a reset nobody could perform.
            this.storageBrokerMock.Verify(broker =>
                broker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    approvalId, It.IsAny<CancellationToken>()),
                Times.Once);

            // and no identity was consulted on the way
            this.accessClientMock.VerifyNoOtherCalls();
            this.auditClientMock.VerifyNoOtherCalls();
        }
    }
}
