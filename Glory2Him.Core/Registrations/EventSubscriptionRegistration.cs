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
using Glory2Him.Core.Services.Foundations.Approvals;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Foundations.ContentTypes;
using Glory2Him.Core.Services.Foundations.BibleReferences;

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
        private readonly IApprovalService approvalService;
        private readonly IBibleReferenceService bibleReferenceService;

        public EventSubscriptionRegistration(
            IEventBroker eventBroker,
            IContentTypeService contentTypeService,
            IContentItemService contentItemService,
            IApprovalService approvalService,
            IBibleReferenceService bibleReferenceService)
        {
            this.eventBroker = eventBroker;
            this.contentTypeService = contentTypeService;
            this.contentItemService = contentItemService;
            this.approvalService = approvalService;
            this.bibleReferenceService = bibleReferenceService;
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
                    Id = EventBrokerIdentifiers.ContentItemOnHardRemovingContentItemByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentItemOnHardRemovingContentItemByIdSubscriptionName,

                    Description = "Handles hard-remove requests: permanently deletes the " +
                        "content item, publishes ContentItemHardRemoved on the removal " +
                        "address, and replies with the deleted entity."
                },
                operation: ContentItemEventOperation.HardRemovingById,
                contentItemEventHandler: this.contentItemService.OnHardRemovingContentItemByIdAsync,
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

            // ── Approval request handlers ────────────────────────────────────────
            await this.eventBroker.SubscribeToApprovalEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalOnAddingApprovalSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalOnAddingApprovalSubscriptionName,

                    Description = "Handles add requests: stores the approval, publishes " +
                        "Approval-Added, and replies with the added entity."
                },
                operation: ApprovalEventOperation.Adding,
                approvalEventHandler: this.approvalService.OnAddingApprovalAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalOnModifyingApprovalSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalOnModifyingApprovalSubscriptionName,

                    Description = "Handles modify requests: updates the approval, publishes " +
                        "Approval-Modified, and replies with the updated entity."
                },
                operation: ApprovalEventOperation.Modifying,
                approvalEventHandler: this.approvalService.OnModifyingApprovalAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalOnRemovingApprovalByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalOnRemovingApprovalByIdSubscriptionName,

                    Description = "Handles remove requests: soft-deletes the approval, " +
                        "publishes Approval-Removed, and replies with the removed entity."
                },
                operation: ApprovalEventOperation.RemovingById,
                approvalEventHandler: this.approvalService.OnRemovingApprovalByIdAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalOnHardRemovingApprovalByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalOnHardRemovingApprovalByIdSubscriptionName,

                    Description = "Handles hard-remove requests: permanently deletes the " +
                        "approval, publishes ApprovalHardRemoved on the removal " +
                        "address, and replies with the deleted entity."
                },
                operation: ApprovalEventOperation.HardRemovingById,
                approvalEventHandler: this.approvalService.OnHardRemovingApprovalByIdAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalOnRetrievingApprovalByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalOnRetrievingApprovalByIdSubscriptionName,

                    Description = "Handles retrieve requests: retrieves an approval by id " +
                        "and replies with it on the delivery."
                },
                operation: ApprovalEventOperation.RetrievingById,
                approvalEventHandler: this.approvalService.OnRetrievingApprovalByIdAsync,
                cancellationToken: cancellationToken);

            // ── BibleReference request handlers ──────────────────────────────────
            await this.eventBroker.SubscribeToBibleReferenceEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.BibleReferenceOnAddingBibleReferenceSubscriptionId,
                    Name = EventBrokerIdentifiers.BibleReferenceOnAddingBibleReferenceSubscriptionName,

                    Description = "Handles add requests: stores the bible reference, publishes " +
                        "BibleReference-Added, and replies with the added entity."
                },
                operation: BibleReferenceEventOperation.Adding,
                bibleReferenceEventHandler: this.bibleReferenceService.OnAddingBibleReferenceAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToBibleReferenceEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.BibleReferenceOnModifyingBibleReferenceSubscriptionId,
                    Name = EventBrokerIdentifiers.BibleReferenceOnModifyingBibleReferenceSubscriptionName,

                    Description = "Handles modify requests: updates the bible reference, publishes " +
                        "BibleReference-Modified, and replies with the updated entity."
                },
                operation: BibleReferenceEventOperation.Modifying,
                bibleReferenceEventHandler: this.bibleReferenceService.OnModifyingBibleReferenceAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToBibleReferenceEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.BibleReferenceOnRemovingBibleReferenceByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.BibleReferenceOnRemovingBibleReferenceByIdSubscriptionName,

                    Description = "Handles remove requests: soft-deletes the bible reference, " +
                        "publishes BibleReference-Removed, and replies with the removed entity."
                },
                operation: BibleReferenceEventOperation.RemovingById,
                bibleReferenceEventHandler: this.bibleReferenceService.OnRemovingBibleReferenceByIdAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToBibleReferenceEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.BibleReferenceOnHardRemovingBibleReferenceByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .BibleReferenceOnHardRemovingBibleReferenceByIdSubscriptionName,

                    Description = "Handles hard-remove requests: permanently deletes the " +
                        "bible reference, publishes BibleReferenceHardRemoved on the removal " +
                        "address, and replies with the deleted entity."
                },
                operation: BibleReferenceEventOperation.HardRemovingById,
                bibleReferenceEventHandler: this.bibleReferenceService.OnHardRemovingBibleReferenceByIdAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToBibleReferenceEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.BibleReferenceOnRetrievingBibleReferenceByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .BibleReferenceOnRetrievingBibleReferenceByIdSubscriptionName,

                    Description = "Handles retrieve requests: retrieves a bible reference by id " +
                        "and replies with it on the delivery."
                },
                operation: BibleReferenceEventOperation.RetrievingById,
                bibleReferenceEventHandler: this.bibleReferenceService.OnRetrievingBibleReferenceByIdAsync,
                cancellationToken: cancellationToken);
        }
    }
}
