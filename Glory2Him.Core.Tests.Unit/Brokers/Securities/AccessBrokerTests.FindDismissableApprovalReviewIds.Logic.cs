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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Moq;
using Xunit;

namespace Glory2Him.Core.Tests.Unit.Brokers.Securities
{
    public partial class AccessBrokerTests
    {
        // The predicate that decides which reviews a content change invalidates. It lives here
        // rather than in the orchestration because the caller-facing read is identity-filtered:
        // an actor with no review role sees only reviews they wrote, and HR-1 forbids reviewing
        // your own content, so an author revising their own submission sees none of the round's
        // approvals. Deciding what to dismiss from that view dismisses nothing and then lets the
        // unfiltered evaluation approve the edit on a review of the replaced text.
        //
        // This is the only place the predicate is exercised. The orchestration's tests mock
        // IAccessBroker, so they can only assert that the flow ASKS — what the answer should be
        // is settled here, against a real broker over a seeded storage broker.
        [Fact]
        public async Task ShouldFindOnlyTheActiveReviewsBelongingToTheApprovalAsync()
        {
            // given: four rows that each fail the filter differently, so a predicate that drops
            // any one clause is distinguishable from one that does not
            Guid approvalId = Guid.NewGuid();
            Guid otherApprovalId = Guid.NewGuid();

            ApprovalReview activeReview = CreateApprovalReview(
                approvalId: approvalId,
                createdBy: "reviewer-one",
                statusId: ApprovalStatus.Approved);

            // Same round, but already dismissed: it no longer counts, so dismissing it again
            // would throw at the transition and abort the reset half-applied.
            ApprovalReview alreadyDismissedReview = CreateApprovalReview(
                approvalId: approvalId,
                createdBy: "reviewer-two",
                statusId: ApprovalStatus.Dismissed);

            // Same round, soft-deleted: withdrawn, and §9.5 keeps the row for audit rather than
            // counting it.
            ApprovalReview softDeletedReview = CreateApprovalReview(
                approvalId: approvalId,
                createdBy: "reviewer-three",
                statusId: ApprovalStatus.Approved,
                isDeleted: true);

            // ANOTHER round entirely. This is the clause whose loss is worst: without it, one
            // author's edit would dismiss every open round's approvals in the table.
            ApprovalReview otherApprovalsReview = CreateApprovalReview(
                approvalId: otherApprovalId,
                createdBy: "reviewer-four",
                statusId: ApprovalStatus.Approved);

            SetupApprovalReviews(
                activeReview,
                alreadyDismissedReview,
                softDeletedReview,
                otherApprovalsReview);

            // when
            List<Guid> actualDismissableReviewIds =
                await this.accessBroker.FindDismissableApprovalReviewIdsAsync(
                    approvalId: approvalId,
                    cancellationToken: default);

            // then
            actualDismissableReviewIds.Should().Equal(new[] { activeReview.Id },
                because: "only a review that is on THIS round, not withdrawn and not already " +
                    "dismissed still carries an approval the edit invalidates");

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalReviewsAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldFindNoDismissableReviewsWhenTheRoundHasNoneAsync()
        {
            // given: a round nobody has reviewed. The caller must be able to tell "nothing to
            // dismiss" from a failure, because the flow proceeds to evaluate either way.
            SetupApprovalReviews();

            // when
            List<Guid> actualDismissableReviewIds =
                await this.accessBroker.FindDismissableApprovalReviewIdsAsync(
                    approvalId: Guid.NewGuid(),
                    cancellationToken: default);

            // then
            actualDismissableReviewIds.Should().BeEmpty(
                because: "an unreviewed round has nothing to invalidate, and that is an empty " +
                    "answer rather than an error");
        }
    }
}
