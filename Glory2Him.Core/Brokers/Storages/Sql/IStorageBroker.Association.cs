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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial interface IStorageBroker
    {
        ValueTask<Association> InsertAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<Association>> SelectAllAssociationsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The row on one canonical pair — both endpoint types, both EFFECTIVE ids and the owning
        /// user — or null when the pair is free. A LIVE row wins, and otherwise the most recently
        /// touched soft-deleted one, which is the candidate the resurrect rule considers.
        ///
        /// <para>DELIBERATELY UNFILTERED (§7.4/§14.6): the retrieve-or-add flow must see a pending
        /// or rejected row belonging to another user, and a soft-deleted one, both of which the
        /// read posture hides from the submitting caller. Endpoints arrive already normalised and
        /// already resolved to effective ids — canonical ordering stays in the service so the write
        /// path and the probe cannot diverge.</para>
        /// </summary>
        ValueTask<Association?> SelectAssociationByPairAsync(
            EntityType entityAType,
            EntityType entityBType,
            Guid entityAEffectiveId,
            Guid entityBEffectiveId,
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The most recently touched LIVE row whose version coverage intersects the given pair on
        /// BOTH endpoints — an endpoint intersects when either side spans <c>AllVersions</c> or
        /// both pin the same version. Two <c>ThisVersionOnly</c> endpoints on DIFFERENT versions of
        /// one group do not overlap, so this must not report them.
        ///
        /// <para>Only live rows can double-render, so soft-deleted rows are excluded here — the one
        /// place this read's posture differs from <see cref="SelectAssociationByPairAsync"/>.</para>
        /// </summary>
        ValueTask<Association?> SelectOverlappingAssociationAsync(
            EntityType entityAType,
            EntityType entityBType,
            string userId,
            Guid entityAGroupId,
            Guid entityBGroupId,
            Scope entityAScope,
            Scope entityBScope,
            Guid entityAEffectiveId,
            Guid entityBEffectiveId,
            Guid? excludedAssociationId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether a LIVE row other than <paramref name="excludedAssociationId"/> already holds
        /// the given canonical pair — the duplicate check a scope change has to pass, because
        /// <c>UX_Associations_Pair</c> keys on the EFFECTIVE id and a scope toggle recomputes it.
        /// </summary>
        ValueTask<bool> ExistsLiveAssociationOnPairAsync(
            EntityType entityAType,
            EntityType entityBType,
            string userId,
            Guid entityAEffectiveId,
            Guid entityBEffectiveId,
            Guid excludedAssociationId,
            CancellationToken cancellationToken = default);

        ValueTask<Association> SelectAssociationByIdAsync(
            Guid associationId,
            CancellationToken cancellationToken = default);

        ValueTask<Association> UpdateAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default);

        ValueTask<Association> DeleteAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertAssociationsAsync(
            List<Association> associations,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateAssociationsAsync(
            List<Association> associations,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteAssociationsAsync(
            List<Association> associations,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<Association>> BulkReadAssociationsAsync(
            List<Association> associations,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertAssociationsAsync(
            List<Association> associations,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsAssociationAsync(
            Guid associationId,
            CancellationToken cancellationToken = default);
    }
}
