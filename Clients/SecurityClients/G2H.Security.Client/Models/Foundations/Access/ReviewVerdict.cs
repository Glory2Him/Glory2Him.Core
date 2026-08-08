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
    /// What a single review currently says.
    ///
    /// <para>This is deliberately narrower than the consuming application's approval-status enum,
    /// which also carries <c>Draft</c> and <c>Submitted</c>. A review can never hold either, so
    /// admitting them here would mean writing decision code for states that cannot occur — and
    /// worse, deciding what such a review should count as. Three members make the impossible
    /// unrepresentable and leave the counting rules with no undefined case.</para>
    /// </summary>
    public enum ReviewVerdict
    {
        /// <summary>The reviewer vouched for the content. Counts toward the threshold.</summary>
        Approved = 0,

        /// <summary>The reviewer refused it. Never counts toward the threshold, and blocks
        /// outright when the policy says so (§8.7).</summary>
        Rejected = 1,

        /// <summary>
        /// The verdict was invalidated by a change to the content it described (§9.5). Retained
        /// for audit; never counts (§8.5 rule 3). It is what <i>happens to</i> a review, never
        /// something its author declares.
        /// </summary>
        Dismissed = 2,
    }
}
