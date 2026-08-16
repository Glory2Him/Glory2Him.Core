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
using Glory2Him.WebApp.Tests.Acceptance.Models.Tags;
using CoreApprovalStatus = Glory2Him.Core.Models.Enums.ApprovalStatus;
using CoreTag = Glory2Him.Core.Models.Foundations.Tags.Tag;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Tags
{
    public partial class TagApiTests
    {
        [Fact]
        public async Task ShouldApproveTagAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();
            string reviewerUserId = Guid.NewGuid().ToString();

            CoreTag submittedTag =
                await this.apiBroker.InsertSubmittedTagAsync(authorUserId);

            Approval approval =
                await this.apiBroker.InsertSubmittedApprovalAsync(submittedTag.Id, authorUserId);

            ApprovalReview approvalReview =
                await this.apiBroker.InsertApprovedReviewAsync(approval.Id, reviewerUserId);

            Tag inputTag = await this.apiBroker.GetTagByIdAsync(submittedTag.Id);
            inputTag.ApprovalStatus = ApprovalStatus.Approved;
            inputTag.IsPublished = true;

            try
            {
                // when
                Tag actualTag = await this.apiBroker.ApproveTagAsync(inputTag);

                // then
                actualTag.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
                actualTag.IsPublished.Should().BeTrue();
                actualTag.IsApprovedByBypass.Should().BeFalse();

                CoreTag storedTag = await this.apiBroker.GetCoreTagByIdAsync(submittedTag.Id);
                storedTag.ApprovalStatus.Should().Be(CoreApprovalStatus.Approved);
                storedTag.IsPublished.Should().BeTrue();
            }
            finally
            {
                // In FK order, and outside the assertions — the arranged rows have no owning
                // endpoint, so a failure here would orphan an Approval and an ApprovalReview in
                // a database nothing else resets.
                await this.apiBroker.RemoveApprovalReviewAsync(approvalReview);
                await this.apiBroker.RemoveApprovalAsync(approval);
                await this.apiBroker.RemoveCoreTagAsync(
                    await this.apiBroker.GetCoreTagByIdAsync(submittedTag.Id));
            }
        }

        [Fact]
        public async Task ShouldRejectTagAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();

            CoreTag submittedTag =
                await this.apiBroker.InsertSubmittedTagAsync(authorUserId);

            Approval approval =
                await this.apiBroker.InsertSubmittedApprovalAsync(submittedTag.Id, authorUserId);

            Tag inputTag = await this.apiBroker.GetTagByIdAsync(submittedTag.Id);
            inputTag.ApprovalStatus = ApprovalStatus.Rejected;

            try
            {
                // when
                Tag actualTag = await this.apiBroker.ApproveTagAsync(inputTag);

                // then
                actualTag.ApprovalStatus.Should().Be(ApprovalStatus.Rejected);
                actualTag.IsPublished.Should().BeFalse();

                CoreTag storedTag = await this.apiBroker.GetCoreTagByIdAsync(submittedTag.Id);
                storedTag.ApprovalStatus.Should().Be(CoreApprovalStatus.Rejected);
            }
            finally
            {
                await this.apiBroker.RemoveApprovalAsync(approval);
                await this.apiBroker.RemoveCoreTagAsync(
                    await this.apiBroker.GetCoreTagByIdAsync(submittedTag.Id));
            }
        }
    }
}
