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

namespace Glory2Him.Core.Models.Foundations.Tags
{
    /// <summary>
    /// What the find-or-add did with the name it was handed
    /// (<c>Backend/Foundations/TagService.TagSuggestions.md §3</c> rules 5 and 6).
    /// </summary>
    public enum TagNameOutcome
    {
        /// <summary>A tag that is neither rejected nor soft-deleted holds the name. Nothing was written.</summary>
        Found,

        /// <summary>No tag held the name, and the handed tag was added under a minted id.</summary>
        Added,

        /// <summary>
        /// A rejected or soft-deleted tag holds the name. Nothing was written, and the reason is
        /// logged rather than answered.
        /// </summary>
        Withheld
    }
}
