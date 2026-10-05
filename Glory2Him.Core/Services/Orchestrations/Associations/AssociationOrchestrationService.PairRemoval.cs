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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations;

namespace Glory2Him.Core.Services.Orchestrations.Associations
{
    internal partial class AssociationOrchestrationService
    {
        // THE PAIR-KEYED WITHDRAWAL (§ARC16.8.1; AssociationOrchestrationService.md §3). A reader
        // names the item and the reaction, never a row id, so it can only ever reach the row the
        // personal key gives it — its own. It takes the upsert's caller shape and runs that flow
        // up to the pair: the same structural validation, the same endpoint resolution, and the
        // caller's UserId from the envelope. It runs no facet gate, because withdrawing is never
        // gated (§ARC16.2.1).
        public ValueTask<AssociationRemovalResult> RemoveAssociationByPairAsync(
            Association association,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateAssociationIsNotNull(association);

                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: association);

                ValidateUserMayRemoveAssociationByPair(envelope.SecurityContext);

                ValidateOnRemoveAssociationByPair(association);
                ValidatePairIsPersonal(association);

                // as on the upsert, the endpoint reads are the ambient caller's, which on an HTTP
                // request IS the caller — so no read envelope is carried
                await ResolvePairEndpointsAsync(
                    association: association,
                    readEnvelope: null,
                    cancellationToken: cancellationToken);

                // UserId is derived, never the caller's to set (§DOM4.10 rules 1 and 2): whatever
                // the request carried is overwritten with the caller's own, so the lookup below
                // can only ever find the caller's row
                association.UserId = envelope.SecurityContext.SubjectId;

                PersonalAssociationMatch? readersRow =
                    await this.associationService.FindPersonalAssociationAsync(
                        association,
                        cancellationToken);

                // Nothing to withdraw: no row, a row already withdrawn — by the reader, or taken
                // down by a moderator — or a row holding a reaction other than the one named, which
                // stays as it is. The same answer for all three, and never a not-found: the end
                // state the caller asked for holds either way (§ARC16.8.1).
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

                // The foundation's soft delete publishes Association-Removed; this service mints
                // no address (§ARC16.8). It exempts the reader's own row from the read-only veto
                // (§SEC14.7 posture A′ rule 4), and a withdrawal records no deletion reason.
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
            IsPersonalEndpoint(association.EntityAType)
                ? association.EntityAKeyId
                : association.EntityBKeyId;
    }
}
