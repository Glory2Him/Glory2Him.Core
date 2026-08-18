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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;

namespace Glory2Him.Core.Services.Foundations.Associations
{
    /// <summary>
    /// The event path of the service: request handlers the event substrate dispatches to,
    /// one per request address (<c>Association-Adding</c>, <c>-Modifying</c>,
    /// <c>-RemovingById</c>, <c>-HardRemovingById</c>, <c>-RetrievingById</c>). Handlers
    /// receive the full request envelope — including the original caller's
    /// <c>SecurityContext</c> — converge on the same private <c>DoXAsync</c> methods the
    /// non-event path uses (which publish the past-tense facts and record both the inbound
    /// and outbound event ids in the <c>ProcessedEvents</c> table), and return the outcome
    /// as the delivery's reply envelope. Mutating handlers check that table first so replayed
    /// or duplicated requests — including a published fact ever looping back into a request
    /// handler — are not applied twice; a deduplicated delivery replies <c>null</c>. Failures
    /// are categorized into the service's typed exceptions and rethrown so the substrate
    /// records the delivery as <c>Error</c> and drives retries; they are never swallowed.
    /// </summary>
    internal partial class AssociationService
    {
        public ValueTask<EventEnvelope<Association>?> OnAddingAssociationAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateAssociationEventEnvelopeAsync(
                    envelope, AssociationEventOperation.Adding);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .AssociationOnAddingAssociationSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Association addedAssociation = await DoAddAssociationAsync(
                    association: envelope.Content,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: addedAssociation);
            });

        public ValueTask<EventEnvelope<Association>?> OnModifyingAssociationAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateAssociationEventEnvelopeAsync(
                    envelope, AssociationEventOperation.Modifying);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .AssociationOnModifyingAssociationSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Association modifiedAssociation =
                    await DoModifyAssociationAsync(
                        association: envelope.Content,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: modifiedAssociation);
            });

        public ValueTask<EventEnvelope<Association>?> OnRemovingAssociationByIdAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateAssociationEventEnvelopeAsync(
                    envelope, AssociationEventOperation.RemovingById);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .AssociationOnRemovingAssociationByIdSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Association removedAssociation =
                    await DoRemoveAssociationByIdAsync(
                        associationId: envelope.Content.Id,
                        deletionReason: envelope.Content.DeletionReason,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: removedAssociation);
            });

        public ValueTask<EventEnvelope<Association>?> OnHardRemovingAssociationByIdAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateAssociationEventEnvelopeAsync(
                    envelope, AssociationEventOperation.HardRemovingById);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .AssociationOnHardRemovingAssociationByIdSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Association deletedAssociation =
                    await DoHardRemoveAssociationByIdAsync(
                        associationId: envelope.Content.Id,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: deletedAssociation);
            });

        public ValueTask<EventEnvelope<Association>?> OnRetrievingAssociationByIdAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateAssociationEventEnvelopeAsync(
                    envelope, AssociationEventOperation.RetrievingById);

                // read-only: naturally idempotent, so no ProcessedEvents bookkeeping; the
                // shared do-work runs the visibility posture against the REQUEST envelope's
                // security context, not the ambient one
                Association retrievedAssociation =
                    await DoRetrieveAssociationByIdAsync(
                        associationId: envelope.Content.Id,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: retrievedAssociation);
            });

        private async ValueTask<bool> AlreadyProcessedAsync(
            EventEnvelope<Association> envelope,
            string receiverName,
            CancellationToken cancellationToken) =>
            await this.storageBroker.SelectProcessedEventExistsAsync(
                eventId: envelope.Metadata.EventId,
                receiverName: receiverName,
                cancellationToken: cancellationToken);

        private async ValueTask RecordEventProcessedAsync(
            EventEnvelope<Association> envelope,
            string receiverName,
            CancellationToken cancellationToken) =>
            await this.storageBroker.InsertProcessedEventAsync(
                processedEvent: new ProcessedEvent
                {
                    Id = await this.identifierBroker.GetIdentifierAsync(),
                    EventId = envelope.Metadata.EventId,
                    ReceiverName = receiverName,
                    ProcessedAt = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync()
                },
                cancellationToken: cancellationToken);

        // ── State-transition handlers (design §9.7.1, §9.2) ───────────────────────────
        //
        // One handler per request address, each converging on the same DoX the direct path
        // uses so the two entry paths cannot diverge.
        //
        // Sort has no handler because it has no request address: its signature needs an
        // anchor and a side, and an envelope carries exactly one entity.

        public ValueTask<EventEnvelope<Association>?> OnApprovingAssociationAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateAssociationEventEnvelopeAsync(
                    envelope, AssociationEventOperation.Approving);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .AssociationOnApprovingAssociationSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Association transitionedAssociation =
                    await DoTransitionAssociationApprovalAsync(
                        association: envelope.Content,
                        inboundEnvelope: envelope,

                        // Admissible because the envelope was VERIFIED above. Only this system
                        // holds the signing key, so a valid signature is what establishes that
                        // the envelope was minted here rather than by whoever could reach the
                        // address — and the security context is inside the signed payload, so a
                        // caller cannot set the flag on a genuine envelope without breaking the
                        // HMAC, nor mint a fresh one carrying it.
                        //
                        // That is what lets the approval workflow sync its decision onto the
                        // entity over an event at all (§16.7.1). It rests on the signing key
                        // never leaving this system; a future host that publishes with a key of
                        // its own would be asserting this identity too, and would need its own
                        // answer here.
                        isSystemIdentityAdmissible: true,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: transitionedAssociation);
            });

        public ValueTask<EventEnvelope<Association>?> OnSettingAssociationConfidenceAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateAssociationEventEnvelopeAsync(
                    envelope, AssociationEventOperation.SettingConfidence);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .AssociationOnSettingAssociationConfidenceSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Association transitionedAssociation =
                    await DoSetAssociationConfidenceAsync(
                        association: envelope.Content,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: transitionedAssociation);
            });

        public ValueTask<EventEnvelope<Association>?> OnSettingAssociationScopeAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateAssociationEventEnvelopeAsync(
                    envelope, AssociationEventOperation.SettingScope);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .AssociationOnSettingAssociationScopeSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                // The event path states both scopes explicitly. The envelope carries an
                // entity whose Scope properties are non-nullable, so "not supplied" cannot be
                // expressed here — sending the entity's values as-is is therefore the honest
                // reading of the request, and the direct path keeps the nullable form for
                // callers who really do want to change one endpoint only.
                Association transitionedAssociation =
                    await DoSetAssociationScopeAsync(
                        associationId: envelope.Content.Id,
                        entityAScope: envelope.Content.EntityAScope,
                        entityBScope: envelope.Content.EntityBScope,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: transitionedAssociation);
            });

    }
}
