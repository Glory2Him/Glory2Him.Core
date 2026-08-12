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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;

namespace Glory2Him.Core.Services.Foundations.Associations
{
    internal partial class AssociationService
    {
        public ValueTask<AssociationPairMatch?> FindAssociationByPairAsync(
            Association association,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateAssociationIsNotNull(association);

                // the envelope exists to capture the ambient security context the contribution
                // gate runs against — the probe is a write-flow primitive, not a public read
                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: association);

                ValidateUserIsNotGloballyBlockedFromContributing(envelope.SecurityContext);
                ValidateOnFindAssociationByPair(association);

                // Deliberately UNFILTERED (§7.4/§14.6): the retrieve-or-add flow must see a
                // pending or rejected row belonging to another user, and a soft-deleted one,
                // both of which the read posture hides from the submitting caller. The
                // projection returned below reveals no row body, so nothing the caller could not
                // already infer from resubmitting leaks.
                IQueryable<Association> allAssociations =
                    await this.storageBroker.SelectAllAssociationsAsync(cancellationToken);

                // The same effective ids the persisted computed column carries and the
                // UX_Associations_Pair index keys on. Computed here from the resolved endpoints
                // so the probe matches exactly the row an insert would collide with.
                Guid entityAEffectiveId = ResolveEffectiveId(
                    association.EntityAScope,
                    association.EntityAGroupId,
                    association.EntityAKeyId);

                Guid entityBEffectiveId = ResolveEffectiveId(
                    association.EntityBScope,
                    association.EntityBGroupId,
                    association.EntityBKeyId);

                // Prefer a LIVE row when one exists (there can be at most one — the unique index
                // filters WHERE IsDeleted = 0), and otherwise the most recently touched
                // soft-deleted row, which is the candidate the resurrect rule considers.
                Association? match = allAssociations
                    .Where(other =>
                        other.EntityAType == association.EntityAType
                            && other.EntityBType == association.EntityBType
                            && other.EntityAEffectiveId == entityAEffectiveId
                            && other.EntityBEffectiveId == entityBEffectiveId
                            && other.UserId == association.UserId)
                    .OrderBy(other => other.IsDeleted)
                    .ThenByDescending(other => other.UpdatedWhen)
                    .FirstOrDefault();

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

        // The probe is called with a resolved association, so its endpoints must be identified:
        // a valid entity type each side and non-empty key and group ids, since the effective id
        // the lookup keys on is computed from them. An unresolved endpoint here would key the
        // lookup off Guid.Empty and match nothing meaningful.
        private static void ValidateOnFindAssociationByPair(Association association) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsInvalid(association.EntityAType), Parameter: nameof(Association.EntityAType)),
                (Rule: IsInvalid(association.EntityBType), Parameter: nameof(Association.EntityBType)),
                (Rule: IsInvalid(association.EntityAKeyId), Parameter: nameof(Association.EntityAKeyId)),
                (Rule: IsInvalid(association.EntityBKeyId), Parameter: nameof(Association.EntityBKeyId)),
                (Rule: IsInvalid(association.EntityAGroupId), Parameter: nameof(Association.EntityAGroupId)),
                (Rule: IsInvalid(association.EntityBGroupId), Parameter: nameof(Association.EntityBGroupId)));
    }
}
