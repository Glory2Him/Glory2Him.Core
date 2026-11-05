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

namespace Glory2Him.WebApp.Tests.Acceptance.Models.Associations
{
    /// <summary>
    /// A reader's suggestion of a tag for a post, as the client sends it: the post's id and the
    /// tag's name, and nothing else (TagSuggestions.md rule 1).
    /// </summary>
    public class TagSuggestion
    {
        public Guid ContentItemId { get; set; }
        public string Name { get; set; }
    }
}
