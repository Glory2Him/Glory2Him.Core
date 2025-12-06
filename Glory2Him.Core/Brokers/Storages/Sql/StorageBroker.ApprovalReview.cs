// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFxceptions;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<ApprovalReview> ApprovalReviews { get; set; }

        public async ValueTask<ApprovalReview> InsertApprovalReviewAsync(ApprovalReview approvalReview) =>
            await InsertAsync(approvalReview);

        public async ValueTask<IQueryable<ApprovalReview>> SelectAllApprovalReviewsAsync() =>
            await SelectAllAsync<ApprovalReview>();

        public async ValueTask<ApprovalReview> SelectApprovalReviewByIdAsync(Guid approvalReviewId) =>
            await SelectAsync<ApprovalReview>(approvalReviewId);

        public async ValueTask<ApprovalReview> UpdateApprovalReviewAsync(ApprovalReview approvalReview) =>
            await UpdateAsync(approvalReview);

        public async ValueTask<ApprovalReview> DeleteApprovalReviewAsync(ApprovalReview approvalReview) =>
            await DeleteAsync(approvalReview);

        public async ValueTask BulkInsertApprovalReviewsAsync(List<ApprovalReview> approvalReviews) =>
            await BulkInsertAsync(approvalReviews);

        public async ValueTask BulkUpdateApprovalReviewsAsync(List<ApprovalReview> approvalReviews) =>
            await BulkUpdateAsync(approvalReviews);

        public async ValueTask BulkDeleteApprovalReviewsAsync(List<ApprovalReview> approvalReviews) =>
            await BulkDeleteAsync(approvalReviews);
    }
}