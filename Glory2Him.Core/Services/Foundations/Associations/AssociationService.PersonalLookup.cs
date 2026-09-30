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
        public async ValueTask<PersonalAssociationMatch?> FindPersonalAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default)
        {
            EventEnvelope<Association> envelope =
                await this.eventEnvelopeBroker.CreateAsync(content: association);

            string callerUserId =
                await this.securityAuditBroker.GetUserIdAsync(envelope.SecurityContext);

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
                                .Select(match => new PersonalAssociationMatch
                                {
                                    Id = match.Id,
                                    EntityBKeyId = match.EntityBKeyId,
                                    IsDeleted = match.IsDeleted
                                }),
                    cancellationToken: cancellationToken);

            return matches.FirstOrDefault();
        }

        // The personal-key condition, written once (§DOM4.6 rule 2; the user story's preamble):
        // the key UX_Associations_PersonalPair holds — the host's type and effective id, the far
        // end's type and the reader — over the unfiltered store, withdrawn rows included, because
        // a revive needs the withdrawn row. The far end's key is not a term: a reader has one row
        // per host whichever reaction it points at. The personal upsert resolves the reader's row
        // with this same condition.
        private static IQueryable<Association> SelectPersonalAssociations(
            IQueryable<Association> associations,
            EntityType entityAType,
            Guid entityAEffectiveId,
            EntityType entityBType,
            string userId) =>
            associations.Where(association =>
                association.EntityAType == entityAType
                    && association.EntityAEffectiveId == entityAEffectiveId
                    && association.EntityBType == entityBType
                    && association.UserId == userId);
    }
}
