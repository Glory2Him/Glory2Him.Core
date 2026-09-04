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
using Glory2Him.WebApp.Tests.Acceptance.Models.Reactions;
using CoreApprovalStatus = Glory2Him.Core.Models.Enums.ApprovalStatus;
using CoreReaction = Glory2Him.Core.Models.Foundations.Reactions.Reaction;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Reactions
{
    public partial class ReactionApiTests
    {
        [Fact]
        public async Task ShouldTransitionReactionApprovalAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();
            string reviewerUserId = Guid.NewGuid().ToString();
            string secondReviewerUserId = Guid.NewGuid().ToString();

            CoreReaction submittedReaction =
                await this.apiBroker.InsertSubmittedReactionAsync(authorUserId);

            Approval approval =
                await this.apiBroker.InsertSubmittedApprovalAsync(
                    EntityType.Reaction, submittedReaction.Id, authorUserId);

            ApprovalReview approvalReview =
                await this.apiBroker.InsertApprovedReviewAsync(approval.Id, reviewerUserId);

            // The seeded default policy requires TWO approvals (ApprovalSettingSeedData), each
            // from a reviewer who is not the author — one standing approval leaves the round
            // one short and the transition is refused rather than made.
            ApprovalReview secondApprovalReview =
                await this.apiBroker.InsertApprovedReviewAsync(approval.Id, secondReviewerUserId);

            Reaction inputReaction = await this.apiBroker.GetReactionByIdAsync(submittedReaction.Id);
            inputReaction.ApprovalStatus = ApprovalStatus.Approved;
            inputReaction.IsPublished = true;

            try
            {
                // when
                Reaction actualReaction = await this.apiBroker.TransitionReactionApprovalAsync(inputReaction);

                // then
                actualReaction.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
                actualReaction.IsPublished.Should().BeTrue();
                actualReaction.IsApprovedByBypass.Should().BeFalse();

                CoreReaction storedReaction = await this.apiBroker.GetCoreReactionByIdAsync(submittedReaction.Id);
                storedReaction.ApprovalStatus.Should().Be(CoreApprovalStatus.Approved);
                storedReaction.IsPublished.Should().BeTrue();
            }
            finally
            {
                // In FK order, and outside the assertions — the arranged rows have no owning
                // endpoint, so a failure here would orphan an Approval and an ApprovalReview in
                // a database nothing else resets.
                await this.apiBroker.RemoveApprovalReviewAsync(secondApprovalReview);
                await this.apiBroker.RemoveApprovalReviewAsync(approvalReview);
                await this.apiBroker.RemoveApprovalAsync(approval);
                await this.apiBroker.RemoveCoreReactionAsync(
                    await this.apiBroker.GetCoreReactionByIdAsync(submittedReaction.Id));
            }
        }

        [Fact]
        public async Task ShouldRejectReactionAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();

            CoreReaction submittedReaction =
                await this.apiBroker.InsertSubmittedReactionAsync(authorUserId);

            Approval approval =
                await this.apiBroker.InsertSubmittedApprovalAsync(
                    EntityType.Reaction, submittedReaction.Id, authorUserId);

            Reaction inputReaction = await this.apiBroker.GetReactionByIdAsync(submittedReaction.Id);
            inputReaction.ApprovalStatus = ApprovalStatus.Rejected;

            try
            {
                // when
                Reaction actualReaction = await this.apiBroker.TransitionReactionApprovalAsync(inputReaction);

                // then
                actualReaction.ApprovalStatus.Should().Be(ApprovalStatus.Rejected);
                actualReaction.IsPublished.Should().BeFalse();

                CoreReaction storedReaction = await this.apiBroker.GetCoreReactionByIdAsync(submittedReaction.Id);
                storedReaction.ApprovalStatus.Should().Be(CoreApprovalStatus.Rejected);
            }
            finally
            {
                await this.apiBroker.RemoveApprovalAsync(approval);
                await this.apiBroker.RemoveCoreReactionAsync(
                    await this.apiBroker.GetCoreReactionByIdAsync(submittedReaction.Id));
            }
        }
    }
}
