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
using CoreApprovalReview = Glory2Him.Core.Models.Foundations.ApprovalReviews.ApprovalReview;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// Arrangement for the approval-review endpoints. A review hangs off an <c>Approval</c> by a
    /// real foreign key, and the parent round is arranged through <c>InsertOpenApprovalAsync</c>
    /// beneath HTTP because no endpoint in this host opens a round.
    ///
    /// <para>What this file adds is the pieces only storage can arrange: a review whose
    /// <c>CreatedBy</c> is a DIFFERENT user from the caller — the API binds <c>CreatedBy</c> to
    /// the acting user, so the owner-only and dismissal cases cannot be set up over HTTP — and an
    /// idempotent physical teardown.</para>
    /// </summary>
    public partial class ApiBroker
    {
        /// <summary>
        /// A review recorded by someone else, at whatever verdict the test needs. Used for the
        /// owner-only refusals and for the dismissal cases, neither of which the caller could
        /// arrange for themselves.
        /// </summary>
        public async ValueTask<CoreApprovalReview> InsertReviewByAsync(
            Guid approvalId,
            string reviewerUserId,
            ApprovalStatus statusId = ApprovalStatus.Approved)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var approvalReview = new CoreApprovalReview
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                StatusId = statusId,
                Comment = "Arranged by the acceptance suite.",
                IsDeleted = false,
                CreatedBy = reviewerUserId,
                CreatedWhen = now,
                UpdatedBy = reviewerUserId,
                UpdatedWhen = now
            };

            return await this.storageBroker.InsertApprovalReviewAsync(approvalReview);
        }


        public async ValueTask<Glory2Him.Core.Models.Foundations.Approvals.Approval>
            GetCoreApprovalByIdAsync(Guid approvalId) =>
            await this.storageBroker.SelectApprovalByIdAsync(approvalId);

        public async ValueTask<CoreApprovalReview> GetCoreApprovalReviewByIdAsync(Guid approvalReviewId) =>
            await this.storageBroker.SelectApprovalReviewByIdAsync(approvalReviewId);

        /// <summary>
        /// Physically removes a review if it is still there, whatever state it is in — the
        /// counterpart of <c>RemoveCoreApprovalCommentByIdAsync</c> and for the same reasons: the
        /// API's own delete is a SOFT delete, so a test that tore down through the endpoint left a
        /// soft-deleted row behind, and one whose assertion threw left a live one.
        /// </summary>
        public async ValueTask RemoveCoreApprovalReviewByIdAsync(Guid approvalReviewId)
        {
            CoreApprovalReview storedApprovalReview =
                await this.storageBroker.SelectApprovalReviewByIdAsync(approvalReviewId);

            if (storedApprovalReview is not null)
            {
                await this.storageBroker.DeleteApprovalReviewAsync(storedApprovalReview);
            }
        }
    }
}
