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
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Foundations.ContentTypes;

namespace Glory2Him.Core.Registrations
{
    /// <summary>
    /// The single, central place where all event subscriptions are configured. Reading
    /// <see cref="RegisterSubscriptionsAsync"/> shows exactly which subscriptions the system
    /// currently runs; nothing subscribes to events anywhere else.
    /// </summary>
    /// <remarks>
    /// Wire-up at application startup:
    /// <code>
    /// services.AddSingleton&lt;IEventBroker, EventBroker&gt;();
    /// services.AddSingleton&lt;IEventEnvelopeFactory, EventEnvelopeFactory&gt;();
    /// services.AddSingleton&lt;IContentTypeService, ContentTypeService&gt;();
    /// services.AddSingleton&lt;IEventSubscriptionRegistration, EventSubscriptionRegistration&gt;();
    ///
    /// // after the container is built:
    /// await app.Services.GetRequiredService&lt;IEventSubscriptionRegistration&gt;().RegisterAsync();
    /// </code>
    /// The <c>EventBroker</c> must be registered as a singleton: subscriptions register their
    /// handlers in the broker instance, and events published through a different instance would
    /// not reach them. Services whose handlers are subscribed here are captured by that
    /// singleton, so they (and their storage broker) must be singleton-compatible; if the host
    /// registers them scoped, wire the subscription through an <c>IServiceScopeFactory</c>
    /// lambda instead of a method group. The broker requires an
    /// <c>EventHighwayConnectionString</c> connection string; the event store schema is
    /// created and migrated automatically on first use.
    /// </remarks>
    public class EventSubscriptionRegistration : IEventSubscriptionRegistration
    {
        private readonly IEventBroker eventBroker;
        private readonly IContentTypeService contentTypeService;
        private readonly IContentItemService contentItemService;

        public EventSubscriptionRegistration(
            IEventBroker eventBroker,
            IContentTypeService contentTypeService,
            IContentItemService contentItemService)
        {
            this.eventBroker = eventBroker;
            this.contentTypeService = contentTypeService;
            this.contentItemService = contentItemService;
        }

        public async ValueTask RegisterAsync(CancellationToken cancellationToken = default)
        {
            await this.eventBroker.RegisterEventParticipantAsync(cancellationToken);
            await this.eventBroker.RegisterEventAddressesAsync(cancellationToken);
            await RegisterSubscriptionsAsync(cancellationToken);
        }

        private async ValueTask RegisterSubscriptionsAsync(CancellationToken cancellationToken)
        {
            // Every event subscription in the system is registered here — one block per
            // subscription, each with a stable Id and globally unique Name so registration
            // stays idempotent across restarts. Each subscription binds to exactly one
            // operation (one event address per entity operation); to receive every operation
            // for an entity, declare one subscription per operation.
            //
            // Handlers live on the owning service's .Substrate partial and come in two shapes:
            // Func<EventEnvelope<T>, CancellationToken, ValueTask> for notification-style
            // subscribers, and Func<EventEnvelope<T>, CancellationToken,
            // ValueTask<EventEnvelope<T>?>> for responders — the returned reply envelope is
            // recorded as the delivery's response on its ListenerEventV2 row (null = no reply).

            // ── ContentType request handlers ─────────────────────────────────────
            await this.eventBroker.SubscribeToContentTypeEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ContentTypeOnAddingContentTypeSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentTypeOnAddingContentTypeSubscriptionName,

                    Description = "Handles add requests: stores the content type, publishes " +
                        "ContentType-Added, and replies with the added entity."
                },
                operation: ContentTypeEventOperation.Adding,
                contentTypeEventHandler: this.contentTypeService.OnAddingContentTypeAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentTypeEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ContentTypeOnModifyingContentTypeSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentTypeOnModifyingContentTypeSubscriptionName,

                    Description = "Handles modify requests: updates the content type, publishes " +
                        "ContentType-Modified, and replies with the updated entity."
                },
                operation: ContentTypeEventOperation.Modifying,
                contentTypeEventHandler: this.contentTypeService.OnModifyingContentTypeAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentTypeEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ContentTypeOnRemovingContentTypeByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentTypeOnRemovingContentTypeByIdSubscriptionName,

                    Description = "Handles remove requests: soft-deletes the content type, " +
                        "publishes ContentType-Removed, and replies with the removed entity."
                },
                operation: ContentTypeEventOperation.RemovingById,
                contentTypeEventHandler: this.contentTypeService.OnRemovingContentTypeByIdAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentTypeEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ContentTypeOnHardRemovingContentTypeByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentTypeOnHardRemovingContentTypeByIdSubscriptionName,

                    Description = "Handles hard-remove requests: permanently deletes the " +
                        "content type, publishes ContentTypeHardRemoved on the removal " +
                        "address, and replies with the deleted entity."
                },
                operation: ContentTypeEventOperation.HardRemovingById,
                contentTypeEventHandler: this.contentTypeService.OnHardRemovingContentTypeByIdAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentTypeEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ContentTypeOnRetrievingContentTypeByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentTypeOnRetrievingContentTypeByIdSubscriptionName,

                    Description = "Handles retrieve requests: retrieves a content type by id " +
                        "and replies with it on the delivery."
                },
                operation: ContentTypeEventOperation.RetrievingById,
                contentTypeEventHandler: this.contentTypeService.OnRetrievingContentTypeByIdAsync,
                cancellationToken: cancellationToken);

            // ── ContentItem request handlers ─────────────────────────────────────
            await this.eventBroker.SubscribeToContentItemEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ContentItemOnAddingContentItemSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentItemOnAddingContentItemSubscriptionName,

                    Description = "Handles add requests: stores the content item, publishes " +
                        "ContentItem-Added, and replies with the added entity."
                },
                operation: ContentItemEventOperation.Adding,
                contentItemEventHandler: this.contentItemService.OnAddingContentItemAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ContentItemOnModifyingContentItemSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentItemOnModifyingContentItemSubscriptionName,

                    Description = "Handles modify requests: updates the content item, publishes " +
                        "ContentItem-Modified, and replies with the updated entity."
                },
                operation: ContentItemEventOperation.Modifying,
                contentItemEventHandler: this.contentItemService.OnModifyingContentItemAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ContentItemOnRemovingContentItemByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentItemOnRemovingContentItemByIdSubscriptionName,

                    Description = "Handles remove requests: soft-deletes the content item, " +
                        "publishes ContentItem-Removed, and replies with the removed entity."
                },
                operation: ContentItemEventOperation.RemovingById,
                contentItemEventHandler: this.contentItemService.OnRemovingContentItemByIdAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ContentItemOnRetrievingContentItemByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentItemOnRetrievingContentItemByIdSubscriptionName,

                    Description = "Handles retrieve requests: retrieves a content item by id " +
                        "and replies with it on the delivery."
                },
                operation: ContentItemEventOperation.RetrievingById,
                contentItemEventHandler: this.contentItemService.OnRetrievingContentItemByIdAsync,
                cancellationToken: cancellationToken);
        }
    }
}
