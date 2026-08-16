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
    public partial interface IApprovalCommentService
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
        /// Records whether this comment is settled — whether it still requires something before
        /// the approval can proceed. Owns <see cref="ApprovalComment.IsResolved"/> and nothing
        /// else: the wording belongs to whoever wrote it and is only ever changed through modify.
        ///
        /// <para>Open to the owner <b>or</b> an <c>Admin</c>. That widening is the operation's
        /// reason to exist: the owner can equally flip the flag through modify, but an
        /// <c>Admin</c> cannot, because modify is owner-only and admitting them there would hand
        /// them the author's words as well (§14.7 rule 5).</para>
        /// </summary>
        /// <param name="isResolved">
        /// The settled state to record. Unsettling (<c>false</c>) rides the same operation, and
        /// is not merely error-correction: a comment recorded as an observation may later turn
        /// out to need action, and one settled prematurely must be able to block again. Without
        /// it, a single mistaken resolve would permanently defeat
        /// <c>RequireReviewCommentResolutionBeforeApprovals</c> for that comment.
        /// </param>
        ValueTask<ApprovalComment> ResolveApprovalCommentAsync(
            Guid approvalCommentId,
            bool isResolved,
            CancellationToken cancellationToken = default);
    }
}
