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
    /// The outcome of a retrieve-or-add suggestion (design §7.4). It is deliberately the whole
    /// of what the flow tells the caller — a status and the row id, never the row body — because
    /// the row may belong to another user and the read posture reports non-public rows to
    /// non-owners as not-found. <see cref="AlreadyPending"/> covers both a pending and a
    /// rejected row on purpose, so a contributor cannot infer a rejection by resubmitting.
    /// </summary>
    public enum AssociationSuggestionStatus
    {
        /// <summary>The pair was unoccupied; a new row was inserted.</summary>
        Created,

        /// <summary>
        /// A row already occupies the pair and is not yet approved (pending OR rejected — the
        /// two are indistinguishable to the caller by design). Nothing was inserted.
        /// </summary>
        AlreadyPending,

        /// <summary>An approved row already occupies the pair and is already visible. Nothing was inserted.</summary>
        AlreadyApproved,

        /// <summary>
        /// The caller's own soft-deleted row was resurrected — deletion cleared and approval
        /// status reset to Draft (never inherited). A row soft-deleted by a moderator is never
        /// resurrected, so this can only follow the contributor's own earlier removal.
        /// </summary>
        Restored
    }
}
