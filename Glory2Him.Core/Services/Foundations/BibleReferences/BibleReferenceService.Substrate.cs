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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;

namespace Glory2Him.Core.Services.Foundations.BibleReferences
{
    /// <summary>
    /// The event path of the service: request handlers the event substrate dispatches to,
    /// one per request address (<c>BibleReference-Adding</c>, <c>-Modifying</c>,
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
    internal partial class BibleReferenceService
    {
        public ValueTask<EventEnvelope<BibleReference>?> OnAddingBibleReferenceAsync(
            EventEnvelope<BibleReference> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateBibleReferenceEventEnvelopeAsync(
                    envelope, BibleReferenceEventOperation.Adding);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.BibleReferenceOnAddingBibleReferenceSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                BibleReference addedBibleReference = await DoAddBibleReferenceAsync(
                    bibleReference: envelope.Content,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: addedBibleReference);
            });

        public ValueTask<EventEnvelope<BibleReference>?> OnModifyingBibleReferenceAsync(
            EventEnvelope<BibleReference> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateBibleReferenceEventEnvelopeAsync(
                    envelope, BibleReferenceEventOperation.Modifying);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.BibleReferenceOnModifyingBibleReferenceSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                BibleReference modifiedBibleReference = await DoModifyBibleReferenceAsync(
                    bibleReference: envelope.Content,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: modifiedBibleReference);
            });

        public ValueTask<EventEnvelope<BibleReference>?> OnRemovingBibleReferenceByIdAsync(
            EventEnvelope<BibleReference> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateBibleReferenceEventEnvelopeAsync(
                    envelope, BibleReferenceEventOperation.RemovingById);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.BibleReferenceOnRemovingBibleReferenceByIdSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                BibleReference removedBibleReference = await DoRemoveBibleReferenceByIdAsync(
                    bibleReferenceId: envelope.Content.Id,
                    deletionReason: envelope.Content.DeletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: removedBibleReference);
            });

        public ValueTask<EventEnvelope<BibleReference>?> OnHardRemovingBibleReferenceByIdAsync(
            EventEnvelope<BibleReference> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateBibleReferenceEventEnvelopeAsync(
                    envelope, BibleReferenceEventOperation.HardRemovingById);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.BibleReferenceOnHardRemovingBibleReferenceByIdSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                BibleReference deletedBibleReference = await DoHardRemoveBibleReferenceByIdAsync(
                    bibleReferenceId: envelope.Content.Id,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: deletedBibleReference);
            });

        public ValueTask<EventEnvelope<BibleReference>?> OnRetrievingBibleReferenceByIdAsync(
            EventEnvelope<BibleReference> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateBibleReferenceEventEnvelopeAsync(
                    envelope, BibleReferenceEventOperation.RetrievingById);

                // read-only: naturally idempotent, so no ProcessedEvents bookkeeping; the
                // shared do-work runs the visibility posture against the REQUEST envelope's
                // security context, not the ambient one
                BibleReference retrievedBibleReference = await DoRetrieveBibleReferenceByIdAsync(
                    bibleReferenceId: envelope.Content.Id,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: retrievedBibleReference);
            });

        public ValueTask<EventEnvelope<BibleReference>?> OnSubmittingBibleReferenceAsync(
            EventEnvelope<BibleReference> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateBibleReferenceEventEnvelopeAsync(
                    envelope, BibleReferenceEventOperation.Submitting);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .BibleReferenceOnSubmittingBibleReferenceSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                // Submit owns only the status, so the id is the whole request; the envelope's
                // other fields are the caller's copy and never trusted by the do-work.
                BibleReference submittedBibleReference =
                    await DoSubmitBibleReferenceAsync(
                        bibleReferenceId: envelope.Content.Id,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: submittedBibleReference);
            });

        public ValueTask<EventEnvelope<BibleReference>?> OnApprovingBibleReferenceAsync(
            EventEnvelope<BibleReference> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateBibleReferenceEventEnvelopeAsync(
                    envelope, BibleReferenceEventOperation.Approving);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .BibleReferenceOnApprovingBibleReferenceSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                BibleReference approvedBibleReference =
                    await DoTransitionBibleReferenceApprovalAsync(
                        bibleReference: envelope.Content,
                        inboundEnvelope: envelope,

                        // This envelope arrived over a PUBLIC event address and its security
                        // context was deserialized, not authenticated (§14.6 rule 4). A caller
                        // who could assert the system identity here would be granted the
                        // workflow's own authority — including the override out of a terminal
                        // state — simply by setting a JSON property. The claim is discarded and
                        // the caller is treated as the ordinary unprivileged one they are.
                        isSystemIdentityAdmissible: false,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: approvedBibleReference);
            });

        private async ValueTask<bool> AlreadyProcessedAsync(
            EventEnvelope<BibleReference> envelope,
            string receiverName,
            CancellationToken cancellationToken) =>
            await this.storageBroker.SelectProcessedEventExistsAsync(
                eventId: envelope.Metadata.EventId,
                receiverName: receiverName,
                cancellationToken: cancellationToken);

        private async ValueTask RecordEventProcessedAsync(
            EventEnvelope<BibleReference> envelope,
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
    }
}
