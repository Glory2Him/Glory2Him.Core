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

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial interface IStorageBroker
    {
        ValueTask<ApprovalReviewRequest> InsertApprovalReviewRequestAsync(
            ApprovalReviewRequest approvalReviewRequest,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ApprovalReviewRequest>> SelectAllApprovalReviewRequestsAsync(
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalReviewRequest> SelectApprovalReviewRequestByIdAsync(
            Guid approvalReviewRequestId,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalReviewRequest> UpdateApprovalReviewRequestAsync(
            ApprovalReviewRequest approvalReviewRequest,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalReviewRequest> DeleteApprovalReviewRequestAsync(
            ApprovalReviewRequest approvalReviewRequest,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertApprovalReviewRequestsAsync(
            List<ApprovalReviewRequest> approvalReviewRequests,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateApprovalReviewRequestsAsync(
            List<ApprovalReviewRequest> approvalReviewRequests,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteApprovalReviewRequestsAsync(
            List<ApprovalReviewRequest> approvalReviewRequests,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<ApprovalReviewRequest>> BulkReadApprovalReviewRequestsAsync(
            List<ApprovalReviewRequest> approvalReviewRequests,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertApprovalReviewRequestsAsync(
            List<ApprovalReviewRequest> approvalReviewRequests,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsApprovalReviewRequestAsync(
            Guid approvalReviewRequestId,
            CancellationToken cancellationToken = default);
    }
}
