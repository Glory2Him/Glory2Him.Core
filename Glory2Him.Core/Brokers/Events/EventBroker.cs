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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventHighway.Abstractions.EventHandlers;
using EventHighway.Core.Clients.EventHighways;
using EventHighway.Core.Models.Configurations;
using EventHighway.Core.Models.Services.Foundations.EventAddresses.V2;
using EventHighway.Core.Models.Services.Foundations.EventListeners.V2;
using EventHighway.Core.Models.Services.Foundations.EventParticipants.V2;
using EventHighway.Core.Models.Services.Foundations.Events.V2;
using EventHighway.Core.Models.Services.Foundations.ListenerEvents.V2;
using EventHighway.EventHandlers;
using EventHighway.SqlServer;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Microsoft.Extensions.Configuration;

namespace Glory2Him.Core.Brokers.Events
{
    internal partial class EventBroker : IEventBroker
    {
        private readonly EventHighwayClient eventHighwayClient;
        private readonly IEnvelopeIntegrityBroker envelopeIntegrityBroker;

        public EventBroker(
            IConfiguration configuration,
            IEnvelopeIntegrityBroker envelopeIntegrityBroker)
        {
            string connectionString = configuration
                .GetConnectionString(name: "EventHighwayConnectionString") ?? string.Empty;

            this.eventHighwayClient =
                new EventHighwayClient(
                    new SqlServerStorageBrokerProvider(connectionString),
                    new EventHighwayConfiguration());

            this.envelopeIntegrityBroker = envelopeIntegrityBroker;
        }

        public async ValueTask RegisterEventParticipantAsync(
            CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            await this.eventHighwayClient.V2.EventParticipantV2Client
                .RetrieveOrAddEventParticipantV2Async(
                    new EventParticipantV2
                    {
                        Id = EventBrokerIdentifiers.Glory2HimParticipantId,
                        Name = "Glory2Him.Core",
                        Description = "The Glory 2 Him core application. " +
                            "Publishes and consumes all domain events in-process.",
                        IsActive = true,
                        IsSecretRequired = false,
                        CreatedDate = now,
                        UpdatedDate = now
                    },
                    cancellationToken);
        }

        public async ValueTask RegisterEventAddressesAsync(
            CancellationToken cancellationToken = default)
        {
            foreach (KeyValuePair<Guid, string> eventAddress in EventBrokerIdentifiers.EventAddresses)
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;

                await this.eventHighwayClient.V2.EventAddressV2Client
                    .RetrieveOrRegisterEventAddressV2Async(
                        new EventAddressV2
                        {
                            Id = eventAddress.Key,
                            Name = eventAddress.Value,
                            Description = $"Domain events for {eventAddress.Value}.",
                            CreatedDate = now,
                            UpdatedDate = now
                        },
                        cancellationToken);
            }
        }

        public ValueTask FireScheduledPendingEventsAsync(
            CancellationToken cancellationToken = default) =>
                this.eventHighwayClient.V2.EventV2Client
                    .FireScheduledPendingEventV2sAsync(cancellationToken);

        private async ValueTask<EventPublishResult<T>> PublishEventAsync<T, TOperation>(
            IReadOnlyDictionary<TOperation, Guid> eventAddressIds,
            string entityName,
            EventEnvelope<T> envelope,
            TOperation operation)
            where TOperation : struct, Enum
        {
            Guid eventAddressId = eventAddressIds[operation];
            string eventName = $"{entityName}{operation}";
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Signed here, at the one point that knows the destination — the composed event name
            // and the request direction are bound into the signature, so the stored event is
            // tamper-evident and cannot be lifted onto another address or replayed as a reply.
            EnvelopeIntegrity integrity = await this.envelopeIntegrityBroker.SignAsync(
                envelope, eventName, EnvelopeDirection.Request);

            var signedEnvelope = new EventEnvelope<T>
            {
                Content = envelope.Content,
                SecurityContext = envelope.SecurityContext,
                RequestContext = envelope.RequestContext,
                Metadata = envelope.Metadata,
                Integrity = integrity
            };

            var eventV2 = new EventV2
            {
                Id = Guid.CreateVersion7(),
                Content = JsonSerializer.Serialize(signedEnvelope),
                EventName = eventName,
                EventAddressV2Id = eventAddressId,
                EventParticipantV2Id = EventBrokerIdentifiers.Glory2HimParticipantId,
                CreatedDate = now,
                UpdatedDate = now
            };

            EventV2 submittedEventV2 = await this.eventHighwayClient.V2.EventV2Client
                .SubmitEventV2Async(eventV2, CancellationToken.None);

            // The reply the receiver signed is bound to the entity type and operation plus the
            // Reply direction; the publisher rebuilds the same name to verify it on read-back.
            string replyEventName = $"{typeof(T).Name}{operation}";
            var deliveries = new List<EventDelivery<T>>();

            foreach (ListenerEventV2 listenerEventV2 in
                submittedEventV2.ListenerEventV2s ?? Enumerable.Empty<ListenerEventV2>())
            {
                deliveries.Add(new EventDelivery<T>
                {
                    SubscriptionId = listenerEventV2.EventListenerV2Id,
                    IsSuccess = listenerEventV2.Status == ListenerEventStatusV2.Success,
                    Status = listenerEventV2.Status.ToString(),
                    ResponseCode = listenerEventV2.ResponseCode,
                    ResponseMessage = listenerEventV2.ResponseMessage,

                    // A failed delivery's Response holds diagnostic text, not a reply envelope —
                    // only a successful delivery carries a deserializable reply, and it is trusted
                    // only if its signature verifies against this operation and the Reply direction.
                    // A reply that fails verification (tampered in storage, or a replayed request)
                    // is dropped to null rather than handed back as authentic.
                    Response = await VerifiedReplyOrNullAsync<T>(listenerEventV2, replyEventName)
                });
            }

            return new EventPublishResult<T>
            {
                EventId = submittedEventV2.Id,
                Deliveries = deliveries
            };
        }

        private async ValueTask<EventEnvelope<T>?> VerifiedReplyOrNullAsync<T>(
            ListenerEventV2 listenerEventV2,
            string replyEventName)
        {
            bool hasReply =
                listenerEventV2.Status == ListenerEventStatusV2.Success
                    && !string.IsNullOrWhiteSpace(listenerEventV2.Response);

            if (hasReply is false)
            {
                return null;
            }

            EventEnvelope<T> replyEnvelope = DeserializeEnvelope<T>(listenerEventV2.Response);

            bool isReplyValid = await this.envelopeIntegrityBroker.VerifyAsync(
                replyEnvelope, replyEventName, EnvelopeDirection.Reply);

            return isReplyValid ? replyEnvelope : null;
        }

        private async ValueTask<EventEnvelope<T>> SignReplyAsync<T>(
            EventEnvelope<T> replyEnvelope,
            string replyEventName)
        {
            EnvelopeIntegrity integrity = await this.envelopeIntegrityBroker.SignAsync(
                replyEnvelope, replyEventName, EnvelopeDirection.Reply);

            return new EventEnvelope<T>
            {
                Content = replyEnvelope.Content,
                SecurityContext = replyEnvelope.SecurityContext,
                RequestContext = replyEnvelope.RequestContext,
                Metadata = replyEnvelope.Metadata,
                Integrity = integrity
            };
        }

        private ValueTask SubscribeToEventAsync<T, TOperation>(
            IReadOnlyDictionary<TOperation, Guid> eventAddressIds,
            EventSubscription subscription,
            TOperation operation,
            Func<EventEnvelope<T>, CancellationToken, ValueTask> eventHandler,
            CancellationToken cancellationToken)
            where TOperation : struct, Enum
        {
            return SubscribeToEventAsync(
                eventAddressIds: eventAddressIds,
                subscription: subscription,
                operation: operation,
                eventHandler: async (EventEnvelope<T> envelope, CancellationToken handlerCancellationToken) =>
                {
                    await eventHandler(envelope, handlerCancellationToken);

                    return null;
                },
                cancellationToken: cancellationToken);
        }

        private async ValueTask SubscribeToEventAsync<T, TOperation>(
            IReadOnlyDictionary<TOperation, Guid> eventAddressIds,
            EventSubscription subscription,
            TOperation operation,
            Func<EventEnvelope<T>, CancellationToken, ValueTask<EventEnvelope<T>?>> eventHandler,
            CancellationToken cancellationToken)
            where TOperation : struct, Enum
        {
            Guid eventAddressId = eventAddressIds[operation];

            // The reply is bound to the entity type and operation it answers, plus the Reply
            // direction. Composed from the generic T and operation the handler was invoked with —
            // the same pair the publisher has when it reads the reply back — so both sides agree
            // without threading the address name through the subscription API.
            string replyEventName = $"{typeof(T).Name}{operation}";

            var delegateEventHandler = new DelegateEventHandler(
                subscription.Id,
                async (content, contentCancellationToken) =>
                {
                    EventEnvelope<T> envelope = DeserializeEnvelope<T>(content);

                    // The handler's returned envelope is the delivery's response payload,
                    // recorded on the ListenerEventV2 row; null when the handler has none.
                    // The token carries the dispatch pipeline's handler timeout/shutdown.
                    EventEnvelope<T>? responseEnvelope =
                        await eventHandler(envelope, contentCancellationToken);

                    return new EventHandlerResult
                    {
                        IsSuccess = true,
                        ResponseCode = "OK",
                        ResponseMessage = $"{subscription.Name} handled the event.",

                        // Sign the reply so the publisher reading it back can tell an authentic
                        // reply from one tampered in storage or replayed. The Reply direction is
                        // what stops a signed reply being lifted onto a request address and
                        // believed as an inbound command.
                        Response = responseEnvelope is null
                            ? null
                            : JsonSerializer.Serialize(
                                await SignReplyAsync(responseEnvelope, replyEventName))
                    };
                },
                subscription.Name);

            await this.eventHighwayClient.V2.RegisterEventHandlerAsync(
                delegateEventHandler,
                cancellationToken);

            DateTimeOffset now = DateTimeOffset.UtcNow;

            await this.eventHighwayClient.V2.EventListenerV2Client
                .RetrieveOrRegisterEventListenerV2Async(
                    new EventListenerV2
                    {
                        Id = subscription.Id,
                        Name = subscription.Name,
                        Description = subscription.Description,
                        HandlerId = delegateEventHandler.Id,
                        HandlerName = delegateEventHandler.Name,
                        EventAddressV2Id = eventAddressId,
                        EventParticipantV2Id = EventBrokerIdentifiers.Glory2HimParticipantId,
                        CreatedDate = now,
                        UpdatedDate = now
                    },
                    cancellationToken);
        }

        private static EventEnvelope<T> DeserializeEnvelope<T>(string content) =>
            JsonSerializer.Deserialize<EventEnvelope<T>>(content)!;
    }
}
