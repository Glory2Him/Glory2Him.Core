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
using Glory2Him.Core.Models.Foundations.Approvals;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<Approval> Approvals { get; set; }

        public async ValueTask<Approval> InsertApprovalAsync(Approval approval) =>
            await InsertAsync(approval);

        public async ValueTask<IQueryable<Approval>> SelectAllApprovalsAsync() =>
            await SelectAllAsync<Approval>();

        public async ValueTask<Approval> SelectApprovalByIdAsync(Guid approvalId) =>
            await SelectAsync<Approval>(approvalId);

        public async ValueTask<Approval> UpdateApprovalAsync(Approval approval) =>
            await UpdateAsync(approval);

        public async ValueTask<Approval> DeleteApprovalAsync(Approval approval) =>
            await DeleteAsync(approval);

        public async ValueTask BulkInsertApprovalsAsync(List<Approval> approvals) =>
            await BulkInsertAsync(approvals);

        public async ValueTask BulkUpdateApprovalsAsync(List<Approval> approvals) =>
            await BulkUpdateAsync(approvals);

        public async ValueTask BulkDeleteApprovalsAsync(List<Approval> approvals) =>
            await BulkDeleteAsync(approvals);
    }
}