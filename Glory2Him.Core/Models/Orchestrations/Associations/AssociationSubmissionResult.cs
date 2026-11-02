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
    /// What a tag suggestion answers: its status, and nothing else (TagSuggestions.md rules 5 and
    /// 7). Unlike <see cref="AssociationSuggestionResult"/> it carries no id, of the tag or of the
    /// pairing: the tag may be one the caller cannot see, and an id the caller holds can be read
    /// back, where a read answers not found for a hidden tag.
    /// </summary>
    public class AssociationSubmissionResult
    {
        public AssociationSubmissionStatus Status { get; set; }
    }
}
