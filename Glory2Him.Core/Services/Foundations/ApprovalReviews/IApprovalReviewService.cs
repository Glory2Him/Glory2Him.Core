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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviews
{
    internal partial interface IApprovalReviewService
    {
        ValueTask<ApprovalReview> AddApprovalReviewAsync(
            ApprovalReview approvalReview,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ApprovalReview>> RetrieveAllApprovalReviewsAsync(
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalReview> RetrieveApprovalReviewByIdAsync(
            Guid approvalReviewId,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalReview> ModifyApprovalReviewAsync(
            ApprovalReview approvalReview,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalReview> RemoveApprovalReviewByIdAsync(
            Guid approvalReviewId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalReview> HardRemoveApprovalReviewByIdAsync(
            Guid approvalReviewId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Dismisses a review (design §9.7.1, §8.8, §9.5). A narrow transition owning exactly
        /// <c>StatusId</c>, driving it to <c>Dismissed</c> — the outcome a review reaches when
        /// an entity-scoped change invalidates it, never a verdict its author declares (which
        /// is why add and modify refuse <c>Dismissed</c>). It is the workflow's act, not the
        /// reviewer's: gated on the publisher tier, not the review role. Refuses a review that
        /// is already dismissed, and publishes <c>ApprovalReview-Dismissed</c> — never
        /// <c>ApprovalReview-Modified</c>, which the workflow subscribes to. The dismissed row
        /// is retained (§9.5) and excluded from the threshold, and the reviewer may file a fresh
        /// review afterwards (§7.7 rule 7).
        /// </summary>
        ValueTask<ApprovalReview> DismissApprovalReviewAsync(
            Guid approvalReviewId,
            CancellationToken cancellationToken = default);
    }
}
