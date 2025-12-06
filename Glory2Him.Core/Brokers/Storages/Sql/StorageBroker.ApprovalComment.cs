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
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<ApprovalComment> ApprovalComments { get; set; }

        public async ValueTask<ApprovalComment> InsertApprovalCommentAsync(ApprovalComment approvalComment) =>
            await InsertAsync(approvalComment);

        public async ValueTask<IQueryable<ApprovalComment>> SelectAllApprovalCommentsAsync() =>
            await SelectAllAsync<ApprovalComment>();

        public async ValueTask<ApprovalComment> SelectApprovalCommentByIdAsync(Guid approvalCommentId) =>
            await SelectAsync<ApprovalComment>(approvalCommentId);

        public async ValueTask<ApprovalComment> UpdateApprovalCommentAsync(ApprovalComment approvalComment) =>
            await UpdateAsync(approvalComment);

        public async ValueTask<ApprovalComment> DeleteApprovalCommentAsync(ApprovalComment approvalComment) =>
            await DeleteAsync(approvalComment);

        public async ValueTask BulkInsertApprovalCommentsAsync(List<ApprovalComment> approvalComments) =>
            await BulkInsertAsync(approvalComments);

        public async ValueTask BulkUpdateApprovalCommentsAsync(List<ApprovalComment> approvalComments) =>
            await BulkUpdateAsync(approvalComments);

        public async ValueTask BulkDeleteApprovalCommentsAsync(List<ApprovalComment> approvalComments) =>
            await BulkDeleteAsync(approvalComments);
    }
}