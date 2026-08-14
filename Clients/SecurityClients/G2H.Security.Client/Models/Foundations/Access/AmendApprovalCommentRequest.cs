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

namespace G2H.Security.Client.Models.Foundations.Access
{
    /// <summary>
    /// Everything the "may this actor change or withdraw this comment?" decision consults.
    ///
    /// <para>Serves both editing the text and soft-deleting the row, because the two ask the same
    /// question: a comment belongs to whoever wrote it, and neither operation is available to
    /// anyone else. They are one decision with two call sites rather than two decisions.</para>
    ///
    /// <para>Notice what is <b>absent</b>: any role at all. No tier widens this — not
    /// <c>Reviewer</c>, not <c>Publisher</c>, not <c>Admin</c>. An <c>Admin</c> who needs to get
    /// past an unresolved comment resolves it (<see cref="ResolveApprovalCommentRequest"/>) or bypasses
    /// the block; neither route rewrites another person's words. Passing roles in would invite a
    /// future rule to widen the gate here, which is exactly what this decision refuses.</para>
    ///
    /// <para>Every property is <c>required</c> for the reason given on
    /// <see cref="ApprovalConditionsRequest"/>.</para>
    /// </summary>
    public class AmendApprovalCommentRequest
    {
        /// <summary>
        /// The user attempting to change or withdraw the comment.
        /// </summary>
        public required AccessActor Actor { get; init; }

        /// <summary>
        /// The <c>CreatedBy</c> of the comment being changed, read from storage rather than from
        /// the caller's payload — a submitted value would let the caller nominate themselves as
        /// the author of someone else's row.
        /// </summary>
        public required string CommentCreatedBy { get; init; }

        /// <summary>
        /// The parent approval's current state. A closed round closes its comments with it, so a
        /// comment cannot be edited or withdrawn after the approval reaches <c>Approved</c> or
        /// <c>Rejected</c> — the record of what was said stands.
        /// </summary>
        public required ApprovalState ApprovalState { get; init; }
    }
}
