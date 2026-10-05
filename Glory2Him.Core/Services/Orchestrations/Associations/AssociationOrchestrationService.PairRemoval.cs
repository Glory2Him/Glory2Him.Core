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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations;

namespace Glory2Him.Core.Services.Orchestrations.Associations
{
    internal partial class AssociationOrchestrationService
    {
        public ValueTask<AssociationRemovalResult> RemoveAssociationByPairAsync(
            Association association,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                ValidateAssociationIsNotNull(association);

                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: association);

                ValidateUserMayRemoveAssociationByPair(envelope.SecurityContext);

                await ResolvePairEndpointsAsync(
                    association: association,
                    readEnvelope: null,
                    cancellationToken: cancellationToken);

                association.UserId = envelope.SecurityContext.SubjectId;

                PersonalAssociationMatch? readersRow =
                    await this.associationService.FindPersonalAssociationAsync(
                        association,
                        cancellationToken);

                if (readersRow is null
                    || readersRow.IsDeleted
                    || readersRow.EntityBKeyId != GetNamedReactionId(association))
                {
                    return new AssociationRemovalResult
                    {
                        Status = AssociationRemovalStatus.NothingToRemove,
                        AssociationId = null,
                    };
                }

                Association withdrawnAssociation =
                    await this.associationService.RemoveAssociationByIdAsync(
                        readersRow.Id,
                        deletionReason: null,
                        cancellationToken);

                return new AssociationRemovalResult
                {
                    Status = AssociationRemovalStatus.Removed,
                    AssociationId = withdrawnAssociation.Id,
                };
            });

        // THE REACTION THE CALLER NAMED, on whichever endpoint the request carries it: the
        // upsert's caller shape names a reaction on either (§ARC16.8.1). Which endpoint holds it
        // is the personalisation lookup's answer, never a test of this service's own (§DOM4.10
        // rule 4).
        private static Guid GetNamedReactionId(Association association) =>
            EntityTypePersonalisation.IsPersonal(association.EntityAType)
                ? association.EntityAKeyId
                : association.EntityBKeyId;
    }
}
