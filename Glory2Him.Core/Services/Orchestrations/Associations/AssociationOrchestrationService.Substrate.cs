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

using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Associations;

namespace Glory2Him.Core.Services.Orchestrations.Associations
{
    internal partial class AssociationOrchestrationService
    {
        public ValueTask<EventEnvelope<Association>?> OnAddingAssociationAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                // Ahead of the verify: a caller who has already cancelled should not pay for an
                // HMAC computation, let alone two endpoint reads.
                cancellationToken.ThrowIfCancellationRequested();

                await ValidateAssociationEventEnvelopeAsync(
                    envelope: envelope,
                    operation: AssociationEventOperation.Adding);

                // AHEAD OF THE DERIVATION, because the derivation is work and a replay should do
                // none of it. The foundation asks this for itself too, but only after this handler
                // has read both endpoint rows — and a re-delivered envelope whose endpoint has since
                // been soft-deleted, or has stopped being visible to the signed caller, would fail
                // that read and be recorded as a failed delivery for an event already applied.
                bool alreadyAdded =
                    await this.associationService.HasAlreadyAddedAssociationAsync(
                        envelope: envelope,
                        cancellationToken: cancellationToken);

                if (alreadyAdded)
                {
                    return null;
                }

                var derivedAssociation = new Association
                {
                    EntityAType = envelope.Content.EntityAType,
                    EntityAKeyId = envelope.Content.EntityAKeyId,
                    EntityBType = envelope.Content.EntityBType,
                    EntityBKeyId = envelope.Content.EntityBKeyId,
                };

                await ResolveEndpointAsync(
                    derivedAssociation.EntityAType,
                    derivedAssociation.EntityAKeyId,
                    onResolved: _ => { },
                    endpointName: "A",
                    readEnvelope: envelope,
                    cancellationToken: cancellationToken);

                await ResolveEndpointAsync(
                    derivedAssociation.EntityBType,
                    derivedAssociation.EntityBKeyId,
                    onResolved: _ => { },
                    endpointName: "B",
                    readEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.associationService.OnAddingAssociationAsync(
                    envelope: envelope,
                    cancellationToken: cancellationToken);
            });
    }
}
