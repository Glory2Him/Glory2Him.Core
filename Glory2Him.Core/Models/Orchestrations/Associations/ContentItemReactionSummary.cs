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
using System.Collections.Generic;

namespace Glory2Him.Core.Models.Orchestrations.Associations
{
    /// <summary>
    /// One content item's reactions as a card shows them: which reactions it has been given, how
    /// many of each, and which one the calling reader holds (§ARC16.8, <i>The projection</i>).
    ///
    /// <para>It carries no total. The sum of the counts is the total by construction, so the
    /// client sums, and a carried total would be a second source of truth for it.</para>
    /// </summary>
    public class ContentItemReactionSummary
    {
        /// <summary>
        /// The id the caller supplied, echoed back verbatim so the client keys its cards on what
        /// it already holds — a version's id, never its group's.
        /// </summary>
        public Guid ContentItemId { get; set; }

        /// <summary>
        /// The item's counts, in the vocabulary's order. Empty where nobody has reacted, and where
        /// the item's winning setting does not show reactions.
        /// </summary>
        public IReadOnlyList<ContentItemReactionCount> Reactions { get; set; } =
            Array.Empty<ContentItemReactionCount>();

        /// <summary>
        /// The reaction the calling reader holds on the item, whether or not it is counted yet;
        /// <c>null</c> where they hold none, where it is not of the public vocabulary, and for an
        /// anonymous caller.
        /// </summary>
        public Guid? ViewerReactionId { get; set; }

        /// <summary>The <c>Name</c> of the reaction <see cref="ViewerReactionId"/> names;
        /// <c>null</c> on the same terms.</summary>
        public string? ViewerReactionName { get; set; }
    }
}
