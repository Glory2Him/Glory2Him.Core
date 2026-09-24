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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Comments;

namespace Glory2Him.Core.Services.Foundations.Comments
{
    public partial interface ICommentService
    {
        ValueTask<Comment> AddCommentAsync(
            Comment comment,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<Comment>> RetrieveAllCommentsAsync(
            CancellationToken cancellationToken = default);

        ValueTask<Comment> RetrieveCommentByIdAsync(
            Guid commentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The same caller-filtered read, taking the envelope the reader is acting under so the
        /// ORIGINAL caller's identity is CARRIED rather than re-asserted — the twin of
        /// <c>IContentItemService.RetrieveContentItemByIdAsync(id, inboundEnvelope, ct)</c>, and
        /// internal for the same reason: a public member taking a caller-supplied context is a
        /// forgery surface.
        ///
        /// <para>Used by <c>AssociationOrchestrationService</c> to resolve an endpoint on the
        /// <c>Association-Adding</c> event path (#631). The overload above mints its own envelope,
        /// which reads the AMBIENT caller: on a delivery that is nobody, or whoever PUBLISHED —
        /// never necessarily the subject the envelope was signed for. A read whose answer depends
        /// on who is asking is passed the envelope it is being made under (§ARC12.5.2).</para>
        /// </summary>
        internal ValueTask<Comment> RetrieveCommentByIdAsync<TSource>(
            Guid commentId,
            EventEnvelope<TSource> inboundEnvelope,
            CancellationToken cancellationToken = default);

        ValueTask<Comment> ModifyCommentAsync(
            Comment comment,
            CancellationToken cancellationToken = default);

        ValueTask<Comment> RemoveCommentByIdAsync(
            Guid commentId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        ValueTask<Comment> HardRemoveCommentByIdAsync(
            Guid commentId,
            CancellationToken cancellationToken = default);

        ValueTask<Comment> SubmitCommentByIdAsync(
            Guid commentId,
            CancellationToken cancellationToken = default);

        ValueTask<Comment> TransitionCommentApprovalAsync(
            Comment comment,
            CancellationToken cancellationToken = default);
    }
}
