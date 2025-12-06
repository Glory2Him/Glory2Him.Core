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
using Glory2Him.Core.Models.Foundations.ApprovalComments;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<ApprovalComment> InsertApprovalCommentAsync(ApprovalComment approvalComment);
        ValueTask<IQueryable<ApprovalComment>> SelectAllApprovalCommentsAsync();
        ValueTask<ApprovalComment> SelectApprovalCommentByIdAsync(Guid approvalCommentId);
        ValueTask<ApprovalComment> UpdateApprovalCommentAsync(ApprovalComment approvalComment);
        ValueTask<ApprovalComment> DeleteApprovalCommentAsync(ApprovalComment approvalComment);
        ValueTask BulkInsertApprovalCommentsAsync(List<ApprovalComment> approvalComments);
        ValueTask BulkUpdateApprovalCommentsAsync(List<ApprovalComment> approvalComments);
        ValueTask BulkDeleteApprovalCommentsAsync(List<ApprovalComment> approvalComments);
    }
}
