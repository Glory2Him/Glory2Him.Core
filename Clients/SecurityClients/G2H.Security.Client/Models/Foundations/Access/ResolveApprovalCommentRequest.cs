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
    /// Everything the "may this actor mark this comment resolved or unresolved?" decision
    /// consults.
    ///
    /// <para>Separate from <see cref="AmendApprovalCommentRequest"/> because it is a separate operation
    /// with a narrower field scope — it owns <c>IsResolved</c> and nothing else (§9.7.1 rule 3,
    /// §10.2 rule 7, the same reason <c>Submit</c>, <c>Approve</c> and <c>Dismiss</c> exist).
    /// Splitting it is what keeps the amend gate owner-only while still letting an <c>Admin</c>
    /// clear a resolution flag, without a "which fields may I touch here" branch inside amend.
    /// </para>
    ///
    /// <para>This is the one comment operation an <c>Admin</c> may perform on someone else's row,
    /// and it is deliberately the only one: resolving records that a comment is settled — that it
    /// no longer requires anything before the approval can proceed — which changes no words.
    /// <c>UpdatedBy</c> then carries the admin's identity, so the intervention is visible rather
    /// than silent, and the owner may set it back while the round is open.</para>
    ///
    /// <para>Every property is <c>required</c> for the reason given on
    /// <see cref="ApprovalConditionsRequest"/>.</para>
    /// </summary>
    public class ResolveApprovalCommentRequest
    {
        /// <summary>
        /// The user attempting to change the resolution flag.
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
        /// The parent approval's current state. Resolution only means anything while the round is
        /// open — once the approval has closed, the block this flag feeds has already been
        /// evaluated for the last time.
        /// </summary>
        public required ApprovalState ApprovalState { get; init; }

        /// <summary>
        /// Whether the parent approval is soft-deleted.
        ///
        /// <para>Asked here for the same reason it is asked on
        /// <see cref="RecordApprovalCommentRequest"/>: the foreign key cannot answer it, because
        /// deletion is a flag and the row stays (§10.4). A taken-down approval accepts no new
        /// comments, and its existing ones stop being resolved with it — otherwise a
        /// comment thread would go on living under an approval that no longer exists to anyone.
        /// </para>
        /// </summary>
        public required bool IsParentApprovalDeleted { get; init; }
    }
}
