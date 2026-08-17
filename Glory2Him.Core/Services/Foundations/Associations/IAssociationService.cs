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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;

namespace Glory2Him.Core.Services.Foundations.Associations
{
    internal partial interface IAssociationService
    {
        ValueTask<Association> AddAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<Association>> RetrieveAllAssociationsAsync(
            CancellationToken cancellationToken = default);

        ValueTask<Association> RetrieveAssociationByIdAsync(
            Guid associationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Looks up the row that occupies the same canonical pair as <paramref name="association"/>
        /// — the same endpoint effective ids and <c>UserId</c> the <c>UX_Associations_Pair</c>
        /// index keys on — over the UNFILTERED store, spanning soft-deleted rows (design §7.4,
        /// §14.6). The retrieve-or-add flow must see rows the submitting user's read posture
        /// hides (another user's pending/rejected row, or a soft-deleted one), so a
        /// visibility-filtered lookup would miss them and let the duplicate through. Returns a
        /// non-leaking <see cref="AssociationPairMatch"/> projection — id, approval state and
        /// soft-delete provenance only — or <c>null</c> when the pair is unoccupied; the row
        /// body never crosses back. The caller supplies an association whose endpoints are
        /// already resolved (scope, group and key set), because the effective id is computed
        /// from them.
        /// </summary>
        ValueTask<AssociationPairMatch?> FindAssociationByPairAsync(
            Association association,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Looks up a LIVE row that OVERLAPS <paramref name="association"/>'s coverage rather than
        /// occupying its exact pair: same endpoint types, groups and <c>UserId</c>, but a version
        /// range that intersects on BOTH endpoints — an <see cref="Scope.AllVersions"/> endpoint
        /// spanning a <see cref="Scope.ThisVersionOnly"/> row's version, or the reverse. Their
        /// effective ids differ, so <c>UX_Associations_Pair</c> cannot catch it, yet both rows
        /// would render the same pairing (design §7.4). Two <see cref="Scope.ThisVersionOnly"/>
        /// endpoints on different versions of one group do NOT overlap and are never returned.
        /// Reads the UNFILTERED store and returns a non-leaking <see cref="AssociationPairMatch"/>
        /// (or <c>null</c> when nothing overlaps); pass <paramref name="excludedAssociationId"/> to
        /// exclude the row being modified from its own check.
        /// </summary>
        ValueTask<AssociationPairMatch?> FindOverlappingAssociationAsync(
            Association association,
            Guid? excludedAssociationId = null,
            CancellationToken cancellationToken = default);

        ValueTask<Association> ModifyAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default);

        ValueTask<Association> RemoveAssociationByIdAsync(
            Guid associationId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        ValueTask<Association> HardRemoveAssociationByIdAsync(
            Guid associationId,
            CancellationToken cancellationToken = default);

        // ── State transitions (design §9.7.1, §9.2) ───────────────────────────────────
        //
        // The general modify is content-only. Every field group that is NOT content gets its
        // own narrow operation that owns exactly its own fields and publishes its own fact.
        //
        // That split is what breaks the approval workflow's write-back cycle: the workflow
        // subscribes to Modified and causes Approved, so if approving published Modified it
        // would re-enter the handler that caused it. ProcessedEvents cannot save this — it is
        // keyed on the event id and a write-back mints a fresh one — and under inline dispatch
        // the repetition is synchronous re-entry inside the originating request.
        //
        // Each takes the whole entity for house-style consistency with Modify, but reads only
        // the fields in its own scope from it; everything else comes from storage.

        // One verb for every approval-state move, because they are one act under different
        // authority rather than three operations: the ordinary Submitted -> Approved/Rejected
        // verdict, the Admin override that re-opens a terminal row (§8.6 HR-4), and the bypass
        // that approves OVER unmet conditions (§12.4.4 rule 11).
        //
        // The bypass was previously its own verb, on the reasoning that a flag would make every
        // ordinary approve a potential bypass and would demote the reason — the only thing that
        // makes a bypass tolerable — to an optional argument on the common path. That is
        // reversed (§8.6.1); what replaced it keeps both mitigations. The reason is validated
        // non-empty and bounded BEFORE any policy is read, so an unexplained bypass is refused
        // under every policy, and the pair that lands on the row is still derived from the
        // verdict rather than accepted — a waiver that turned out to be unnecessary records no
        // bypass at all.
        ValueTask<Association> TransitionAssociationApprovalAsync(
            Association association,
            CancellationToken cancellationToken = default);

        ValueTask<Association> SortAssociationAsync(
            Association association,
            Association anchorAssociation,
            SortPosition position,
            CancellationToken cancellationToken = default);

        ValueTask<Association> SetAssociationConfidenceAsync(
            Association association,
            CancellationToken cancellationToken = default);

        // Nullable on purpose: null means "leave this endpoint alone". Scope.AllVersions is
        // 0, so a non-nullable parameter cannot tell an omitted value from a deliberate one —
        // and the value it would default to is the WIDENING one, on the operation whose
        // Publisher/Admin gate exists precisely because widening reach is consequential.
        ValueTask<Association> SetAssociationScopeAsync(
            Guid associationId,
            Scope? entityAScope,
            Scope? entityBScope,
            CancellationToken cancellationToken = default);
    }
}
