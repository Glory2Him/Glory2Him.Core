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
    /// One approval comment, reduced to the two fields the comment-resolution rule consults.
    ///
    /// <para>The comment <i>text</i> deliberately does not cross. Whether a comment is still
    /// outstanding is answered by <see cref="IsResolved"/>, and handing a decision function prose
    /// it cannot act on invites someone to start reading it.</para>
    /// </summary>
    public class ApprovalCommentRecord
    {
        /// <summary>
        /// Whether this comment is settled — whether it still requires something before the
        /// approval can proceed. An outstanding comment blocks the approval as a whole when the
        /// policy requires resolution — never an individual reviewer's verdict (§8.5 rule 7).
        ///
        /// <para>Not every comment asks for anything: an observation, or a reviewer recording
        /// rationale for others to see, is created settled and never blocks. So a <c>true</c>
        /// here does not imply anyone resolved anything.</para>
        /// </summary>
        public required bool IsResolved { get; init; }

        /// <summary>
        /// Whether the row is soft-deleted. A withdrawn comment is not an outstanding one, so a
        /// deleted comment does not block — but the caller passes it rather than filtering, so
        /// the rule lives in one place.
        /// </summary>
        public required bool IsDeleted { get; init; }
    }
}
