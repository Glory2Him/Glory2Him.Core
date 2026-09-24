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
    /// <summary>
    /// The event path of the add. <c>Association-Adding</c> binds HERE rather than to the
    /// foundation (#631), because its handler derives <c>Entity{A,B}ContentType</c> — an
    /// authorization input (§DOM4.5 rule 3) — from the two endpoint rows the request names, and
    /// the foundation, confined to its own entity, validates that value for enum-definedness
    /// only. While the address bound the foundation, a publisher could name a content type it
    /// held no role for and have it accepted on that alone.
    ///
    /// <para><b>The foundation keeps everything else.</b> This handler verifies, asks the
    /// duplicate question early, and runs the SAME write flow the method path runs — then hands
    /// the SAME envelope down. Deduplication on <c>Metadata.EventId</c>, the audit stamping,
    /// canonical ordering, the write, the past-tense fact and the reply are all still the
    /// foundation's, reached with the envelope untouched. The precedent is
    /// <c>ContentItemSetting-Adding</c> (#456), followed unchanged.</para>
    /// </summary>
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

                // THE SAME WRITE FLOW THE METHOD PATH RUNS, on a working copy of the raw endpoints
                // rather than on the envelope's content: that content is covered by the HMAC, and
                // the foundation, the ProcessedEvents record and the reply are all built from it,
                // so no part the rules read may be edited here (§SEC14.6 rule 4).
                Association derivedAssociation = CreateAddRequestFrom(envelope.Content);

                await DeriveAssociationToAddAsync(
                    association: derivedAssociation,
                    inboundEnvelope: envelope,
                    readEnvelope: envelope,
                    cancellationToken: cancellationToken);

                ValidateClaimsAreTheDerivation(
                    claimedAssociation: envelope.Content,
                    derivedAssociation: derivedAssociation);

                await this.associationService.FindAssociationByPairAsync(
                    association: derivedAssociation,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                await this.associationService.FindOverlappingAssociationAsync(
                    association: derivedAssociation,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.associationService.OnAddingAssociationAsync(
                    envelope: envelope,
                    cancellationToken: cancellationToken);
            });

        // Only the RAW endpoints are the caller's to supply (see ValidateOnAddAssociation), so
        // they are all the working copy carries. Everything else the flow needs it derives.
        private static Association CreateAddRequestFrom(Association claimedAssociation) =>
            new Association
            {
                EntityAType = claimedAssociation.EntityAType,
                EntityAKeyId = claimedAssociation.EntityAKeyId,
                EntityBType = claimedAssociation.EntityBType,
                EntityBKeyId = claimedAssociation.EntityBKeyId,
            };
    }
}
