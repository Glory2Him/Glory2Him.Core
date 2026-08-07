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

        ValueTask<Association> SubmitAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default);

        ValueTask<Association> ApproveAssociationAsync(
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

        ValueTask<Association> SetAssociationScopeAsync(
            Association association,
            CancellationToken cancellationToken = default);
    }
}
