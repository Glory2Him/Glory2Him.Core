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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;

namespace Glory2Him.Core.Services.Foundations.Associations
{
    internal partial class AssociationService
    {
        public ValueTask<AssociationPairMatch?> FindOverlappingAssociationAsync(
            Association association,
            Guid? excludedAssociationId = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateAssociationIsNotNull(association);

                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: association);

                ValidateUserIsNotGloballyBlockedFromContributing(envelope.SecurityContext);
                ValidateOnFindAssociationByPair(association);

                // Match the SAME canonical endpoint order stored rows carry (an insert normalizes
                // before persisting), so a reversed-order request is not blind to the row it would
                // overlap — the exact concern the pair probe had.
                association = NormalizeEndpointOrder(association);

                Guid entityAEffectiveId = ResolveEffectiveId(
                    association.EntityAScope,
                    association.EntityAGroupId,
                    association.EntityAKeyId);

                Guid entityBEffectiveId = ResolveEffectiveId(
                    association.EntityBScope,
                    association.EntityBGroupId,
                    association.EntityBKeyId);

                // Overlap, not exact match: two rows on the same canonical pair (same endpoint
                // types, groups and UserId) whose version coverage intersects on BOTH endpoints.
                // An endpoint's coverage intersects when either side spans AllVersions (which
                // covers the whole group, so it contains the other's version) OR both pin the
                // SAME version (equal effective ids). Two ThisVersionOnly endpoints on DIFFERENT
                // versions of the same group do NOT overlap — other versions do not inherit — so
                // this must not flag them. Only LIVE rows can double-render, so soft-deleted rows
                // are excluded.
                //
                // Same UNFILTERED posture the exact-pair probe carries: an overlapping row
                // belonging to another user, or a pending one, is hidden from the submitting
                // caller yet still renders, so a visibility-filtered read would miss it and let
                // the double-render through. And asked for the same way — as one row, with the
                // token, rather than as a predicate composed onto a live queryable and executed
                // synchronously on the request thread.
                Association? match = await this.storageBroker.SelectOverlappingAssociationAsync(
                    entityAType: association.EntityAType,
                    entityBType: association.EntityBType,
                    userId: association.UserId,
                    entityAGroupId: association.EntityAGroupId,
                    entityBGroupId: association.EntityBGroupId,
                    entityAScope: association.EntityAScope,
                    entityBScope: association.EntityBScope,
                    entityAEffectiveId: entityAEffectiveId,
                    entityBEffectiveId: entityBEffectiveId,
                    excludedAssociationId: excludedAssociationId,
                    cancellationToken: cancellationToken);

                if (match is null)
                {
                    return null;
                }

                return new AssociationPairMatch
                {
                    Id = match.Id,
                    ApprovalStatus = match.ApprovalStatus,
                    IsDeleted = match.IsDeleted,
                    CreatedBy = match.CreatedBy,
                    DeletedBy = match.DeletedBy,
                };
            });
    }
}
