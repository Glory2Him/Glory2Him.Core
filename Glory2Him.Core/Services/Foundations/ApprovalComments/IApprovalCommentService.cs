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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.ApprovalComments;

namespace Glory2Him.Core.Services.Foundations.ApprovalComments
{
    internal partial interface IApprovalCommentService
    {
        ValueTask<ApprovalComment> AddApprovalCommentAsync(
            ApprovalComment approvalComment,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ApprovalComment>> RetrieveAllApprovalCommentsAsync(
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalComment> RetrieveApprovalCommentByIdAsync(
            Guid approvalCommentId,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalComment> ModifyApprovalCommentAsync(
            ApprovalComment approvalComment,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalComment> RemoveApprovalCommentByIdAsync(
            Guid approvalCommentId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalComment> HardRemoveApprovalCommentByIdAsync(
            Guid approvalCommentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Records whether the question this comment raised has been answered. Owns
        /// <see cref="ApprovalComment.IsResolved"/> and nothing else — the wording belongs to
        /// whoever wrote it and is only ever changed through modify.
        /// </summary>
        /// <param name="isResolved">
        /// The resolution state to record. Reopening (<c>false</c>) rides the same operation as
        /// resolving: a question wrongly marked answered must be answerable again, or a mistaken
        /// resolve would permanently defeat
        /// <c>RequireReviewCommentResolutionBeforeApprovals</c> for that comment.
        /// </param>
        ValueTask<ApprovalComment> ResolveApprovalCommentAsync(
            Guid approvalCommentId,
            bool isResolved,
            CancellationToken cancellationToken = default);
    }
}
