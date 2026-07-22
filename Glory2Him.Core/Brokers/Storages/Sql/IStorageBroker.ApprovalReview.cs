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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<ApprovalReview> InsertApprovalReviewAsync(
            ApprovalReview approvalReview,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ApprovalReview>> SelectAllApprovalReviewsAsync(
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalReview> SelectApprovalReviewByIdAsync(
            Guid approvalReviewId,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalReview> UpdateApprovalReviewAsync(
            ApprovalReview approvalReview,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalReview> DeleteApprovalReviewAsync(
            ApprovalReview approvalReview,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertApprovalReviewsAsync(
            List<ApprovalReview> approvalReviews,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateApprovalReviewsAsync(
            List<ApprovalReview> approvalReviews,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteApprovalReviewsAsync(
            List<ApprovalReview> approvalReviews,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<ApprovalReview>> BulkReadApprovalReviewsAsync(
            List<ApprovalReview> approvalReviews,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertApprovalReviewsAsync(
            List<ApprovalReview> approvalReviews,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsApprovalReviewAsync(
            Guid approvalReviewId,
            CancellationToken cancellationToken = default);
    }
}
