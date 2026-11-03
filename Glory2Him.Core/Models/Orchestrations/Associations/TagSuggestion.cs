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

namespace Glory2Him.Core.Models.Orchestrations.Associations
{
    /// <summary>
    /// A reader's suggestion of a tag for a post, by the tag's name (TagSuggestions.md rule 1):
    /// the post's id and the name as it was typed, and nothing else. The server finds the tag the
    /// name names, or adds one, and pairs it with the post, so the caller never names a tag by
    /// its id.
    /// </summary>
    public class TagSuggestion
    {
        /// <summary>The id of the post the tag is suggested for.</summary>
        public Guid ContentItemId { get; set; }

        /// <summary>
        /// The tag's name as the reader typed it. The server trims white space around it and
        /// changes nothing else (TagSuggestions.md rule 2).
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}
