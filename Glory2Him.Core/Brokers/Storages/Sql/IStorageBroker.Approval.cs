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
