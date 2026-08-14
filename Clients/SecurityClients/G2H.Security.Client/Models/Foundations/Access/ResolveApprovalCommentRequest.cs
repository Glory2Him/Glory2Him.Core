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
    /// and it is deliberately the only one: resolving records that a question was answered, which
    /// changes no words. <c>UpdatedBy</c> then carries the admin's identity, so the intervention
    /// is visible rather than silent, and the owner may set it back while the round is open.
    /// </para>
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
        /// The <c>CreatedBy</c> of the comment, read from storage rather than from the caller's
        /// payload.
        /// </summary>
        public required string CommentCreatedBy { get; init; }

        /// <summary>
        /// The parent approval's current state. Resolution only means anything while the round is
        /// open — once the approval has closed, the block this flag feeds has already been
        /// evaluated for the last time.
        /// </summary>
        public required ApprovalState ApprovalState { get; init; }
    }
}
