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
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        public DbSet<Approval> Approvals { get; set; }

        public async ValueTask<Approval> InsertApprovalAsync(
            Approval approval,
            CancellationToken cancellationToken = default) =>
            await InsertAsync(approval, cancellationToken);

        public async ValueTask<IQueryable<Approval>> SelectAllApprovalsAsync(
            CancellationToken cancellationToken = default) =>
            await SelectAllAsync<Approval>(cancellationToken);

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