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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.WebApp.Tests.Acceptance.Models.Comments;
using CoreApprovalStatus = Glory2Him.Core.Models.Enums.ApprovalStatus;
using CoreComment = Glory2Him.Core.Models.Foundations.Comments.Comment;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Comments
{
    public partial class CommentApiTests
    {
        [Fact]
        public async Task ShouldTransitionCommentApprovalAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();
            string reviewerUserId = Guid.NewGuid().ToString();
            string secondReviewerUserId = Guid.NewGuid().ToString();

            CoreComment submittedComment =
                await this.apiBroker.InsertSubmittedCommentAsync(authorUserId);

            Approval approval =
                await this.apiBroker.InsertSubmittedApprovalAsync(
                    EntityType.Comment, submittedComment.Id, authorUserId);

            ApprovalReview approvalReview =
                await this.apiBroker.InsertApprovedReviewAsync(approval.Id, reviewerUserId);

            // The seeded default policy requires TWO approvals (ApprovalSettingSeedData), each
            // from a reviewer who is not the author — one standing approval leaves the round
            // one short and the transition is refused rather than made.
            ApprovalReview secondApprovalReview =
                await this.apiBroker.InsertApprovedReviewAsync(approval.Id, secondReviewerUserId);

            Comment inputComment = await this.apiBroker.GetCommentByIdAsync(submittedComment.Id);
            inputComment.ApprovalStatus = ApprovalStatus.Approved;
            inputComment.IsPublished = true;

            try
            {
                // when
                Comment actualComment = await this.apiBroker.TransitionCommentApprovalAsync(inputComment);

                // then
                actualComment.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
                actualComment.IsPublished.Should().BeTrue();
                actualComment.IsApprovedByBypass.Should().BeFalse();

                CoreComment storedComment = await this.apiBroker.GetCoreCommentByIdAsync(submittedComment.Id);
                storedComment.ApprovalStatus.Should().Be(CoreApprovalStatus.Approved);
                storedComment.IsPublished.Should().BeTrue();
            }
            finally
            {
                // In FK order, and outside the assertions — the arranged rows have no owning
                // endpoint, so a failure here would orphan an Approval and an ApprovalReview in
                // a database nothing else resets.
                await this.apiBroker.RemoveApprovalReviewAsync(secondApprovalReview);
                await this.apiBroker.RemoveApprovalReviewAsync(approvalReview);
                await this.apiBroker.RemoveApprovalAsync(approval);
                await this.apiBroker.RemoveCoreCommentAsync(
                    await this.apiBroker.GetCoreCommentByIdAsync(submittedComment.Id));
            }
        }

        [Fact]
        public async Task ShouldRejectCommentAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();

            CoreComment submittedComment =
                await this.apiBroker.InsertSubmittedCommentAsync(authorUserId);

            Approval approval =
                await this.apiBroker.InsertSubmittedApprovalAsync(
                    EntityType.Comment, submittedComment.Id, authorUserId);

            Comment inputComment = await this.apiBroker.GetCommentByIdAsync(submittedComment.Id);
            inputComment.ApprovalStatus = ApprovalStatus.Rejected;

            try
            {
                // when
                Comment actualComment = await this.apiBroker.TransitionCommentApprovalAsync(inputComment);

                // then
                actualComment.ApprovalStatus.Should().Be(ApprovalStatus.Rejected);
                actualComment.IsPublished.Should().BeFalse();

                CoreComment storedComment = await this.apiBroker.GetCoreCommentByIdAsync(submittedComment.Id);
                storedComment.ApprovalStatus.Should().Be(CoreApprovalStatus.Rejected);
            }
            finally
            {
                await this.apiBroker.RemoveApprovalAsync(approval);
                await this.apiBroker.RemoveCoreCommentAsync(
                    await this.apiBroker.GetCoreCommentByIdAsync(submittedComment.Id));
            }
        }
    }
}
