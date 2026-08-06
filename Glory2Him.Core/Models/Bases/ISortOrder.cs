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

namespace Glory2Him.Core.Models.Bases
{
    /// <summary>
    /// Position within an ordered list. Ordering is neither content nor approval state,
    /// so it is its own interface written only by the sort operation (design §9.7.1 rule 4)
    /// — keeping it off <see cref="IApproval"/> means an author can arrange their own list
    /// without fetching a reviewer, and keeps a permanently null column off the entities
    /// that never appear in an ordered list.
    /// </summary>
    public interface ISortOrder
    {
        /// <summary>
        /// Position within the containing list. Null when unordered.
        ///
        /// <para>Values are <b>sparse</b> (100, 200, 300 …), not dense: placing an item
        /// between two others takes the midpoint, so a move rewrites one row rather than
        /// renumbering everything after it. Ties are legal and resolved by the design §11.7
        /// tie-break chain — a unique index would turn every move into a two-step dance to
        /// vacate the target value first.</para>
        /// </summary>
        int? SortOrder { get; set; }
    }
}
