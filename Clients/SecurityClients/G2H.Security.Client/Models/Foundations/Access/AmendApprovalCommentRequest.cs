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
        /// The <c>CreatedBy</c> of the comment being acted on.
        ///
        /// <para><b>The caller must read this from storage, never from the request payload.</b>
        /// This client is a pure function and cannot fetch it — a payload-supplied value would
        /// let a caller nominate themselves as the author of someone else's row and pass the
        /// ownership gate. <c>AccessBroker</c> passes it through rather than reading it, so the
        /// obligation lands on the <b>foundation service</b>, which already loads the stored
        /// comment before it asks. Nothing in this project or the broker can enforce it.</para>
        /// </summary>
        public required string CommentCreatedBy { get; init; }

        /// <summary>
        /// The parent approval's current state. A closed round closes its comments with it, so a
        /// comment cannot be edited or withdrawn after the approval reaches <c>Approved</c> or
        /// <c>Rejected</c> — the record of what was said stands.
        /// </summary>
        public required ApprovalState ApprovalState { get; init; }

        /// <summary>
        /// Whether the parent approval is soft-deleted.
        ///
        /// <para>Asked here for the same reason it is asked on
        /// <see cref="RecordApprovalCommentRequest"/>: the foreign key cannot answer it, because
        /// deletion is a flag and the row stays (§10.4). A taken-down approval accepts no new
        /// comments, and its existing ones stop being changed or withdrawn with it — otherwise a
        /// comment thread would go on living under an approval that no longer exists to anyone.
        /// </para>
        /// </summary>
        public required bool IsParentApprovalDeleted { get; init; }
    }
}
