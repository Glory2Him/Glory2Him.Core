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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;

namespace Glory2Him.Core.Services.Foundations.Associations
{
    internal partial class AssociationService
    {
        public ValueTask<PersonalAssociationMatch?> FindPersonalAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateAssociationIsNotNull(association);

                // the envelope captures the caller the lookup answers for: a reader's row is
                // found for that reader and nobody else
                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: association);

                // an anonymous caller owns no row, and is answered as a reader asking for another
                // reader's row is: a denied read answers not found
                if (envelope.SecurityContext is null
                    || envelope.SecurityContext.IsAuthenticated is false)
                {
                    await this.loggingBroker.LogWarningAsync(
                        message: "Personal content item association lookup denied. The caller " +
                            "is not authenticated; reported to the caller as not found.");

                    return null;
                }

                ValidateOnFindPersonalAssociation(association);

                string callerUserId =
                    await this.securityAuditBroker.GetUserIdAsync(envelope.SecurityContext);

                // a denied read answers not found, as a reader with no row is answered, and the
                // true reason stays server-side (ts-foundations-012). No role lifts this: the
                // lookup serves the owner alone (§SEC14.7 posture A′ rule 7).
                if (association.UserId != callerUserId)
                {
                    await this.loggingBroker.LogWarningAsync(
                        message: "Personal content item association lookup denied. User " +
                            $"\"{callerUserId}\" asked for another user's row; reported to the " +
                            "caller as not found.");

                    return null;
                }

                // every stored row is canonical, so the key is taken from the canonical
                // orientation: a request naming the reaction first would otherwise key the lookup
                // off the reaction (§DOM4.4 rule 4)
                association = NormalizeEndpointOrder(association);

                Guid entityAEffectiveId = ResolveEffectiveId(
                    association.EntityAScope,
                    association.EntityAGroupId,
                    association.EntityAKeyId);

                IReadOnlyList<PersonalAssociationMatch> matches =
                    await this.storageBroker.SelectAssociationsAsync(
                        query: associations =>
                            SelectPersonalAssociations(
                                associations,
                                entityAType: association.EntityAType,
                                entityAEffectiveId: entityAEffectiveId,
                                entityBType: association.EntityBType,
                                userId: callerUserId)
                                    .Take(1)
                                    .Select(match => new PersonalAssociationMatch
                                    {
                                        Id = match.Id,
                                        EntityBKeyId = match.EntityBKeyId,
                                        IsDeleted = match.IsDeleted
                                    }),
                        cancellationToken: cancellationToken);

                return matches.FirstOrDefault();
            });

        // A null UserId is an editorial row, which has no personal key to look up. The endpoints
        // must be identified — a supported type and scope, a key and a group each side — because
        // the key the lookup runs on is taken from them: under AllVersions the effective id is the
        // group id, so an empty group would key the lookup off nothing, and a scope that is
        // neither would be keyed as ThisVersionOnly — each answering "no row" rather than refusing.
        private static void ValidateOnFindPersonalAssociation(Association association) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsInvalid(association.UserId), Parameter: nameof(Association.UserId)),
                (Rule: IsInvalid(association.EntityAType), Parameter: nameof(Association.EntityAType)),
                (Rule: IsInvalid(association.EntityBType), Parameter: nameof(Association.EntityBType)),
                (Rule: IsInvalid(association.EntityAKeyId), Parameter: nameof(Association.EntityAKeyId)),
                (Rule: IsInvalid(association.EntityBKeyId), Parameter: nameof(Association.EntityBKeyId)),
                (Rule: IsInvalid(association.EntityAGroupId), Parameter: nameof(Association.EntityAGroupId)),
                (Rule: IsInvalid(association.EntityBGroupId), Parameter: nameof(Association.EntityBGroupId)),
                (Rule: IsInvalid(association.EntityAScope), Parameter: nameof(Association.EntityAScope)),
                (Rule: IsInvalid(association.EntityBScope), Parameter: nameof(Association.EntityBScope)));

        // The personal-key condition, written once (§DOM4.6 rule 2; the user story's preamble):
        // the key UX_Associations_PersonalPair holds — the host's type and effective id, the far
        // end's type and the reader — over the unfiltered store, withdrawn rows included, because
        // a revive needs the withdrawn row. The far end's key is not a term: a reader has one row
        // per host whichever reaction it points at. The personal upsert (#719) is to resolve the
        // reader's row with this condition rather than write a second one.
        //
        // Ordered, because rows written before this feature can give one reader more than one row
        // on a host (§DOM4.10 rule 6): the live row first, then the most recently updated — the
        // order the pair probe takes.
        private static IQueryable<Association> SelectPersonalAssociations(
            IQueryable<Association> associations,
            EntityType entityAType,
            Guid entityAEffectiveId,
            EntityType entityBType,
            string userId) =>
            associations
                .Where(association =>
                    association.EntityAType == entityAType
                        && association.EntityAEffectiveId == entityAEffectiveId
                        && association.EntityBType == entityBType
                        && association.UserId == userId)
                .OrderBy(association => association.IsDeleted)
                .ThenByDescending(association => association.UpdatedWhen);
    }
}
