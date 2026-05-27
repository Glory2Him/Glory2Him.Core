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
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<ApprovalComment> ApprovalComments { get; set; }

        public async ValueTask<ApprovalComment> InsertApprovalCommentAsync(
            ApprovalComment approvalComment,
            CancellationToken cancellationToken = default) =>
            await InsertAsync(approvalComment, cancellationToken);

        public async ValueTask<IQueryable<ApprovalComment>> SelectAllApprovalCommentsAsync() =>
            await SelectAllAsync<ApprovalComment>();

        public async ValueTask<ApprovalComment> SelectApprovalCommentByIdAsync(
            Guid approvalCommentId,
            CancellationToken cancellationToken = default) =>
            await SelectAsync<ApprovalComment>(new object[] { approvalCommentId }, cancellationToken);

        public async ValueTask<ApprovalComment> UpdateApprovalCommentAsync(
            ApprovalComment approvalComment,
            CancellationToken cancellationToken = default) =>
            await UpdateAsync(approvalComment, cancellationToken);

        public async ValueTask<ApprovalComment> DeleteApprovalCommentAsync(
            ApprovalComment approvalComment,
            CancellationToken cancellationToken = default) =>
            await DeleteAsync(approvalComment, cancellationToken);

        public async ValueTask BulkInsertApprovalCommentsAsync(
            List<ApprovalComment> approvalComments,
            CancellationToken cancellationToken = default) =>
            await BulkInsertAsync(approvalComments, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateApprovalCommentsAsync(
            List<ApprovalComment> approvalComments,
            CancellationToken cancellationToken = default) =>
            await BulkUpdateAsync(approvalComments, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteApprovalCommentsAsync(
            List<ApprovalComment> approvalComments,
            CancellationToken cancellationToken = default) =>
            await BulkDeleteAsync(approvalComments, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<ApprovalComment>> BulkReadApprovalCommentsAsync(
            List<ApprovalComment> approvalComments,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(approvalComments, cancellationToken);

        public async ValueTask BulkUpsertApprovalCommentsAsync(
            List<ApprovalComment> approvalComments,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(approvalComments, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsApprovalCommentAsync(
            Guid approvalCommentId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<ApprovalComment>(new object[] { approvalCommentId }, cancellationToken);
    }
}