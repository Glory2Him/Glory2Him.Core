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
    /// The outcome of a tag suggestion (TagSuggestions.md rule 7). Whose a pairing is, is told
    /// only for a tag the caller can see: every suggestion of a tag the caller cannot see answers
    /// <see cref="Created"/>, whatever already holds the pairing, so nothing tells such a tag from
    /// one the caller created (TagSuggestions.md rule 5). The numbers cross the wire, so a member
    /// is appended and none moves.
    /// </summary>
    public enum AssociationSubmissionStatus
    {
        /// <summary>
        /// The pairing was inserted; or the tag is one the caller cannot see, whatever holds the
        /// pairing; or the tag is withheld, the caller can see it, and nothing holds or covers
        /// the pairing, though nothing was inserted.
        /// </summary>
        Created,

        /// <summary>
        /// The caller can see the tag, and a pairing another reader created already holds or
        /// covers the pair, at any status, live or soft-deleted. Nothing was written.
        /// </summary>
        AlreadySuggested,

        /// <summary>
        /// The caller can see the tag, and the caller's own live pairing, at <c>Draft</c> or
        /// <c>Submitted</c>, already holds or covers the pair. Nothing was written.
        /// </summary>
        AlreadySuggestedByCaller,

        /// <summary>
        /// The caller can see the tag, and the caller's own pairing that is no longer pending — a
        /// live one at <c>Approved</c> or <c>Rejected</c>, or a soft-deleted one at any status —
        /// already holds or covers the pair. It never says which. Nothing was written.
        /// </summary>
        PreviouslySuggestedByCaller
    }
}
