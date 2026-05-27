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
using EFxceptions;
using Glory2Him.Core.Models.Foundations.Approvals;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<Approval> Approvals { get; set; }

        public async ValueTask<Approval> InsertApprovalAsync(
            Approval approval,
            CancellationToken cancellationToken = default) =>
            await InsertAsync(approval, cancellationToken);

        public async ValueTask<IQueryable<Approval>> SelectAllApprovalsAsync() =>
            await SelectAllAsync<Approval>();

        public async ValueTask<Approval> SelectApprovalByIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default) =>
            await SelectAsync<Approval>(new object[] { approvalId }, cancellationToken);

        public async ValueTask<Approval> UpdateApprovalAsync(
            Approval approval,
            CancellationToken cancellationToken = default) =>
            await UpdateAsync(approval, cancellationToken);

        public async ValueTask<Approval> DeleteApprovalAsync(
            Approval approval,
            CancellationToken cancellationToken = default) =>
            await DeleteAsync(approval, cancellationToken);

        public async ValueTask BulkInsertApprovalsAsync(
            List<Approval> approvals,
            CancellationToken cancellationToken = default) =>
            await BulkInsertAsync(approvals, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateApprovalsAsync(
            List<Approval> approvals,
            CancellationToken cancellationToken = default) =>
            await BulkUpdateAsync(approvals, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteApprovalsAsync(
            List<Approval> approvals,
            CancellationToken cancellationToken = default) =>
            await BulkDeleteAsync(approvals, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<Approval>> BulkReadApprovalsAsync(
            List<Approval> approvals,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(approvals, cancellationToken);

        public async ValueTask BulkUpsertApprovalsAsync(
            List<Approval> approvals,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(approvals, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsApprovalAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<Approval>(new object[] { approvalId }, cancellationToken);
    }
}