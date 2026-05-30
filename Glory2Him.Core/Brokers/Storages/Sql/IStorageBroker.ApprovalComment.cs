// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.ApprovalComments;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<ApprovalComment> InsertApprovalCommentAsync(
            ApprovalComment approvalComment,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ApprovalComment>> SelectAllApprovalCommentsAsync();

        ValueTask<ApprovalComment> SelectApprovalCommentByIdAsync(
            Guid approvalCommentId,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalComment> UpdateApprovalCommentAsync(
            ApprovalComment approvalComment,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalComment> DeleteApprovalCommentAsync(
            ApprovalComment approvalComment,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertApprovalCommentsAsync(
            List<ApprovalComment> approvalComments,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateApprovalCommentsAsync(
            List<ApprovalComment> approvalComments,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteApprovalCommentsAsync(
            List<ApprovalComment> approvalComments,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<ApprovalComment>> BulkReadApprovalCommentsAsync(
            List<ApprovalComment> approvalComments,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertApprovalCommentsAsync(
            List<ApprovalComment> approvalComments,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsApprovalCommentAsync(
            Guid approvalCommentId,
            CancellationToken cancellationToken = default);
    }
}
