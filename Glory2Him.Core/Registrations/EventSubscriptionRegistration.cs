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
using Glory2Him.Core.Brokers.Events;
using Microsoft.Extensions.DependencyInjection;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Services.Foundations.ApprovalComments;
using Glory2Him.Core.Services.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Services.Foundations.ApprovalReviews;
using Glory2Him.Core.Services.Foundations.Approvals;
using Glory2Him.Core.Services.Foundations.ApprovalSettings;
using Glory2Him.Core.Services.Foundations.Associations;
using Glory2Him.Core.Services.Foundations.BibleReferences;
using Glory2Him.Core.Services.Foundations.Comments;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Foundations.ContentItemSettings;
using Glory2Him.Core.Services.Foundations.Links;
using Glory2Him.Core.Services.Foundations.Reactions;
using Glory2Him.Core.Services.Foundations.Tags;
using Glory2Him.Core.Services.Orchestrations.Approvals;
using Glory2Him.Core.Services.Processings.ContentItems;
using Glory2Him.Core.Services.Processings.Links;

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
    /// // EventBroker signs on publish and every foundation service verifies on receive,
    /// // so the integrity broker must be registered alongside them. Its keys come from the
    /// // EventEnvelopeSigning configuration section.
    /// services.AddSingleton&lt;IEnvelopeIntegrityBroker, EnvelopeIntegrityBroker&gt;();
    /// services.AddSingleton&lt;IEventBroker, EventBroker&gt;();
    /// services.AddSingleton&lt;IEventEnvelopeFactory, EventEnvelopeFactory&gt;();
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
    internal class EventSubscriptionRegistration : IEventSubscriptionRegistration
    {
        private readonly IEventBroker eventBroker;
        private readonly IServiceScopeFactory serviceScopeFactory;

        public EventSubscriptionRegistration(
            IEventBroker eventBroker,
            IServiceScopeFactory serviceScopeFactory)
        {
            this.eventBroker = eventBroker;
            this.serviceScopeFactory = serviceScopeFactory;
        }

        // Every handler below is bound through here rather than as a method group on a held
        // service, and the reason is measured rather than defensive.
        //
        // Substrate delivery is serialised WITHIN one publish but fully parallel ACROSS
        // concurrent publishes — eight simultaneous publishes were observed running eight
        // handlers at once on eight threads. A service captured once at registration would
        // therefore hand the same StorageBroker — a DbContext, which is not thread-safe — to
        // all eight. The race is invisible in a single-threaded test and certain under load.
        //
        // So the scope is opened per DELIVERY: the service, its DbContext and its request-bound
        // brokers live exactly as long as the one fact being handled, which is the same lifetime
        // they would have serving one HTTP request. This is what lets the host register them
        // scoped and still bind them here, the arrangement this class's remarks called for.
        private Func<EventEnvelope<TEntity>, CancellationToken, ValueTask<EventEnvelope<TEntity>?>>
            Scoped<TService, TEntity>(
                Func<TService, Func<EventEnvelope<TEntity>, CancellationToken,
                    ValueTask<EventEnvelope<TEntity>?>>> handler)
                where TService : notnull =>
                async (envelope, cancellationToken) =>
                {
                    await using AsyncServiceScope scope =
                        this.serviceScopeFactory.CreateAsyncScope();

                    TService service = scope.ServiceProvider.GetRequiredService<TService>();

                    return await handler(service)(envelope, cancellationToken);
                };

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
                contentItemEventHandler: Scoped<IContentItemService, ContentItem>(
                        service => service.OnAddingContentItemAsync),
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
                contentItemEventHandler: Scoped<IContentItemService, ContentItem>(
                        service => service.OnModifyingContentItemAsync),
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
                contentItemEventHandler: Scoped<IContentItemService, ContentItem>(
                        service => service.OnRemovingContentItemByIdAsync),
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
                contentItemEventHandler: Scoped<IContentItemService, ContentItem>(
                        service => service.OnHardRemovingContentItemByIdAsync),
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
                contentItemEventHandler: Scoped<IContentItemService, ContentItem>(
                        service => service.OnRetrievingContentItemByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ContentItemOnSubmittingContentItemSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentItemOnSubmittingContentItemSubscriptionName,

                    Description = "Handles submit requests: moves a content item Draft -> " +
                        "Submitted, publishes ContentItem-Submitted, and replies with the " +
                        "updated entity."
                },
                operation: ContentItemEventOperation.Submitting,
                contentItemEventHandler: Scoped<IContentItemService, ContentItem>(
                        service => service.OnSubmittingContentItemAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ContentItemOnApprovingContentItemSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentItemOnApprovingContentItemSubscriptionName,

                    Description = "Handles approve requests: decides a submitted content item, " +
                        "publishes ContentItem-Approved or ContentItem-Rejected per the " +
                        "decision, and replies with the updated entity."
                },
                operation: ContentItemEventOperation.Approving,
                contentItemEventHandler: Scoped<IContentItemService, ContentItem>(
                        service => service.OnApprovingContentItemAsync),
                cancellationToken: cancellationToken);

            // ── ContentItem processing request handlers ───────────────────────
            // The publication swap. Versioned entities are approved through HERE, not through
            // the foundation address: granting approval also has to clear the group's published
            // slot first, and the unique filtered index refuses a promote that runs while the
            // incumbent still holds it (§9.7.7 rule 7, §12.4.1 rule 10).
            await this.eventBroker.SubscribeToContentItemProcessingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ContentItemProcessingOnApprovingContentItemSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentItemProcessingOnApprovingContentItemSubscriptionName,

                    Description = "Handles the approval command for a versioned contentItem: "
                        + "unpublishes the group's previously published row, then forwards the "
                        + "decision to the foundation, which publishes ContentItem-Approved or "
                        + "ContentItem-Rejected."
                },
                operation: ContentItemProcessingEventOperation.Approving,
                contentItemProcessingEventHandler: Scoped<IContentItemProcessingService, ContentItem>(
                        service => service.OnApprovingContentItemAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemProcessingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ContentItemProcessingOnAddingContentItemSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentItemProcessingOnAddingContentItemSubscriptionName,

                    Description = "Handles add requests: runs the contribution gate and the " +
                        "duplicate-content rule, adds the content item via the foundation " +
                        "service (which publishes ContentItem-Added), and replies with the " +
                        "created entity; duplicate adds fail as already existing."
                },
                operation: ContentItemProcessingEventOperation.Adding,
                contentItemProcessingEventHandler: Scoped<IContentItemProcessingService, ContentItem>(
                        service => service.OnAddingContentItemAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemProcessingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ContentItemProcessingOnModifyingContentItemSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentItemProcessingOnModifyingContentItemSubscriptionName,

                    Description = "Handles modify requests: runs the contribution gate, the " +
                        "ownership/role permission rules and the duplicate-content rule, then " +
                        "modifies the content item in place (which publishes ContentItem-Modified) " +
                        "or forks a new version for an owner modify of an approved item (which " +
                        "publishes ContentItem-Added), and replies with the resulting entity."
                },
                operation: ContentItemProcessingEventOperation.Modifying,
                contentItemProcessingEventHandler: Scoped<IContentItemProcessingService, ContentItem>(
                        service => service.OnModifyingContentItemAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemProcessingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ContentItemProcessingOnRemovingContentItemByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentItemProcessingOnRemovingContentItemByIdSubscriptionName,

                    Description = "Handles remove requests: runs the contribution gate and the " +
                        "owner/Administrators permission rule, then soft deletes the content item via " +
                        "the foundation service (which publishes ContentItem-Removed), and " +
                        "replies with the removed entity; ApprovalStatus is left untouched."
                },
                operation: ContentItemProcessingEventOperation.RemovingById,
                contentItemProcessingEventHandler: Scoped<IContentItemProcessingService, ContentItem>(
                        service => service.OnRemovingContentItemByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemProcessingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ContentItemProcessingOnRetrievingContentItemByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentItemProcessingOnRetrievingContentItemByIdSubscriptionName,

                    Description = "Handles retrieve requests: applies the canonical content " +
                        "visibility rules — public versions reply for any caller, non-public " +
                        "versions only for the owner or a review role — and replies with the " +
                        "retrieved entity on the delivery; no completion fact is published."
                },
                operation: ContentItemProcessingEventOperation.RetrievingById,

                contentItemProcessingEventHandler:
                    Scoped<IContentItemProcessingService, ContentItem>(
                        service => service.OnRetrievingContentItemByIdAsync),

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
                approvalEventHandler: Scoped<IApprovalService, Approval>(
                        service => service.OnAddingApprovalAsync),
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
                approvalEventHandler: Scoped<IApprovalService, Approval>(
                        service => service.OnModifyingApprovalAsync),
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
                approvalEventHandler: Scoped<IApprovalService, Approval>(
                        service => service.OnRemovingApprovalByIdAsync),
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
                approvalEventHandler: Scoped<IApprovalService, Approval>(
                        service => service.OnHardRemovingApprovalByIdAsync),
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
                approvalEventHandler: Scoped<IApprovalService, Approval>(
                        service => service.OnRetrievingApprovalByIdAsync),
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
                bibleReferenceEventHandler: Scoped<IBibleReferenceService, BibleReference>(
                        service => service.OnAddingBibleReferenceAsync),
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
                bibleReferenceEventHandler: Scoped<IBibleReferenceService, BibleReference>(
                        service => service.OnModifyingBibleReferenceAsync),
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
                bibleReferenceEventHandler: Scoped<IBibleReferenceService, BibleReference>(
                        service => service.OnRemovingBibleReferenceByIdAsync),
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
                bibleReferenceEventHandler: Scoped<IBibleReferenceService, BibleReference>(
                        service => service.OnHardRemovingBibleReferenceByIdAsync),
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
                bibleReferenceEventHandler: Scoped<IBibleReferenceService, BibleReference>(
                        service => service.OnRetrievingBibleReferenceByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToBibleReferenceEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.BibleReferenceOnSubmittingBibleReferenceSubscriptionId,
                    Name = EventBrokerIdentifiers.BibleReferenceOnSubmittingBibleReferenceSubscriptionName,

                    Description = "Handles submit requests: moves a bibleReference Draft -> Submitted, " +
                        "publishes BibleReference-Submitted, and replies with the updated entity."
                },
                operation: BibleReferenceEventOperation.Submitting,
                bibleReferenceEventHandler: Scoped<IBibleReferenceService, BibleReference>(
                        service => service.OnSubmittingBibleReferenceAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToBibleReferenceEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.BibleReferenceOnApprovingBibleReferenceSubscriptionId,
                    Name = EventBrokerIdentifiers.BibleReferenceOnApprovingBibleReferenceSubscriptionName,

                    Description = "Handles approve requests: decides a submitted bibleReference, " +
                        "publishes BibleReference-Approved or BibleReference-Rejected per the decision, and replies " +
                        "with the updated entity."
                },
                operation: BibleReferenceEventOperation.Approving,
                bibleReferenceEventHandler: Scoped<IBibleReferenceService, BibleReference>(
                        service => service.OnApprovingBibleReferenceAsync),
                cancellationToken: cancellationToken);

            // ── Tag request handlers ─────────────────────────────────────────────
            await this.eventBroker.SubscribeToTagEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.TagOnAddingTagSubscriptionId,
                    Name = EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,

                    Description = "Handles add requests: stores the tag, publishes " +
                        "Tag-Added, and replies with the added entity."
                },
                operation: TagEventOperation.Adding,
                tagEventHandler: Scoped<ITagService, Tag>(
                        service => service.OnAddingTagAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToTagEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.TagOnModifyingTagSubscriptionId,
                    Name = EventBrokerIdentifiers.TagOnModifyingTagSubscriptionName,

                    Description = "Handles modify requests: updates the tag, publishes " +
                        "Tag-Modified, and replies with the updated entity."
                },
                operation: TagEventOperation.Modifying,
                tagEventHandler: Scoped<ITagService, Tag>(
                        service => service.OnModifyingTagAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToTagEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.TagOnRemovingTagByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.TagOnRemovingTagByIdSubscriptionName,

                    Description = "Handles remove requests: soft-deletes the tag, " +
                        "publishes Tag-Removed, and replies with the removed entity."
                },
                operation: TagEventOperation.RemovingById,
                tagEventHandler: Scoped<ITagService, Tag>(
                        service => service.OnRemovingTagByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToTagEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.TagOnHardRemovingTagByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.TagOnHardRemovingTagByIdSubscriptionName,

                    Description = "Handles hard-remove requests: permanently deletes the " +
                        "tag, publishes TagHardRemoved on the removal " +
                        "address, and replies with the deleted entity."
                },
                operation: TagEventOperation.HardRemovingById,
                tagEventHandler: Scoped<ITagService, Tag>(
                        service => service.OnHardRemovingTagByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToTagEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.TagOnRetrievingTagByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.TagOnRetrievingTagByIdSubscriptionName,

                    Description = "Handles retrieve requests: retrieves a tag by id " +
                        "and replies with it on the delivery."
                },
                operation: TagEventOperation.RetrievingById,
                tagEventHandler: Scoped<ITagService, Tag>(
                        service => service.OnRetrievingTagByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToTagEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.TagOnSubmittingTagSubscriptionId,
                    Name = EventBrokerIdentifiers.TagOnSubmittingTagSubscriptionName,

                    Description = "Handles submit requests: moves a tag Draft -> Submitted, " +
                        "publishes Tag-Submitted, and replies with the updated entity."
                },
                operation: TagEventOperation.Submitting,
                tagEventHandler: Scoped<ITagService, Tag>(
                        service => service.OnSubmittingTagAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToTagEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.TagOnApprovingTagSubscriptionId,
                    Name = EventBrokerIdentifiers.TagOnApprovingTagSubscriptionName,

                    Description = "Handles approve requests: decides a submitted tag, " +
                        "publishes Tag-Approved or Tag-Rejected per the decision, and replies " +
                        "with the updated entity."
                },
                operation: TagEventOperation.Approving,
                tagEventHandler: Scoped<ITagService, Tag>(
                        service => service.OnApprovingTagAsync),
                cancellationToken: cancellationToken);

            // ── Link request handlers ─────────────────────────────────────
            await this.eventBroker.SubscribeToLinkEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.LinkOnAddingLinkSubscriptionId,
                    Name = EventBrokerIdentifiers.LinkOnAddingLinkSubscriptionName,

                    Description = "Handles add requests: stores the link, publishes " +
                        "Link-Added, and replies with the added entity."
                },
                operation: LinkEventOperation.Adding,
                linkEventHandler: Scoped<ILinkService, Link>(
                        service => service.OnAddingLinkAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToLinkEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.LinkOnModifyingLinkSubscriptionId,
                    Name = EventBrokerIdentifiers.LinkOnModifyingLinkSubscriptionName,

                    Description = "Handles modify requests: updates the link, publishes " +
                        "Link-Modified, and replies with the updated entity."
                },
                operation: LinkEventOperation.Modifying,
                linkEventHandler: Scoped<ILinkService, Link>(
                        service => service.OnModifyingLinkAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToLinkEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.LinkOnRemovingLinkByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.LinkOnRemovingLinkByIdSubscriptionName,

                    Description = "Handles remove requests: soft-deletes the link, " +
                        "publishes Link-Removed, and replies with the removed entity."
                },
                operation: LinkEventOperation.RemovingById,
                linkEventHandler: Scoped<ILinkService, Link>(
                        service => service.OnRemovingLinkByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToLinkEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.LinkOnHardRemovingLinkByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.LinkOnHardRemovingLinkByIdSubscriptionName,

                    Description = "Handles hard-remove requests: permanently deletes the " +
                        "link, publishes LinkHardRemoved on the removal " +
                        "address, and replies with the deleted entity."
                },
                operation: LinkEventOperation.HardRemovingById,
                linkEventHandler: Scoped<ILinkService, Link>(
                        service => service.OnHardRemovingLinkByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToLinkEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.LinkOnRetrievingLinkByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.LinkOnRetrievingLinkByIdSubscriptionName,

                    Description = "Handles retrieve requests: retrieves a link by id " +
                        "and replies with it on the delivery."
                },
                operation: LinkEventOperation.RetrievingById,
                linkEventHandler: Scoped<ILinkService, Link>(
                        service => service.OnRetrievingLinkByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToLinkEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.LinkOnSubmittingLinkSubscriptionId,
                    Name = EventBrokerIdentifiers.LinkOnSubmittingLinkSubscriptionName,

                    Description = "Handles submit requests: moves a link Draft -> Submitted, " +
                        "publishes Link-Submitted, and replies with the updated entity."
                },
                operation: LinkEventOperation.Submitting,
                linkEventHandler: Scoped<ILinkService, Link>(
                        service => service.OnSubmittingLinkAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToLinkEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.LinkOnApprovingLinkSubscriptionId,
                    Name = EventBrokerIdentifiers.LinkOnApprovingLinkSubscriptionName,

                    Description = "Handles approve requests: decides a submitted link, " +
                        "publishes Link-Approved or Link-Rejected per the decision, and replies " +
                        "with the updated entity."
                },
                operation: LinkEventOperation.Approving,
                linkEventHandler: Scoped<ILinkService, Link>(
                        service => service.OnApprovingLinkAsync),
                cancellationToken: cancellationToken);

            // ── Link processing request handlers ──────────────────────────
            // The publication swap. Versioned entities are approved through HERE, not through
            // the foundation address: granting approval also has to clear the group's published
            // slot first, and the unique filtered index refuses a promote that runs while the
            // incumbent still holds it (§9.7.7 rule 7, §12.4.1 rule 10).
            await this.eventBroker.SubscribeToLinkProcessingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.LinkProcessingOnApprovingLinkSubscriptionId,
                    Name = EventBrokerIdentifiers.LinkProcessingOnApprovingLinkSubscriptionName,

                    Description = "Handles the approval command for a versioned link: "
                        + "unpublishes the group's previously published row, then forwards the "
                        + "decision to the foundation, which publishes Link-Approved or "
                        + "Link-Rejected."
                },
                operation: LinkProcessingEventOperation.Approving,
                linkProcessingEventHandler: Scoped<ILinkProcessingService, Link>(
                        service => service.OnApprovingLinkAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToLinkProcessingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.LinkProcessingOnAddingLinkSubscriptionId,
                    Name = EventBrokerIdentifiers.LinkProcessingOnAddingLinkSubscriptionName,

                    Description = "Handles add requests: runs the contribution gate, lands " +
                        "the link as version 1 of a new group via the foundation service " +
                        "(which publishes Link-Added), and replies with the created entity."
                },
                operation: LinkProcessingEventOperation.Adding,
                linkProcessingEventHandler: Scoped<ILinkProcessingService, Link>(
                        service => service.OnAddingLinkAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToLinkProcessingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.LinkProcessingOnModifyingLinkSubscriptionId,
                    Name = EventBrokerIdentifiers.LinkProcessingOnModifyingLinkSubscriptionName,

                    Description = "Handles modify requests: runs the contribution gate and the " +
                        "ownership/role permission rules, then modifies the link in place " +
                        "(which publishes Link-Modified) or forks a new version for an owner " +
                        "modify of a terminal row (which publishes Link-Added), and replies " +
                        "with the resulting entity."
                },
                operation: LinkProcessingEventOperation.Modifying,
                linkProcessingEventHandler: Scoped<ILinkProcessingService, Link>(
                        service => service.OnModifyingLinkAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToLinkProcessingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.LinkProcessingOnRemovingLinkByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.LinkProcessingOnRemovingLinkByIdSubscriptionName,

                    Description = "Handles remove requests: runs the contribution gate and the " +
                        "owner/Administrators permission rule, then soft deletes the link via the " +
                        "foundation service (which publishes Link-Removed), and replies with " +
                        "the removed entity; ApprovalStatus is left untouched."
                },
                operation: LinkProcessingEventOperation.RemovingById,
                linkProcessingEventHandler: Scoped<ILinkProcessingService, Link>(
                        service => service.OnRemovingLinkByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToLinkProcessingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.LinkProcessingOnRetrievingLinkByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.LinkProcessingOnRetrievingLinkByIdSubscriptionName,

                    Description = "Handles retrieve requests: applies the canonical content " +
                        "visibility rules — public versions reply for any caller, non-public " +
                        "versions only for the owner or a review role — and replies with the " +
                        "retrieved entity on the delivery; no completion fact is published."
                },
                operation: LinkProcessingEventOperation.RetrievingById,
                linkProcessingEventHandler: Scoped<ILinkProcessingService, Link>(
                        service => service.OnRetrievingLinkByIdAsync),
                cancellationToken: cancellationToken);

            // ── Reaction request handlers ───────────────────────────────────────
            await this.eventBroker.SubscribeToReactionEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ReactionOnAddingReactionSubscriptionId,
                    Name = EventBrokerIdentifiers.ReactionOnAddingReactionSubscriptionName,

                    Description = "Handles add requests: stores the reaction, publishes " +
                        "Reaction-Added, and replies with the added entity."
                },
                operation: ReactionEventOperation.Adding,
                reactionEventHandler: Scoped<IReactionService, Reaction>(
                        service => service.OnAddingReactionAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToReactionEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ReactionOnModifyingReactionSubscriptionId,
                    Name = EventBrokerIdentifiers.ReactionOnModifyingReactionSubscriptionName,

                    Description = "Handles modify requests: updates the reaction, publishes " +
                        "Reaction-Modified, and replies with the updated entity."
                },
                operation: ReactionEventOperation.Modifying,
                reactionEventHandler: Scoped<IReactionService, Reaction>(
                        service => service.OnModifyingReactionAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToReactionEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ReactionOnRemovingReactionByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.ReactionOnRemovingReactionByIdSubscriptionName,

                    Description = "Handles remove requests: soft-deletes the reaction, " +
                        "publishes Reaction-Removed, and replies with the removed entity."
                },
                operation: ReactionEventOperation.RemovingById,
                reactionEventHandler: Scoped<IReactionService, Reaction>(
                        service => service.OnRemovingReactionByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToReactionEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ReactionOnHardRemovingReactionByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.ReactionOnHardRemovingReactionByIdSubscriptionName,

                    Description = "Handles hard-remove requests: permanently deletes the " +
                        "reaction, publishes ReactionHardRemoved on the removal " +
                        "address, and replies with the deleted entity."
                },
                operation: ReactionEventOperation.HardRemovingById,
                reactionEventHandler: Scoped<IReactionService, Reaction>(
                        service => service.OnHardRemovingReactionByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToReactionEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ReactionOnRetrievingReactionByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.ReactionOnRetrievingReactionByIdSubscriptionName,

                    Description = "Handles retrieve requests: retrieves a reaction by id " +
                        "and replies with it on the delivery."
                },
                operation: ReactionEventOperation.RetrievingById,
                reactionEventHandler: Scoped<IReactionService, Reaction>(
                        service => service.OnRetrievingReactionByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToReactionEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ReactionOnSubmittingReactionSubscriptionId,
                    Name = EventBrokerIdentifiers.ReactionOnSubmittingReactionSubscriptionName,

                    Description = "Handles submit requests: moves a reaction Draft -> Submitted, " +
                        "publishes Reaction-Submitted, and replies with the updated entity."
                },
                operation: ReactionEventOperation.Submitting,
                reactionEventHandler: Scoped<IReactionService, Reaction>(
                        service => service.OnSubmittingReactionAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToReactionEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ReactionOnApprovingReactionSubscriptionId,
                    Name = EventBrokerIdentifiers.ReactionOnApprovingReactionSubscriptionName,

                    Description = "Handles approve requests: decides a submitted reaction, " +
                        "publishes Reaction-Approved or Reaction-Rejected per the decision, and replies " +
                        "with the updated entity."
                },
                operation: ReactionEventOperation.Approving,
                reactionEventHandler: Scoped<IReactionService, Reaction>(
                        service => service.OnApprovingReactionAsync),
                cancellationToken: cancellationToken);

            // ── Comment request handlers ─────────────────────────────────────────
            await this.eventBroker.SubscribeToCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.CommentOnAddingCommentSubscriptionId,
                    Name = EventBrokerIdentifiers.CommentOnAddingCommentSubscriptionName,

                    Description = "Handles add requests: stores the comment, publishes " +
                        "Comment-Added, and replies with the added entity."
                },
                operation: CommentEventOperation.Adding,
                commentEventHandler: Scoped<ICommentService, Comment>(
                        service => service.OnAddingCommentAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.CommentOnModifyingCommentSubscriptionId,
                    Name = EventBrokerIdentifiers.CommentOnModifyingCommentSubscriptionName,

                    Description = "Handles modify requests: updates the comment, publishes " +
                        "Comment-Modified, and replies with the updated entity."
                },
                operation: CommentEventOperation.Modifying,
                commentEventHandler: Scoped<ICommentService, Comment>(
                        service => service.OnModifyingCommentAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.CommentOnRemovingCommentByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.CommentOnRemovingCommentByIdSubscriptionName,

                    Description = "Handles remove requests: soft-deletes the comment, " +
                        "publishes Comment-Removed, and replies with the removed entity."
                },
                operation: CommentEventOperation.RemovingById,
                commentEventHandler: Scoped<ICommentService, Comment>(
                        service => service.OnRemovingCommentByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.CommentOnHardRemovingCommentByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.CommentOnHardRemovingCommentByIdSubscriptionName,

                    Description = "Handles hard-remove requests: permanently deletes the " +
                        "comment, publishes CommentHardRemoved on the removal " +
                        "address, and replies with the deleted entity."
                },
                operation: CommentEventOperation.HardRemovingById,
                commentEventHandler: Scoped<ICommentService, Comment>(
                        service => service.OnHardRemovingCommentByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.CommentOnRetrievingCommentByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.CommentOnRetrievingCommentByIdSubscriptionName,

                    Description = "Handles retrieve requests: retrieves a comment by id " +
                        "and replies with it on the delivery."
                },
                operation: CommentEventOperation.RetrievingById,
                commentEventHandler: Scoped<ICommentService, Comment>(
                        service => service.OnRetrievingCommentByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.CommentOnSubmittingCommentSubscriptionId,
                    Name = EventBrokerIdentifiers.CommentOnSubmittingCommentSubscriptionName,

                    Description = "Handles submit requests: moves a comment Draft -> Submitted, " +
                        "publishes Comment-Submitted, and replies with the updated entity."
                },
                operation: CommentEventOperation.Submitting,
                commentEventHandler: Scoped<ICommentService, Comment>(
                        service => service.OnSubmittingCommentAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.CommentOnApprovingCommentSubscriptionId,
                    Name = EventBrokerIdentifiers.CommentOnApprovingCommentSubscriptionName,

                    Description = "Handles approve requests: decides a submitted comment, " +
                        "publishes Comment-Approved or Comment-Rejected per the decision, and replies " +
                        "with the updated entity."
                },
                operation: CommentEventOperation.Approving,
                commentEventHandler: Scoped<ICommentService, Comment>(
                        service => service.OnApprovingCommentAsync),
                cancellationToken: cancellationToken);

            // ── ApprovalComment request handlers ──────────────────────────────────
            await this.eventBroker.SubscribeToApprovalCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalCommentOnAddingApprovalCommentSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalCommentOnAddingApprovalCommentSubscriptionName,

                    Description = "Handles add requests: stores the approval comment, publishes " +
                        "ApprovalComment-Added, and replies with the added entity."
                },
                operation: ApprovalCommentEventOperation.Adding,
                approvalCommentEventHandler: Scoped<IApprovalCommentService, ApprovalComment>(
                        service => service.OnAddingApprovalCommentAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalCommentOnModifyingApprovalCommentSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalCommentOnModifyingApprovalCommentSubscriptionName,

                    Description = "Handles modify requests: updates the approval comment, publishes " +
                        "ApprovalComment-Modified, and replies with the updated entity."
                },
                operation: ApprovalCommentEventOperation.Modifying,
                approvalCommentEventHandler: Scoped<IApprovalCommentService, ApprovalComment>(
                        service => service.OnModifyingApprovalCommentAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalCommentOnRemovingApprovalCommentByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalCommentOnRemovingApprovalCommentByIdSubscriptionName,

                    Description = "Handles remove requests: soft-deletes the approval comment, " +
                        "publishes ApprovalComment-Removed, and replies with the removed entity."
                },
                operation: ApprovalCommentEventOperation.RemovingById,
                approvalCommentEventHandler: Scoped<IApprovalCommentService, ApprovalComment>(
                        service => service.OnRemovingApprovalCommentByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalCommentOnHardRemovingApprovalCommentByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalCommentOnHardRemovingApprovalCommentByIdSubscriptionName,

                    Description = "Handles hard-remove requests: permanently deletes the " +
                        "approval comment, publishes ApprovalCommentHardRemoved on the removal " +
                        "address, and replies with the deleted entity."
                },
                operation: ApprovalCommentEventOperation.HardRemovingById,

                approvalCommentEventHandler:
                    Scoped<IApprovalCommentService, ApprovalComment>(
                        service => service.OnHardRemovingApprovalCommentByIdAsync),

                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalCommentOnRetrievingApprovalCommentByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalCommentOnRetrievingApprovalCommentByIdSubscriptionName,

                    Description = "Handles retrieve requests: retrieves an approval comment by id " +
                        "and replies with it on the delivery."
                },
                operation: ApprovalCommentEventOperation.RetrievingById,

                approvalCommentEventHandler:
                    Scoped<IApprovalCommentService, ApprovalComment>(
                        service => service.OnRetrievingApprovalCommentByIdAsync),

                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalCommentOnResolvingApprovalCommentSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalCommentOnResolvingApprovalCommentSubscriptionName,

                    Description = "Handles resolve requests: records whether a comment is " +
                        "settled or still outstanding, publishes ApprovalComment-Resolved, " +
                        "and replies with the updated entity."
                },
                operation: ApprovalCommentEventOperation.Resolving,
                approvalCommentEventHandler: Scoped<IApprovalCommentService, ApprovalComment>(
                        service => service.OnResolvingApprovalCommentAsync),
                cancellationToken: cancellationToken);

            // The review flow's trigger (§9.7.5). A recorded review may complete the round,
            // so the workflow evaluates it — or ends it outright where a standing rejection
            // blocks under BlockOnReject.
            await this.eventBroker.SubscribeToApprovalReviewEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnApprovalReviewAddedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnApprovalReviewAddedSubscriptionName,

                    Description = "Reacts to a recorded review: evaluates the round it "
                        + "belongs to, and ends it immediately where a standing "
                        + "rejection blocks."
                },
                operation: ApprovalReviewEventOperation.Added,
                approvalReviewEventHandler:
                    Scoped<IApprovalOrchestrationService, ApprovalReview>(
                        service => service.OnApprovalReviewAddedAsync),
                cancellationToken: cancellationToken);

            // The other seven workflow-record fact addresses (§10.17(a)). Every one of them can
            // move a §8.5 predicate, so every one has an ear. None of these handlers infers a
            // direction from the address it arrived on — each re-runs the whole evaluation and
            // lets the decision function say whether anything changed.

            await this.eventBroker.SubscribeToApprovalReviewEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnApprovalReviewModifiedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnApprovalReviewModifiedSubscriptionName,

                    Description = "Reacts to an amended verdict: re-tests the round the review belongs to."
                },
                operation: ApprovalReviewEventOperation.Modified,
                approvalReviewEventHandler:
                    Scoped<IApprovalOrchestrationService, ApprovalReview>(
                        service => service.OnApprovalReviewModifiedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalReviewEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnApprovalReviewRemovedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnApprovalReviewRemovedSubscriptionName,

                    Description = "Reacts to a withdrawn review: re-tests the round, whose count may drop "
                        + "or whose blocking rejection may lift. Serves the hard removal "
                        + "too, which shares this address."
                },
                operation: ApprovalReviewEventOperation.Removed,
                approvalReviewEventHandler:
                    Scoped<IApprovalOrchestrationService, ApprovalReview>(
                        service => service.OnApprovalReviewRemovedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalReviewEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnApprovalReviewDismissedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnApprovalReviewDismissedSubscriptionName,

                    Description = "Reacts to a dismissed verdict leaving the active set. Stands down "
                        + "while this service is itself dismissing that approval's stale "
                        + "reviews, so the round is evaluated once for that loop."
                },
                operation: ApprovalReviewEventOperation.Dismissed,
                approvalReviewEventHandler:
                    Scoped<IApprovalOrchestrationService, ApprovalReview>(
                        service => service.OnApprovalReviewDismissedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnApprovalCommentAddedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnApprovalCommentAddedSubscriptionName,

                    Description = "Reacts to a new comment: one born outstanding blocks an approval that "
                        + "was clear, and one born settled moves nothing — which the "
                        + "re-test establishes rather than assumes."
                },
                operation: ApprovalCommentEventOperation.Added,
                approvalCommentEventHandler:
                    Scoped<IApprovalOrchestrationService, ApprovalComment>(
                        service => service.OnApprovalCommentAddedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnApprovalCommentModifiedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnApprovalCommentModifiedSubscriptionName,

                    Description = "Reacts to an amended comment: the general modify is one of the two "
                        + "writers of IsResolved (§14.7 rule 5)."
                },
                operation: ApprovalCommentEventOperation.Modified,
                approvalCommentEventHandler:
                    Scoped<IApprovalOrchestrationService, ApprovalComment>(
                        service => service.OnApprovalCommentModifiedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnApprovalCommentResolvedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnApprovalCommentResolvedSubscriptionName,

                    Description = "Reacts to a settled comment: the resolve transition is the other of "
                        + "the two writers of IsResolved (§14.7 rule 5)."
                },
                operation: ApprovalCommentEventOperation.Resolved,
                approvalCommentEventHandler:
                    Scoped<IApprovalOrchestrationService, ApprovalComment>(
                        service => service.OnApprovalCommentResolvedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnApprovalCommentRemovedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnApprovalCommentRemovedSubscriptionName,

                    Description = "Reacts to a removed comment: soft-deleting an outstanding one "
                        + "unblocks the approval. Serves the hard removal too, which "
                        + "shares this address."
                },
                operation: ApprovalCommentEventOperation.Removed,
                approvalCommentEventHandler:
                    Scoped<IApprovalOrchestrationService, ApprovalComment>(
                        service => service.OnApprovalCommentRemovedAsync),
                cancellationToken: cancellationToken);

            // ── ApprovalReview request handlers ──────────────────────────────────
            await this.eventBroker.SubscribeToApprovalReviewEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalReviewOnAddingApprovalReviewSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalReviewOnAddingApprovalReviewSubscriptionName,

                    Description = "Handles add requests: stores the approval review, publishes " +
                        "ApprovalReview-Added, and replies with the added entity."
                },
                operation: ApprovalReviewEventOperation.Adding,
                approvalReviewEventHandler: Scoped<IApprovalReviewService, ApprovalReview>(
                        service => service.OnAddingApprovalReviewAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalReviewEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalReviewOnModifyingApprovalReviewSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalReviewOnModifyingApprovalReviewSubscriptionName,

                    Description = "Handles modify requests: updates the approval review, publishes " +
                        "ApprovalReview-Modified, and replies with the updated entity."
                },
                operation: ApprovalReviewEventOperation.Modifying,
                approvalReviewEventHandler: Scoped<IApprovalReviewService, ApprovalReview>(
                        service => service.OnModifyingApprovalReviewAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalReviewEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalReviewOnRemovingApprovalReviewByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalReviewOnRemovingApprovalReviewByIdSubscriptionName,

                    Description = "Handles remove requests: soft-deletes the approval review, " +
                        "publishes ApprovalReview-Removed, and replies with the removed entity."
                },
                operation: ApprovalReviewEventOperation.RemovingById,
                approvalReviewEventHandler: Scoped<IApprovalReviewService, ApprovalReview>(
                        service => service.OnRemovingApprovalReviewByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalReviewEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalReviewOnHardRemovingApprovalReviewByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalReviewOnHardRemovingApprovalReviewByIdSubscriptionName,

                    Description = "Handles hard-remove requests: permanently deletes the " +
                        "approval review, publishes ApprovalReviewHardRemoved on the removal " +
                        "address, and replies with the deleted entity."
                },
                operation: ApprovalReviewEventOperation.HardRemovingById,
                approvalReviewEventHandler: Scoped<IApprovalReviewService, ApprovalReview>(
                        service => service.OnHardRemovingApprovalReviewByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalReviewEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalReviewOnRetrievingApprovalReviewByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalReviewOnRetrievingApprovalReviewByIdSubscriptionName,

                    Description = "Handles retrieve requests: retrieves an approval review by id " +
                        "and replies with it on the delivery."
                },
                operation: ApprovalReviewEventOperation.RetrievingById,
                approvalReviewEventHandler: Scoped<IApprovalReviewService, ApprovalReview>(
                        service => service.OnRetrievingApprovalReviewByIdAsync),
                cancellationToken: cancellationToken);

            // ── ApprovalReviewRequest request handlers ───────────────────────────
            // No Modifying handler, and that is by design: an invitation has nothing amendable
            // (§7.9), so the address does not exist to subscribe to.
            await this.eventBroker.SubscribeToApprovalReviewRequestEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalReviewRequestOnAddingApprovalReviewRequestSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalReviewRequestOnAddingApprovalReviewRequestSubscriptionName,

                    Description = "Handles add requests: stores the approval review request, " +
                        "publishes ApprovalReviewRequest-Added, and replies with the added entity."
                },
                operation: ApprovalReviewRequestEventOperation.Adding,
                approvalReviewRequestEventHandler:
                    Scoped<IApprovalReviewRequestService, ApprovalReviewRequest>(
                        service => service.OnAddingApprovalReviewRequestAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalReviewRequestEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalReviewRequestOnRemovingApprovalReviewRequestByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalReviewRequestOnRemovingApprovalReviewRequestByIdSubscriptionName,

                    Description = "Handles withdraw requests: soft-deletes the approval review " +
                        "request, publishes ApprovalReviewRequest-Removed, and replies with the " +
                        "withdrawn entity."
                },
                operation: ApprovalReviewRequestEventOperation.RemovingById,
                approvalReviewRequestEventHandler:
                    Scoped<IApprovalReviewRequestService, ApprovalReviewRequest>(
                        service => service.OnRemovingApprovalReviewRequestByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalReviewRequestEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalReviewRequestOnHardRemovingApprovalReviewRequestByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalReviewRequestOnHardRemovingApprovalReviewRequestByIdSubscriptionName,

                    Description = "Handles hard-remove requests: permanently deletes the approval " +
                        "review request, publishes ApprovalReviewRequestHardRemoved on the removal " +
                        "address, and replies with the deleted entity."
                },
                operation: ApprovalReviewRequestEventOperation.HardRemovingById,
                approvalReviewRequestEventHandler:
                    Scoped<IApprovalReviewRequestService, ApprovalReviewRequest>(
                        service => service.OnHardRemovingApprovalReviewRequestByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalReviewRequestEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalReviewRequestOnRetrievingApprovalReviewRequestByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalReviewRequestOnRetrievingApprovalReviewRequestByIdSubscriptionName,

                    Description = "Handles retrieve requests: retrieves an approval review request " +
                        "by id and replies with it on the delivery."
                },
                operation: ApprovalReviewRequestEventOperation.RetrievingById,
                approvalReviewRequestEventHandler:
                    Scoped<IApprovalReviewRequestService, ApprovalReviewRequest>(
                        service => service.OnRetrievingApprovalReviewRequestByIdAsync),
                cancellationToken: cancellationToken);

            // ── ApprovalSetting request handlers ─────────────────────────────────
            await this.eventBroker.SubscribeToApprovalSettingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalSettingOnAddingApprovalSettingSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalSettingOnAddingApprovalSettingSubscriptionName,

                    Description = "Handles add requests: stores the approval setting, publishes " +
                        "ApprovalSetting-Added, and replies with the added entity."
                },
                operation: ApprovalSettingEventOperation.Adding,
                approvalSettingEventHandler: Scoped<IApprovalSettingService, ApprovalSetting>(
                        service => service.OnAddingApprovalSettingAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalSettingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalSettingOnModifyingApprovalSettingSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalSettingOnModifyingApprovalSettingSubscriptionName,

                    Description = "Handles modify requests: updates the approval setting, publishes " +
                        "ApprovalSetting-Modified, and replies with the updated entity."
                },
                operation: ApprovalSettingEventOperation.Modifying,
                approvalSettingEventHandler: Scoped<IApprovalSettingService, ApprovalSetting>(
                        service => service.OnModifyingApprovalSettingAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalSettingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalSettingOnRemovingApprovalSettingByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalSettingOnRemovingApprovalSettingByIdSubscriptionName,

                    Description = "Handles remove requests: soft-deletes the approval setting, " +
                        "publishes ApprovalSetting-Removed, and replies with the removed entity."
                },
                operation: ApprovalSettingEventOperation.RemovingById,
                approvalSettingEventHandler: Scoped<IApprovalSettingService, ApprovalSetting>(
                        service => service.OnRemovingApprovalSettingByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalSettingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalSettingOnHardRemovingApprovalSettingByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalSettingOnHardRemovingApprovalSettingByIdSubscriptionName,

                    Description = "Handles hard-remove requests: permanently deletes the " +
                        "approval setting, publishes ApprovalSettingHardRemoved on the removal " +
                        "address, and replies with the deleted entity."
                },
                operation: ApprovalSettingEventOperation.HardRemovingById,
                approvalSettingEventHandler: Scoped<IApprovalSettingService, ApprovalSetting>(
                        service => service.OnHardRemovingApprovalSettingByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalSettingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalSettingOnRetrievingApprovalSettingByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalSettingOnRetrievingApprovalSettingByIdSubscriptionName,

                    Description = "Handles retrieve requests: retrieves an approval setting by id " +
                        "and replies with it on the delivery."
                },
                operation: ApprovalSettingEventOperation.RetrievingById,
                approvalSettingEventHandler: Scoped<IApprovalSettingService, ApprovalSetting>(
                        service => service.OnRetrievingApprovalSettingByIdAsync),
                cancellationToken: cancellationToken);

            // ── Association request handlers ──────────────────────────
            await this.eventBroker.SubscribeToAssociationEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .AssociationOnAddingAssociationSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .AssociationOnAddingAssociationSubscriptionName,

                    Description = "Handles add requests: stores the content item association, " +
                        "publishes Association-Added, and replies with the added entity."
                },
                operation: AssociationEventOperation.Adding,
                associationEventHandler:
                    Scoped<IAssociationService, Association>(
                        service => service.OnAddingAssociationAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToAssociationEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .AssociationOnModifyingAssociationSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .AssociationOnModifyingAssociationSubscriptionName,

                    Description = "Handles modify requests: updates the content item association, " +
                        "publishes Association-Modified, and replies with the updated entity."
                },
                operation: AssociationEventOperation.Modifying,
                associationEventHandler:
                    Scoped<IAssociationService, Association>(
                        service => service.OnModifyingAssociationAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToAssociationEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .AssociationOnRemovingAssociationByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .AssociationOnRemovingAssociationByIdSubscriptionName,

                    Description = "Handles remove requests: soft-deletes the content item " +
                        "association, publishes Association-Removed, and replies " +
                        "with the removed entity."
                },
                operation: AssociationEventOperation.RemovingById,
                associationEventHandler:
                    Scoped<IAssociationService, Association>(
                        service => service.OnRemovingAssociationByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToAssociationEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .AssociationOnHardRemovingAssociationByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .AssociationOnHardRemovingAssociationByIdSubscriptionName,

                    Description = "Handles hard-remove requests: permanently deletes the " +
                        "content item association, publishes AssociationHardRemoved " +
                        "on the removal address, and replies with the deleted entity."
                },
                operation: AssociationEventOperation.HardRemovingById,
                associationEventHandler:
                    Scoped<IAssociationService, Association>(
                        service => service.OnHardRemovingAssociationByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToAssociationEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .AssociationOnRetrievingAssociationByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .AssociationOnRetrievingAssociationByIdSubscriptionName,

                    Description = "Handles retrieve requests: retrieves a content item " +
                        "association by id and replies with it on the delivery."
                },
                operation: AssociationEventOperation.RetrievingById,
                associationEventHandler:
                    Scoped<IAssociationService, Association>(
                        service => service.OnRetrievingAssociationByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToAssociationEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .AssociationOnApprovingAssociationSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .AssociationOnApprovingAssociationSubscriptionName,

                    Description = "Handles approve requests: applies the approval decision, publishes Association-Approved, and replies with the updated entity."
                },
                operation: AssociationEventOperation.Approving,
                associationEventHandler:
                    Scoped<IAssociationService, Association>(
                        service => service.OnApprovingAssociationAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToAssociationEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .AssociationOnSettingAssociationConfidenceSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .AssociationOnSettingAssociationConfidenceSubscriptionName,

                    Description = "Handles set-confidence requests: writes all four confidence fields as one unit, publishes Association-ConfidenceSet, and replies with the updated entity."
                },
                operation: AssociationEventOperation.SettingConfidence,
                associationEventHandler:
                    Scoped<IAssociationService, Association>(
                        service => service.OnSettingAssociationConfidenceAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToAssociationEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .AssociationOnSettingAssociationScopeSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .AssociationOnSettingAssociationScopeSubscriptionName,

                    Description = "Handles set-scope requests: changes endpoint scope after re-checking pair uniqueness, publishes Association-Scoped, and replies with the updated entity."
                },
                operation: AssociationEventOperation.SettingScope,
                associationEventHandler:
                    Scoped<IAssociationService, Association>(
                        service => service.OnSettingAssociationScopeAsync),
                cancellationToken: cancellationToken);

            // ── ContentItemSetting request handlers ──────────────────────────────
            await this.eventBroker.SubscribeToContentItemSettingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ContentItemSettingOnAddingContentItemSettingSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ContentItemSettingOnAddingContentItemSettingSubscriptionName,

                    Description = "Handles add requests: stores the content item setting, " +
                        "publishes ContentItemSetting-Added, and replies with the added entity."
                },
                operation: ContentItemSettingEventOperation.Adding,
                contentItemSettingEventHandler:
                    Scoped<IContentItemSettingService, ContentItemSetting>(
                        service => service.OnAddingContentItemSettingAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemSettingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ContentItemSettingOnModifyingContentItemSettingSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ContentItemSettingOnModifyingContentItemSettingSubscriptionName,

                    Description = "Handles modify requests: updates the content item setting, " +
                        "publishes ContentItemSetting-Modified, and replies with the updated entity."
                },
                operation: ContentItemSettingEventOperation.Modifying,
                contentItemSettingEventHandler:
                    Scoped<IContentItemSettingService, ContentItemSetting>(
                        service => service.OnModifyingContentItemSettingAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemSettingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ContentItemSettingOnRemovingContentItemSettingByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ContentItemSettingOnRemovingContentItemSettingByIdSubscriptionName,

                    Description = "Handles remove requests: soft-deletes the content item " +
                        "setting, publishes ContentItemSetting-Removed, and replies with the " +
                        "removed entity."
                },
                operation: ContentItemSettingEventOperation.RemovingById,
                contentItemSettingEventHandler:
                    Scoped<IContentItemSettingService, ContentItemSetting>(
                        service => service.OnRemovingContentItemSettingByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemSettingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ContentItemSettingOnHardRemovingContentItemSettingByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ContentItemSettingOnHardRemovingContentItemSettingByIdSubscriptionName,

                    Description = "Handles hard-remove requests: permanently deletes the " +
                        "content item setting, publishes ContentItemSettingHardRemoved on the " +
                        "removal address, and replies with the deleted entity."
                },
                operation: ContentItemSettingEventOperation.HardRemovingById,
                contentItemSettingEventHandler:
                    Scoped<IContentItemSettingService, ContentItemSetting>(
                        service => service.OnHardRemovingContentItemSettingByIdAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemSettingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ContentItemSettingOnRetrievingContentItemSettingByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ContentItemSettingOnRetrievingContentItemSettingByIdSubscriptionName,

                    Description = "Handles retrieve requests: retrieves a content item setting " +
                        "by id and replies with it on the delivery."
                },
                operation: ContentItemSettingEventOperation.RetrievingById,
                contentItemSettingEventHandler:
                    Scoped<IContentItemSettingService, ContentItemSetting>(
                        service => service.OnRetrievingContentItemSettingByIdAsync),
                cancellationToken: cancellationToken);

            // ── Approval workflow fact subscriptions ─────────────────────────────
            // Everything above is a request handler; these are the first FACT subscriptions in
            // the system, and the only ones that cross a service boundary — the address belongs
            // to whoever publishes it, the handler to the workflow that reacts.
            //
            // The tier each one binds to is the whole point (§10.17 rule 1). ContentItem and
            // Link complete their writes in a processing service, so their completion facts are
            // ContentItemProcessing-Added/-Modified and LinkProcessing-Added/-Modified; binding
            // to the foundation instead would react to the version fork's second row as if it
            // were a second amendment (§10.17 rule 2, §12.4.1 rules 6-7). The other five have
            // no layer above their foundation, so the foundation fact is their top-layer fact.
            //
            // -Removed is absent by design, not by omission: a takedown is not a moderation
            // step and must never re-open or re-evaluate an approval (§9.7.6).
            await this.eventBroker.SubscribeToContentItemProcessingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalOrchestrationOnContentItemAddedSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalOrchestrationOnContentItemAddedSubscriptionName,

                    Description = "Opens or reinstates the content item's approval when the " +
                        "processing service reports a completed add, and evaluates it if it " +
                        "is already submitted."
                },
                operation: ContentItemProcessingEventOperation.Added,
                contentItemProcessingEventHandler:
                    Scoped<IApprovalOrchestrationService, ContentItem>(
                        service => service.OnContentItemAddedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemProcessingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalOrchestrationOnContentItemModifiedSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalOrchestrationOnContentItemModifiedSubscriptionName,

                    Description = "Dismisses the content item's recorded reviews when the " +
                        "effective policy requires re-approval on change, then re-evaluates."
                },
                operation: ContentItemProcessingEventOperation.Modified,
                contentItemProcessingEventHandler:
                    Scoped<IApprovalOrchestrationService, ContentItem>(
                        service => service.OnContentItemModifiedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToLinkProcessingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalOrchestrationOnLinkAddedSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalOrchestrationOnLinkAddedSubscriptionName,

                    Description = "Opens or reinstates the link's approval when the processing " +
                        "service reports a completed add, and evaluates it if it is already " +
                        "submitted."
                },
                operation: LinkProcessingEventOperation.Added,
                linkProcessingEventHandler:
                    Scoped<IApprovalOrchestrationService, Link>(
                        service => service.OnLinkAddedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToLinkProcessingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalOrchestrationOnLinkModifiedSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalOrchestrationOnLinkModifiedSubscriptionName,

                    Description = "Dismisses the link's recorded reviews when the effective " +
                        "policy requires re-approval on change, then re-evaluates."
                },
                operation: LinkProcessingEventOperation.Modified,
                linkProcessingEventHandler:
                    Scoped<IApprovalOrchestrationService, Link>(
                        service => service.OnLinkModifiedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToTagEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalOrchestrationOnTagAddedSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalOrchestrationOnTagAddedSubscriptionName,

                    Description = "Opens or reinstates the tag's approval when one is added, " +
                        "and evaluates it if it is already submitted."
                },
                operation: TagEventOperation.Added,
                tagEventHandler: Scoped<IApprovalOrchestrationService, Tag>(
                        service => service.OnTagAddedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToTagEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalOrchestrationOnTagModifiedSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalOrchestrationOnTagModifiedSubscriptionName,

                    Description = "Dismisses the tag's recorded reviews when the effective " +
                        "policy requires re-approval on change, then re-evaluates."
                },
                operation: TagEventOperation.Modified,
                tagEventHandler: Scoped<IApprovalOrchestrationService, Tag>(
                        service => service.OnTagModifiedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalOrchestrationOnCommentAddedSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalOrchestrationOnCommentAddedSubscriptionName,

                    Description = "Opens or reinstates the comment's approval when one is " +
                        "added, and evaluates it if it is already submitted."
                },
                operation: CommentEventOperation.Added,
                commentEventHandler: Scoped<IApprovalOrchestrationService, Comment>(
                        service => service.OnCommentAddedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalOrchestrationOnCommentModifiedSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalOrchestrationOnCommentModifiedSubscriptionName,

                    Description = "Dismisses the comment's recorded reviews when the effective " +
                        "policy requires re-approval on change, then re-evaluates."
                },
                operation: CommentEventOperation.Modified,
                commentEventHandler: Scoped<IApprovalOrchestrationService, Comment>(
                        service => service.OnCommentModifiedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToReactionEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalOrchestrationOnReactionAddedSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalOrchestrationOnReactionAddedSubscriptionName,

                    Description = "Opens or reinstates the reaction's approval when one is " +
                        "added, and evaluates it if it is already submitted."
                },
                operation: ReactionEventOperation.Added,
                reactionEventHandler: Scoped<IApprovalOrchestrationService, Reaction>(
                        service => service.OnReactionAddedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToReactionEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalOrchestrationOnReactionModifiedSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalOrchestrationOnReactionModifiedSubscriptionName,

                    Description = "Dismisses the reaction's recorded reviews when the effective " +
                        "policy requires re-approval on change, then re-evaluates."
                },
                operation: ReactionEventOperation.Modified,
                reactionEventHandler: Scoped<IApprovalOrchestrationService, Reaction>(
                        service => service.OnReactionModifiedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToBibleReferenceEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalOrchestrationOnBibleReferenceAddedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnBibleReferenceAddedSubscriptionName,

                    Description = "Opens or reinstates the Bible reference's approval when one " +
                        "is added, and evaluates it if it is already submitted."
                },
                operation: BibleReferenceEventOperation.Added,
                bibleReferenceEventHandler:
                    Scoped<IApprovalOrchestrationService, BibleReference>(
                        service => service.OnBibleReferenceAddedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToBibleReferenceEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnBibleReferenceModifiedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnBibleReferenceModifiedSubscriptionName,

                    Description = "Dismisses the Bible reference's recorded reviews when the " +
                        "effective policy requires re-approval on change, then re-evaluates."
                },
                operation: BibleReferenceEventOperation.Modified,
                bibleReferenceEventHandler:
                    Scoped<IApprovalOrchestrationService, BibleReference>(
                        service => service.OnBibleReferenceModifiedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToAssociationEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalOrchestrationOnAssociationAddedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnAssociationAddedSubscriptionName,

                    Description = "Opens or reinstates the association's approval when one is " +
                        "added, and evaluates it if it is already submitted."
                },
                operation: AssociationEventOperation.Added,
                associationEventHandler:
                    Scoped<IApprovalOrchestrationService, Association>(
                        service => service.OnAssociationAddedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToAssociationEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnAssociationModifiedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnAssociationModifiedSubscriptionName,

                    Description = "Dismisses the association's recorded reviews when the " +
                        "effective policy requires re-approval on change, then re-evaluates."
                },
                operation: AssociationEventOperation.Modified,
                associationEventHandler:
                    Scoped<IApprovalOrchestrationService, Association>(
                        service => service.OnAssociationModifiedAsync),
                cancellationToken: cancellationToken);

            // -SUBMITTED, all seven on the FOUNDATION address. The submit verb is a foundation
            // transition on every entity, so unlike the Added/Modified pairs above there is no
            // processing tier to prefer for ContentItem and Link (§10.17 rule 1 picks the
            // top-layer fact, and here the foundation's is the only one).

            await this.eventBroker.SubscribeToTagEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnTagSubmittedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnTagSubmittedSubscriptionName,

                    Description = "Moves the tag's approval to Submitted when the submit " +
                        "verb moves the tag, then evaluates the round."
                },
                operation: TagEventOperation.Submitted,
                tagEventHandler:
                    Scoped<IApprovalOrchestrationService, Tag>(
                        service => service.OnTagSubmittedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnContentItemSubmittedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnContentItemSubmittedSubscriptionName,

                    Description = "Moves the content item's approval to Submitted when the submit " +
                        "verb moves the content item, then evaluates the round."
                },
                operation: ContentItemEventOperation.Submitted,
                contentItemEventHandler:
                    Scoped<IApprovalOrchestrationService, ContentItem>(
                        service => service.OnContentItemSubmittedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToLinkEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnLinkSubmittedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnLinkSubmittedSubscriptionName,

                    Description = "Moves the link's approval to Submitted when the submit " +
                        "verb moves the link, then evaluates the round."
                },
                operation: LinkEventOperation.Submitted,
                linkEventHandler:
                    Scoped<IApprovalOrchestrationService, Link>(
                        service => service.OnLinkSubmittedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnCommentSubmittedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnCommentSubmittedSubscriptionName,

                    Description = "Moves the comment's approval to Submitted when the submit " +
                        "verb moves the comment, then evaluates the round."
                },
                operation: CommentEventOperation.Submitted,
                commentEventHandler:
                    Scoped<IApprovalOrchestrationService, Comment>(
                        service => service.OnCommentSubmittedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToReactionEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnReactionSubmittedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnReactionSubmittedSubscriptionName,

                    Description = "Moves the reaction's approval to Submitted when the submit " +
                        "verb moves the reaction, then evaluates the round."
                },
                operation: ReactionEventOperation.Submitted,
                reactionEventHandler:
                    Scoped<IApprovalOrchestrationService, Reaction>(
                        service => service.OnReactionSubmittedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToBibleReferenceEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnBibleReferenceSubmittedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnBibleReferenceSubmittedSubscriptionName,

                    Description = "Moves the bible reference's approval to Submitted when the submit " +
                        "verb moves the bible reference, then evaluates the round."
                },
                operation: BibleReferenceEventOperation.Submitted,
                bibleReferenceEventHandler:
                    Scoped<IApprovalOrchestrationService, BibleReference>(
                        service => service.OnBibleReferenceSubmittedAsync),
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToAssociationEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnAssociationSubmittedSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalOrchestrationOnAssociationSubmittedSubscriptionName,

                    Description = "Moves the association's approval to Submitted when the submit " +
                        "verb moves the association, then evaluates the round."
                },
                operation: AssociationEventOperation.Submitted,
                associationEventHandler:
                    Scoped<IApprovalOrchestrationService, Association>(
                        service => service.OnAssociationSubmittedAsync),
                cancellationToken: cancellationToken);
        }
    }
}
