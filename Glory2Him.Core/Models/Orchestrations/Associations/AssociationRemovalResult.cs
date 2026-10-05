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
    /// What the pair-keyed withdrawal returns: the outcome and the withdrawn row's id, and nothing
    /// else (§ARC16.8.1), exactly as <see cref="AssociationSuggestionResult"/> carries no more —
    /// the row body would leak authorship.
    /// </summary>
    public class AssociationRemovalResult
    {
        public AssociationRemovalStatus Status { get; set; }

        /// <summary>
        /// The id of the row that was withdrawn, or <c>null</c> where nothing was.
        /// </summary>
        public Guid? AssociationId { get; set; }
    }
}
