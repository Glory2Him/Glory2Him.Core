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
    /// The state of the approval record a decision is about. <c>Dismissed</c> is absent because it
    /// applies to reviews only — an approval never holds it (§9.5).
    /// </summary>
    public enum ApprovalState
    {
        /// <summary>Not yet ready for review. Nothing is reviewable and nothing can be
        /// approved.</summary>
        Draft = 0,

        /// <summary>
        /// Under review. This is the <b>only</b> state in which a review may be written (§7.7
        /// rule 2b) and the only state from which a decision may be applied.
        /// </summary>
        Submitted = 1,

        /// <summary>The round closed in favour. Reopening it is an edit, not a review.</summary>
        Approved = 2,

        /// <summary>The round closed against. Reopening it is an edit, not a review.</summary>
        Rejected = 3,
    }
}
