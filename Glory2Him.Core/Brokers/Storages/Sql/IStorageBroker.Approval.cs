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
using Glory2Him.Core.Models.Foundations.Approvals;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<Approval> InsertApprovalAsync(Approval approval);
        ValueTask<IQueryable<Approval>> SelectAllApprovalsAsync();
        ValueTask<Approval> SelectApprovalByIdAsync(Guid approvalId);
        ValueTask<Approval> UpdateApprovalAsync(Approval approval);
        ValueTask<Approval> DeleteApprovalAsync(Approval approval);
        ValueTask BulkInsertApprovalsAsync(List<Approval> approvals);
        ValueTask BulkUpdateApprovalsAsync(List<Approval> approvals);
        ValueTask BulkDeleteApprovalsAsync(List<Approval> approvals);
    }
}
