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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.Approvals;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<Approval> InsertApprovalAsync(Approval approval, CancellationToken cancellationToken = default);
        ValueTask<IQueryable<Approval>> SelectAllApprovalsAsync();
        ValueTask<Approval> SelectApprovalByIdAsync(Guid approvalId, CancellationToken cancellationToken = default);
        ValueTask<Approval> UpdateApprovalAsync(Approval approval, CancellationToken cancellationToken = default);
        ValueTask<Approval> DeleteApprovalAsync(Approval approval, CancellationToken cancellationToken = default);
        ValueTask BulkInsertApprovalsAsync(List<Approval> approvals, CancellationToken cancellationToken = default);
        ValueTask BulkUpdateApprovalsAsync(List<Approval> approvals, CancellationToken cancellationToken = default);
        ValueTask BulkDeleteApprovalsAsync(List<Approval> approvals, CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<Approval>> BulkReadApprovalsAsync(
            List<Approval> approvals,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertApprovalsAsync(
            List<Approval> approvals,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsApprovalAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default);
    }
}
