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

namespace Glory2Him.Core.Models.Orchestrations.Approvals
{
    /// <summary>
    /// What one account id is CALLED (design 16.7.4).
    ///
    /// <para><b>The same two fields ReviewerCandidate carries, and deliberately so.</b> Both are
    /// user-enumeration answers, and 16.7.4 already ruled how one of those is exposed: the
    /// requesting tier, an account id and a display name, and nothing a caller could mine. They
    /// stay separate TYPES because they answer different questions - a candidate is somebody who
    /// may be invited, and this is somebody the round already names - and collapsing them would
    /// let a resolver's output be mistaken for an eligibility list.</para>
    ///
    /// <para><b>Why it is not a projection on the review read.</b> The panel needs names for
    /// reviewers, for invited people and for candidates. A display name hung off ApprovalReview
    /// would answer the first surface and leave the next to invent its own, and three lookups are
    /// three chances to disagree. One resolver, asked with whatever ids a surface is holding,
    /// keeps the composition in a single place.</para>
    ///
    /// <para><b>And not a denormalised column either.</b> Storing the name on the row at write
    /// time - the trade ApprovalReviewRequest.RequestedUserDisplayName already made - leaves every
    /// historical row asserting a name its owner has since changed.</para>
    /// </summary>
    public class ReviewerDisplayName
    {
        /// <summary>
        /// The account id, echoed back so a caller can join the answer onto the rows it already
        /// holds without depending on ordering. Read off the resolved account, so it is always the
        /// canonical form rather than whichever spelling was asked with.
        /// </summary>
        public required string UserId { get; init; }

        /// <summary>
        /// What to show - the preferred name, else the full name, else the username. Composed by
        /// the same rule the candidates read uses, which is what stops two surfaces rendering one
        /// person under two names. Presentation only: nothing compares it.
        /// </summary>
        public required string DisplayName { get; init; }
    }
}
