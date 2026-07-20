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
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Foundations.ContentTypes.Exceptions;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;

namespace Glory2Him.Core.Services.Foundations.ContentTypes
{
    /// <summary>
    /// The event path of the service: request handlers the event substrate dispatches to,
    /// one per request address (<c>ContentType-Adding</c>, <c>-Modifying</c>,
    /// <c>-RemovingById</c>, <c>-RetrievingById</c>). Handlers receive the full request
    /// envelope — including the original caller's <c>SecurityContext</c> — converge on the
    /// same private <c>DoXAsync</c> methods the non-event path uses (which publish the
    /// past-tense facts), and return the outcome as the delivery's reply envelope. Mutating
    /// handlers are guarded by the <c>ProcessedEvents</c> table so replayed or duplicated
    /// requests are not applied twice; a deduplicated delivery replies <c>null</c>. Failures
    /// are categorized into the service's typed exceptions and rethrown so the substrate
    /// records the delivery as <c>Error</c> and drives retries; they are never swallowed.
    /// </summary>
    public partial class ContentTypeService
    {
        public ValueTask<EventEnvelope<ContentType>?> OnAddingContentTypeAsync(
            EventEnvelope<ContentType> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateContentTypeEventEnvelope(envelope);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope,
                    EventBrokerIdentifiers.ContentTypeOnAddingContentTypeSubscriptionName,
                    cancellationToken);

                if (alreadyProcessed)
                    return null;

                ContentType addedContentType =
                    await DoAddContentTypeAsync(envelope.Content, envelope, cancellationToken);

                await RecordEventProcessedAsync(
                    envelope,
                    EventBrokerIdentifiers.ContentTypeOnAddingContentTypeSubscriptionName,
                    cancellationToken);

                return await this.eventEnvelopeFactory.CreateNextAsync(envelope, addedContentType);
            });

        public ValueTask<EventEnvelope<ContentType>?> OnModifyingContentTypeAsync(
            EventEnvelope<ContentType> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateContentTypeEventEnvelope(envelope);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope,
                    EventBrokerIdentifiers.ContentTypeOnModifyingContentTypeSubscriptionName,
                    cancellationToken);

                if (alreadyProcessed)
                    return null;

                ContentType modifiedContentType =
                    await DoModifyContentTypeAsync(envelope.Content, envelope, cancellationToken);

                await RecordEventProcessedAsync(
                    envelope,
                    EventBrokerIdentifiers.ContentTypeOnModifyingContentTypeSubscriptionName,
                    cancellationToken);

                return await this.eventEnvelopeFactory.CreateNextAsync(envelope, modifiedContentType);
            });

        public ValueTask<EventEnvelope<ContentType>?> OnRemovingContentTypeByIdAsync(
            EventEnvelope<ContentType> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateContentTypeEventEnvelope(envelope);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope,
                    EventBrokerIdentifiers.ContentTypeOnRemovingContentTypeByIdSubscriptionName,
                    cancellationToken);

                if (alreadyProcessed)
                    return null;

                ContentType removedContentType =
                    await DoRemoveContentTypeByIdAsync(
                        envelope.Content.Id,
                        envelope.Content.DeletionReason,
                        envelope,
                        cancellationToken);

                await RecordEventProcessedAsync(
                    envelope,
                    EventBrokerIdentifiers.ContentTypeOnRemovingContentTypeByIdSubscriptionName,
                    cancellationToken);

                return await this.eventEnvelopeFactory.CreateNextAsync(envelope, removedContentType);
            });

        public ValueTask<EventEnvelope<ContentType>?> OnRetrievingContentTypeByIdAsync(
            EventEnvelope<ContentType> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateContentTypeEventEnvelope(envelope);

                // read-only: naturally idempotent, so no ProcessedEvents bookkeeping
                ContentType retrievedContentType =
                    await RetrieveContentTypeByIdAsync(envelope.Content.Id, cancellationToken);

                return await this.eventEnvelopeFactory.CreateNextAsync(
                    envelope,
                    retrievedContentType);
            });

        private async ValueTask<bool> AlreadyProcessedAsync(
            EventEnvelope<ContentType> envelope,
            string receiverName,
            CancellationToken cancellationToken) =>
            await this.storageBroker.SelectProcessedEventExistsAsync(
                envelope.Metadata.EventId,
                receiverName,
                cancellationToken);

        private async ValueTask RecordEventProcessedAsync(
            EventEnvelope<ContentType> envelope,
            string receiverName,
            CancellationToken cancellationToken) =>
            await this.storageBroker.InsertProcessedEventAsync(
                new ProcessedEvent
                {
                    Id = await this.identifierBroker.GetIdentifierAsync(),
                    EventId = envelope.Metadata.EventId,
                    ReceiverName = receiverName,
                    ProcessedAt = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync()
                },
                cancellationToken);

        private static void ValidateContentTypeEventEnvelope(EventEnvelope<ContentType> envelope)
        {
            if (envelope is null || envelope.Content is null)
            {
                throw new InvalidContentTypeEventException(
                    message: "Invalid content type event. " +
                        "The event envelope and its content are required.");
            }
        }
    }
}
