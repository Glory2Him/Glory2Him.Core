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

using Glory2Him.Core.Models.Enums;

namespace Glory2Him.Core.Models.Orchestrations.Associations
{
    /// <summary>
    /// The outcome of an upsert (§ARC16.8.1): an editorial pair's retrieve-or-add suggestion
    /// (design §7.4), or a reader's reaction given, changed or brought back (§DOM4.10). It is
    /// deliberately the whole of what the flow tells the caller — a status and the row id, never
    /// the row body — because the row may belong to another user and the read posture reports
    /// non-public rows to non-owners as not-found. <see cref="AlreadyPending"/> covers a pending,
    /// a rejected and a taken-down row on purpose, so a caller cannot infer a rejection or a
    /// takedown by trying again. The numbers cross the wire, so a member is appended and none
    /// moves.
    /// </summary>
    public enum AssociationSuggestionStatus
    {
        /// <summary>
        /// A new row was inserted: the editorial pair was free, or the reader had no reaction on
        /// the item.
        /// </summary>
        Created,

        /// <summary>
        /// A row already occupies the pair but is not a live, approved one: pending, rejected or
        /// soft-deleted, a reaction taken down by somebody else included. The caller cannot tell
        /// which, by design. Nothing was written.
        /// </summary>
        AlreadyPending,

        /// <summary>
        /// An approved row already occupies the pair, and is already visible: the editorial pair,
        /// or the reaction the reader already holds. Nothing was written.
        /// </summary>
        AlreadyApproved,

        /// <summary>
        /// No row occupies the exact pair, but a differently-scoped row already covers it — an
        /// endpoint spanning <see cref="Scope.AllVersions"/> overlaps a
        /// <see cref="Scope.ThisVersionOnly"/> row's version (or the reverse), so both would
        /// render the same pairing. The effective ids differ, so the unique index cannot catch
        /// this; the overlap probe does. Nothing was inserted.
        /// </summary>
        OverlapsExisting,

        /// <summary>
        /// The reader's own withdrawn row was revived to the reaction they gave again. It comes
        /// back at the status it was withdrawn at, because a revive writes no status of its own
        /// (§DOM4.10 rule 8). A row somebody else took down is never revived (§DOM4.10 rule 7), so
        /// this follows only the reader's own withdrawal.
        /// </summary>
        Restored,

        /// <summary>
        /// The reader's row was repointed to the reaction they gave, from another one they held
        /// or from one they had withdrawn, in one write (§DOM4.10). Appended rather than placed
        /// beside <see cref="Restored"/>, because the number crosses the wire and no member moves.
        /// </summary>
        Repointed
    }
}
