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
    /// One review on the approval, reduced to the four fields any rule here consults. The comment
    /// text, timestamps and identifiers stay behind — a decision function should not be handed
    /// what it cannot use.
    /// </summary>
    public class ReviewRecord
    {
        /// <summary>
        /// The reviewer this verdict belongs to.
        /// </summary>
        public required string ReviewerId { get; init; }

        /// <summary>
        /// Who wrote the row.
        ///
        /// <para>Carried <i>alongside</i> <see cref="ReviewerId"/> rather than assumed equal to it,
        /// because the rule that bars reviewing and deciding the same round attaches to either
        /// (§8.6 regardless-rule 1). They are bound to the same user on the paths that exist
        /// today, but a rule written against only one of them would be a rule with a hole in it
        /// the moment that stops being true.</para>
        /// </summary>
        public required string CreatedBy { get; init; }

        /// <summary>
        /// What the review currently says.
        /// </summary>
        public required ReviewVerdict Verdict { get; init; }

        /// <summary>
        /// Whether the row is soft-deleted.
        ///
        /// <para>Deleted rows are passed in rather than filtered out by the caller, so that
        /// "which reviews count" is decided in one place and can be tested. A caller that
        /// filtered first would be making half the decision silently.</para>
        /// </summary>
        public required bool IsDeleted { get; init; }
    }
}
