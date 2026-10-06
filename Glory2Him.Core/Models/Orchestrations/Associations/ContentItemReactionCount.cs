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
    /// How many readers gave one reaction to one content item, under the reaction's own names
    /// (§ARC16.8, <i>The projection</i>). The wire carries the entity's names, <c>Name</c> and
    /// <c>UnicodeEmoji</c> (§DOM5.2), never the view's.
    /// </summary>
    public class ContentItemReactionCount
    {
        /// <summary>The reaction's id, which for a Single-Row far end is the association's
        /// <c>EntityBKeyId</c>.</summary>
        public Guid ReactionId { get; set; }

        /// <summary>The reaction's <c>Name</c>.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>The reaction's <c>UnicodeEmoji</c>.</summary>
        public string UnicodeEmoji { get; set; } = string.Empty;

        /// <summary>
        /// How many counted rows give this reaction to the item's version group. Never zero: a
        /// reaction nobody gave has no entry at all.
        /// </summary>
        public int Count { get; set; }
    }
}
