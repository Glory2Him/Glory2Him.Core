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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        public DbSet<ApprovalReviewRequest> ApprovalReviewRequests { get; set; }

        public async ValueTask<ApprovalReviewRequest> InsertApprovalReviewRequestAsync(
            ApprovalReviewRequest approvalReviewRequest,
            CancellationToken cancellationToken = default) =>
            await InsertAsync(approvalReviewRequest, cancellationToken);

        public async ValueTask<IQueryable<ApprovalReviewRequest>> SelectAllApprovalReviewRequestsAsync(
            CancellationToken cancellationToken = default) =>
            await SelectAllAsync<ApprovalReviewRequest>(cancellationToken);

        public async ValueTask<ApprovalReviewRequest> SelectApprovalReviewRequestByIdAsync(
            Guid approvalReviewRequestId,
            CancellationToken cancellationToken = default) =>
            await SelectAsync<ApprovalReviewRequest>(
                new object[] { approvalReviewRequestId }, cancellationToken);

        public async ValueTask<ApprovalReviewRequest> UpdateApprovalReviewRequestAsync(
            ApprovalReviewRequest approvalReviewRequest,
            CancellationToken cancellationToken = default) =>
            await UpdateAsync(approvalReviewRequest, cancellationToken);

        public async ValueTask<ApprovalReviewRequest> DeleteApprovalReviewRequestAsync(
            ApprovalReviewRequest approvalReviewRequest,
            CancellationToken cancellationToken = default) =>
            await DeleteAsync(approvalReviewRequest, cancellationToken);

        public async ValueTask BulkInsertApprovalReviewRequestsAsync(
            List<ApprovalReviewRequest> approvalReviewRequests,
            CancellationToken cancellationToken = default) =>
            await BulkInsertAsync(approvalReviewRequests, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateApprovalReviewRequestsAsync(
            List<ApprovalReviewRequest> approvalReviewRequests,
            CancellationToken cancellationToken = default) =>
            await BulkUpdateAsync(approvalReviewRequests, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteApprovalReviewRequestsAsync(
            List<ApprovalReviewRequest> approvalReviewRequests,
            CancellationToken cancellationToken = default) =>
            await BulkDeleteAsync(approvalReviewRequests, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<ApprovalReviewRequest>> BulkReadApprovalReviewRequestsAsync(
            List<ApprovalReviewRequest> approvalReviewRequests,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(approvalReviewRequests, cancellationToken);

        public async ValueTask BulkUpsertApprovalReviewRequestsAsync(
            List<ApprovalReviewRequest> approvalReviewRequests,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(approvalReviewRequests, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsApprovalReviewRequestAsync(
            Guid approvalReviewRequestId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<ApprovalReviewRequest>(
                new object[] { approvalReviewRequestId }, cancellationToken);
    }
}
