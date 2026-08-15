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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using CoreTag = Glory2Him.Core.Models.Foundations.Tags.Tag;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// Arrangement for the approve endpoint. The approve decision reads the APPROVAL row's
    /// status, not the tag's, and no endpoint in this host creates that row — submitting a tag
    /// writes only <c>Tag.ApprovalStatus</c>, and the approval round would normally be opened by
    /// an approval orchestration reacting to the published fact. So the round has to be arranged
    /// beneath HTTP. These are real rows written through the host's own storage broker, read
    /// back by the production <c>AccessBroker</c> through the production <c>StorageBroker</c>.
    /// </summary>
    public partial class ApiBroker
    {
        public async ValueTask<CoreTag> InsertSubmittedTagAsync(string authorUserId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var tag = new CoreTag
            {
                Id = Guid.NewGuid(),
                Name = Guid.NewGuid().ToString("N").Substring(0, 30),
                ApprovalStatus = ApprovalStatus.Submitted,
                IsPublished = false,
                IsDeleted = false,
                CreatedBy = authorUserId,
                CreatedWhen = now,
                UpdatedBy = authorUserId,
                UpdatedWhen = now
            };

            return await this.storageBroker.InsertTagAsync(tag);
        }

        public async ValueTask<Approval> InsertSubmittedApprovalAsync(
            Guid tagId,
            string authorUserId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var approval = new Approval
            {
                Id = Guid.NewGuid(),
                EntityType = EntityType.Tag,
                EntityId = tagId,
                ApprovalStatus = ApprovalStatus.Submitted,
                CreatedBy = authorUserId,
                CreatedWhen = now,
                UpdatedBy = authorUserId,
                UpdatedWhen = now
            };

            return await this.storageBroker.InsertApprovalAsync(approval);
        }

        /// <summary>
        /// The approving caller must hold no active review of their own — a publisher who filed
        /// a review has spent their vote on the round — so the reviewer here is a third party.
        /// </summary>
        public async ValueTask<ApprovalReview> InsertApprovedReviewAsync(
            Guid approvalId,
            string reviewerUserId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var approvalReview = new ApprovalReview
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                StatusId = ApprovalStatus.Approved,
                Comment = "Arranged by the acceptance suite.",
                IsDeleted = false,
                CreatedBy = reviewerUserId,
                CreatedWhen = now,
                UpdatedBy = reviewerUserId,
                UpdatedWhen = now
            };

            return await this.storageBroker.InsertApprovalReviewAsync(approvalReview);
        }

        public async ValueTask RemoveApprovalReviewAsync(ApprovalReview approvalReview) =>
            await this.storageBroker.DeleteApprovalReviewAsync(approvalReview);

        public async ValueTask RemoveApprovalAsync(Approval approval) =>
            await this.storageBroker.DeleteApprovalAsync(approval);

        public async ValueTask<CoreTag> GetCoreTagByIdAsync(Guid tagId) =>
            await this.storageBroker.SelectTagByIdAsync(tagId);

        public async ValueTask RemoveCoreTagAsync(CoreTag tag) =>
            await this.storageBroker.DeleteTagAsync(tag);
    }
}
