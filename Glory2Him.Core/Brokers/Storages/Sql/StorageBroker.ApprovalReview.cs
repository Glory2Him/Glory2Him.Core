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
using EFxceptions;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<ApprovalReview> ApprovalReviews { get; set; }

        public async ValueTask<ApprovalReview> InsertApprovalReviewAsync(
            ApprovalReview approvalReview,
            CancellationToken cancellationToken = default) =>
            await InsertAsync(approvalReview, cancellationToken);

        public async ValueTask<IQueryable<ApprovalReview>> SelectAllApprovalReviewsAsync() =>
            await SelectAllAsync<ApprovalReview>();

        public async ValueTask<ApprovalReview> SelectApprovalReviewByIdAsync(
            Guid approvalReviewId,
            CancellationToken cancellationToken = default) =>
            await SelectAsync<ApprovalReview>(new object[] { approvalReviewId }, cancellationToken);

        public async ValueTask<ApprovalReview> UpdateApprovalReviewAsync(
            ApprovalReview approvalReview,
            CancellationToken cancellationToken = default) =>
            await UpdateAsync(approvalReview, cancellationToken);

        public async ValueTask<ApprovalReview> DeleteApprovalReviewAsync(
            ApprovalReview approvalReview,
            CancellationToken cancellationToken = default) =>
            await DeleteAsync(approvalReview, cancellationToken);

        public async ValueTask BulkInsertApprovalReviewsAsync(
            List<ApprovalReview> approvalReviews,
            CancellationToken cancellationToken = default) =>
            await BulkInsertAsync(approvalReviews, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateApprovalReviewsAsync(
            List<ApprovalReview> approvalReviews,
            CancellationToken cancellationToken = default) =>
            await BulkUpdateAsync(approvalReviews, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteApprovalReviewsAsync(
            List<ApprovalReview> approvalReviews,
            CancellationToken cancellationToken = default) =>
            await BulkDeleteAsync(approvalReviews, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<ApprovalReview>> BulkReadApprovalReviewsAsync(
            List<ApprovalReview> approvalReviews,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(approvalReviews, cancellationToken);

        public async ValueTask BulkUpsertApprovalReviewsAsync(
            List<ApprovalReview> approvalReviews,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(approvalReviews, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsApprovalReviewAsync(
            Guid approvalReviewId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<ApprovalReview>(new object[] { approvalReviewId }, cancellationToken);
    }
}