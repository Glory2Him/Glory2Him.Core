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

namespace Glory2Him.Core.Models.Orchestrations.Associations
{
    /// <summary>
    /// The outcome of a reader withdrawing their reaction by its pair (§ARC16.8.1). Either way the
    /// reader holds nothing on the pair afterwards, which is the end state they asked for, so a
    /// withdrawal is idempotent and never answers not-found.
    /// </summary>
    public enum AssociationRemovalStatus
    {
        /// <summary>
        /// The reader's live row holding the reaction they named was withdrawn.
        /// </summary>
        Removed,

        /// <summary>
        /// Nothing was withdrawn: the reader holds no row on the item, their row is already
        /// withdrawn, or it holds a different reaction, which is left as it is.
        /// </summary>
        NothingToRemove
    }
}
