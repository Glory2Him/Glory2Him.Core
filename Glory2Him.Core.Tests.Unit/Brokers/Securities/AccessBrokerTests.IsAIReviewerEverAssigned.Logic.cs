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
        // Gate 6 of the automatic assignment (design §8.6.2.1): has anybody EVER decided about
        // Berean on this round. The predicate is one term — ApprovalId — and carries no IsDeleted
        // term at all, which is the whole reason the member exists.
        //
        // A BROKER UNIT TEST, despite this repository's "brokers hold no logic and get no unit
        // tests" rule, and it is not an exception carved for this member: AccessBroker is the
        // established gathering-broker exception (§8.6.1) because it composes predicates, and a
        // dropped conjunct hides in exactly that. Its own suite is the precedent —
        // FindDismissableApprovalReviewIds, FindRetirableApprovalReviewRequestIds and
        // FindResettableAIReviewerAssignmentId all sit beside this file. The STORAGE broker
        // member this composes over is where the rule does apply, and it gets nothing.
        [Fact]
        public async Task ShouldFindAnAssignmentOnARoundWhateverIsDeletedSaysAsync()
        {
            // given: a round whose only assignment a moderator WITHDREW, and a different round
            // carrying a live one. The withdrawn row is what a filtered read would miss, and the
            // other round's live row is what makes the ApprovalId conjunct observable — without
            // it, a predicate that answered "any assignment anywhere" would pass too.
            Guid approvalId = Guid.NewGuid();
            Guid otherApprovalId = Guid.NewGuid();

            AIReviewerAssignment withdrawnAssignment =
                CreateAIReviewerAssignment(approvalId: approvalId, isDeleted: true);

            AIReviewerAssignment otherRoundsLiveAssignment =
                CreateAIReviewerAssignment(approvalId: otherApprovalId, isDeleted: false);

            SetupAIReviewerAssignments(withdrawnAssignment, otherRoundsLiveAssignment);

            // when
            bool isEverAssigned = await this.accessBroker.IsAIReviewerEverAssignedAsync(
                approvalId: approvalId,
                cancellationToken: default);

            // then
            isEverAssigned.Should().BeTrue(
                because: "a withdrawal is a decision, and an automatic policy must not overturn "
                    + "the one a moderator made — a gate that looked only at live rows would put "
                    + "Berean straight back on the next Approval-Modified");

            // The UNFILTERED read, and NOT the round-keyed one. That second verification is the
            // rule rather than belt and braces: SelectAIReviewerAssignmentByApprovalIdAsync
            // filters IsDeleted == false inside the query, so composing this gate over it would
            // answer false for exactly the round above.
            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAIReviewerAssignmentsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The other half of the same conjunct: a round nobody has ever asked about answers
        /// false, so the automatic assignment has something to do the first time.
        /// </summary>
        [Fact]
        public async Task ShouldFindNoAssignmentWhenNoRowNamesTheRoundAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();

            SetupAIReviewerAssignments(
                CreateAIReviewerAssignment(approvalId: Guid.NewGuid(), isDeleted: false));

            // when
            bool isEverAssigned = await this.accessBroker.IsAIReviewerEverAssignedAsync(
                approvalId: approvalId,
                cancellationToken: default);

            // then
            isEverAssigned.Should().BeFalse(
                because: "no row names this round at all, which is the one answer that may drive "
                    + "the write");
        }
    }
}
