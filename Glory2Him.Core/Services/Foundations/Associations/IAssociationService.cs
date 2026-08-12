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

        ValueTask<Association> ApproveAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default);

        // Approving OVER unmet conditions, which is its own verb rather than a flag on the
        // one above (§12.4.4 rule 11). A flag would make every ordinary approve a potential
        // bypass, and the reason — the only thing that makes a bypass tolerable — would be an
        // optional argument on the common path instead of a required one here.
        //
        // The reason is a parameter and not a field on the entity because it is an argument to
        // the DECISION, not a value the caller may write: what lands on the row is derived
        // from the verdict, and is cleared when the verdict waived nothing.
        ValueTask<Association> BypassApproveAssociationAsync(
            Association association,
            string bypassReason,
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
