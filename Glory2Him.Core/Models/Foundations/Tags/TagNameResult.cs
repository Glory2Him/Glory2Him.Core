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

using System;

namespace Glory2Him.Core.Models.Foundations.Tags
{
    /// <summary>
    /// The non-leaking answer of the find-or-add
    /// (<c>Backend/Foundations/TagService.TagSuggestions.md §3</c> rule 8). The read by name
    /// behind it sees rows the read posture hides from the caller — another reader's
    /// non-public tag, a rejected one, a soft-deleted one — so the row itself must never cross
    /// back: it carries another user's authorship and the state a non-owner is told nothing
    /// about. This carries the tag's id and what the find-or-add did, and nothing else, so a
    /// withheld tag's reason stays in the log.
    /// </summary>
    public class TagNameResult
    {
        /// <summary>The id of the tag that holds the name, or of the tag that was added.</summary>
        public Guid Id { get; set; }

        /// <summary>Whether the tag was found, added or withheld.</summary>
        public TagNameOutcome Outcome { get; set; }
    }
}
