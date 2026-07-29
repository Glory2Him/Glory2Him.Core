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
using Glory2Him.Core.Services.Foundations.Tags;
using Glory2Him.Core.Services.Foundations.Links;
using Glory2Him.Core.Services.Foundations.Reactions;
using Glory2Him.Core.Services.Foundations.Comments;
using Glory2Him.Core.Services.Foundations.ApprovalComments;
using Glory2Him.Core.Services.Foundations.ApprovalReviews;
using Glory2Him.Core.Services.Foundations.ApprovalSettings;
using Glory2Him.Core.Services.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Services.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Services.Foundations.ContentItemAssociations;
using Glory2Him.Core.Services.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Events.Orchestrations;
using Glory2Him.Core.Services.Orchestrations.ContentItems;

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
        private readonly ITagService tagService;
        private readonly ILinkService linkService;
        private readonly IReactionService reactionService;
        private readonly ICommentService commentService;
        private readonly IApprovalCommentService approvalCommentService;
        private readonly IApprovalReviewService approvalReviewService;
        private readonly IApprovalSettingService approvalSettingService;
        private readonly IApprovalSettingReviewerRoleService approvalSettingReviewerRoleService;
        private readonly IApprovalSettingPublisherRoleService approvalSettingPublisherRoleService;
        private readonly IContentItemAssociationService contentItemAssociationService;
        private readonly IContentItemSettingService contentItemSettingService;
        private readonly IContentItemOrchestrationService contentItemOrchestrationService;

        public EventSubscriptionRegistration(
            IEventBroker eventBroker,
            IContentTypeService contentTypeService,
            IContentItemService contentItemService,
            IApprovalService approvalService,
            IBibleReferenceService bibleReferenceService,
            ITagService tagService,
            ILinkService linkService,
            IReactionService reactionService,
            ICommentService commentService,
            IApprovalCommentService approvalCommentService,
            IApprovalReviewService approvalReviewService,
            IApprovalSettingService approvalSettingService,
            IApprovalSettingReviewerRoleService approvalSettingReviewerRoleService,
            IApprovalSettingPublisherRoleService approvalSettingPublisherRoleService,
            IContentItemAssociationService contentItemAssociationService,
            IContentItemSettingService contentItemSettingService,
            IContentItemOrchestrationService contentItemOrchestrationService)
        {
            this.eventBroker = eventBroker;
            this.contentTypeService = contentTypeService;
            this.contentItemService = contentItemService;
            this.approvalService = approvalService;
            this.bibleReferenceService = bibleReferenceService;
            this.tagService = tagService;
            this.linkService = linkService;
            this.reactionService = reactionService;
            this.commentService = commentService;
            this.approvalCommentService = approvalCommentService;
            this.approvalReviewService = approvalReviewService;
            this.approvalSettingService = approvalSettingService;
            this.approvalSettingReviewerRoleService = approvalSettingReviewerRoleService;
            this.approvalSettingPublisherRoleService = approvalSettingPublisherRoleService;
            this.contentItemAssociationService = contentItemAssociationService;
            this.contentItemSettingService = contentItemSettingService;
            this.contentItemOrchestrationService = contentItemOrchestrationService;
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

            // ── ContentItem orchestration request handlers ───────────────────────
            await this.eventBroker.SubscribeToContentItemSubmissionEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ContentItemOrchestrationOnSubmittingContentItemSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentItemOrchestrationOnSubmittingContentItemSubscriptionName,

                    Description = "Handles submit requests: runs the contribution gate and the " +
                        "duplicate-content rule, adds the content item via the foundation " +
                        "service (which publishes ContentItem-Added), and replies with the " +
                        "created entity; duplicate submissions fail as already existing."
                },
                operation: ContentItemSubmissionEventOperation.Submitting,
                contentItemSubmissionEventHandler: this.contentItemOrchestrationService.OnSubmittingContentItemAsync,
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
                tagEventHandler: this.tagService.OnAddingTagAsync,
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
                tagEventHandler: this.tagService.OnModifyingTagAsync,
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
                tagEventHandler: this.tagService.OnRemovingTagByIdAsync,
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
                tagEventHandler: this.tagService.OnHardRemovingTagByIdAsync,
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
                tagEventHandler: this.tagService.OnRetrievingTagByIdAsync,
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
                linkEventHandler: this.linkService.OnAddingLinkAsync,
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
                linkEventHandler: this.linkService.OnModifyingLinkAsync,
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
                linkEventHandler: this.linkService.OnRemovingLinkByIdAsync,
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
                linkEventHandler: this.linkService.OnHardRemovingLinkByIdAsync,
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
                linkEventHandler: this.linkService.OnRetrievingLinkByIdAsync,
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
                reactionEventHandler: this.reactionService.OnAddingReactionAsync,
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
                reactionEventHandler: this.reactionService.OnModifyingReactionAsync,
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
                reactionEventHandler: this.reactionService.OnRemovingReactionByIdAsync,
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
                reactionEventHandler: this.reactionService.OnHardRemovingReactionByIdAsync,
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
                reactionEventHandler: this.reactionService.OnRetrievingReactionByIdAsync,
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
                commentEventHandler: this.commentService.OnAddingCommentAsync,
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
                commentEventHandler: this.commentService.OnModifyingCommentAsync,
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
                commentEventHandler: this.commentService.OnRemovingCommentByIdAsync,
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
                commentEventHandler: this.commentService.OnHardRemovingCommentByIdAsync,
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
                commentEventHandler: this.commentService.OnRetrievingCommentByIdAsync,
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
                approvalCommentEventHandler: this.approvalCommentService.OnAddingApprovalCommentAsync,
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
                approvalCommentEventHandler: this.approvalCommentService.OnModifyingApprovalCommentAsync,
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
                approvalCommentEventHandler: this.approvalCommentService.OnRemovingApprovalCommentByIdAsync,
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
                    this.approvalCommentService.OnHardRemovingApprovalCommentByIdAsync,

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
                    this.approvalCommentService.OnRetrievingApprovalCommentByIdAsync,

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
                approvalReviewEventHandler: this.approvalReviewService.OnAddingApprovalReviewAsync,
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
                approvalReviewEventHandler: this.approvalReviewService.OnModifyingApprovalReviewAsync,
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
                approvalReviewEventHandler: this.approvalReviewService.OnRemovingApprovalReviewByIdAsync,
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
                approvalReviewEventHandler: this.approvalReviewService.OnHardRemovingApprovalReviewByIdAsync,
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
                approvalReviewEventHandler: this.approvalReviewService.OnRetrievingApprovalReviewByIdAsync,
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
                approvalSettingEventHandler: this.approvalSettingService.OnAddingApprovalSettingAsync,
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
                approvalSettingEventHandler: this.approvalSettingService.OnModifyingApprovalSettingAsync,
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
                approvalSettingEventHandler: this.approvalSettingService.OnRemovingApprovalSettingByIdAsync,
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
                approvalSettingEventHandler: this.approvalSettingService.OnHardRemovingApprovalSettingByIdAsync,
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
                approvalSettingEventHandler: this.approvalSettingService.OnRetrievingApprovalSettingByIdAsync,
                cancellationToken: cancellationToken);

            // ── ApprovalSettingReviewerRole request handlers ─────────────────────────────
            await this.eventBroker.SubscribeToApprovalSettingReviewerRoleEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnAddingApprovalSettingReviewerRoleSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnAddingApprovalSettingReviewerRoleSubscriptionName,

                    Description = "Handles add requests: stores the approval setting reviewer role, " +
                        "publishes ApprovalSettingReviewerRole-Added, and replies with the added entity."
                },
                operation: ApprovalSettingReviewerRoleEventOperation.Adding,
                approvalSettingReviewerRoleEventHandler: this.approvalSettingReviewerRoleService.OnAddingApprovalSettingReviewerRoleAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalSettingReviewerRoleEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnModifyingApprovalSettingReviewerRoleSubscriptionId,

                    Name =
                        EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnModifyingApprovalSettingReviewerRoleSubscriptionName,

                    Description = "Handles modify requests: updates the approval setting reviewer role, " +
                        "publishes ApprovalSettingReviewerRole-Modified, and replies with the updated entity."
                },
                operation: ApprovalSettingReviewerRoleEventOperation.Modifying,

                approvalSettingReviewerRoleEventHandler:
                    this.approvalSettingReviewerRoleService.OnModifyingApprovalSettingReviewerRoleAsync,

                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalSettingReviewerRoleEventAsync(
                subscription: new EventSubscription
                {
                    Id =
                        EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionId,

                    Name =
                        EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionName,

                    Description = "Handles remove requests: soft-deletes the approval setting " +
                        "role, publishes ApprovalSettingReviewerRole-Removed, and replies with the " +
                        "removed entity."
                },
                operation: ApprovalSettingReviewerRoleEventOperation.RemovingById,

                approvalSettingReviewerRoleEventHandler:
                    this.approvalSettingReviewerRoleService.OnRemovingApprovalSettingReviewerRoleByIdAsync,

                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalSettingReviewerRoleEventAsync(
                subscription: new EventSubscription
                {
                    Id =
                        EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnHardRemovingApprovalSettingReviewerRoleByIdSubscriptionId,

                    Name =
                        EventBrokerIdentifiers
                            .ApprovalSettingReviewerRoleOnHardRemovingApprovalSettingReviewerRoleByIdSubscriptionName,

                    Description = "Handles hard-remove requests: permanently deletes the " +
                        "approval setting reviewer role, publishes ApprovalSettingReviewerRoleHardRemoved on " +
                        "the removal address, and replies with the deleted entity."
                },
                operation: ApprovalSettingReviewerRoleEventOperation.HardRemovingById,

                approvalSettingReviewerRoleEventHandler:
                    this.approvalSettingReviewerRoleService.OnHardRemovingApprovalSettingReviewerRoleByIdAsync,

                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalSettingReviewerRoleEventAsync(
                subscription: new EventSubscription
                {
                    Id =
                        EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnRetrievingApprovalSettingReviewerRoleByIdSubscriptionId,

                    Name =
                        EventBrokerIdentifiers
                            .ApprovalSettingReviewerRoleOnRetrievingApprovalSettingReviewerRoleByIdSubscriptionName,

                    Description = "Handles retrieve requests: retrieves an approval setting " +
                        "role by id and replies with it on the delivery."
                },
                operation: ApprovalSettingReviewerRoleEventOperation.RetrievingById,

                approvalSettingReviewerRoleEventHandler:
                    this.approvalSettingReviewerRoleService.OnRetrievingApprovalSettingReviewerRoleByIdAsync,

                cancellationToken: cancellationToken);

            // ── ApprovalSettingPublisherRole request handlers ─────────────────────────────
            await this.eventBroker.SubscribeToApprovalSettingPublisherRoleEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnAddingApprovalSettingPublisherRoleSubscriptionId,
                    Name = EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnAddingApprovalSettingPublisherRoleSubscriptionName,

                    Description = "Handles add requests: stores the approval setting publisher role, " +
                        "publishes ApprovalSettingPublisherRole-Added, and replies with the added entity."
                },
                operation: ApprovalSettingPublisherRoleEventOperation.Adding,
                approvalSettingPublisherRoleEventHandler: this.approvalSettingPublisherRoleService.OnAddingApprovalSettingPublisherRoleAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalSettingPublisherRoleEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionId,

                    Name =
                        EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName,

                    Description = "Handles modify requests: updates the approval setting publisher role, " +
                        "publishes ApprovalSettingPublisherRole-Modified, and replies with the updated entity."
                },
                operation: ApprovalSettingPublisherRoleEventOperation.Modifying,

                approvalSettingPublisherRoleEventHandler:
                    this.approvalSettingPublisherRoleService.OnModifyingApprovalSettingPublisherRoleAsync,

                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalSettingPublisherRoleEventAsync(
                subscription: new EventSubscription
                {
                    Id =
                        EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnRemovingApprovalSettingPublisherRoleByIdSubscriptionId,

                    Name =
                        EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnRemovingApprovalSettingPublisherRoleByIdSubscriptionName,

                    Description = "Handles remove requests: soft-deletes the approval setting " +
                        "role, publishes ApprovalSettingPublisherRole-Removed, and replies with the " +
                        "removed entity."
                },
                operation: ApprovalSettingPublisherRoleEventOperation.RemovingById,

                approvalSettingPublisherRoleEventHandler:
                    this.approvalSettingPublisherRoleService.OnRemovingApprovalSettingPublisherRoleByIdAsync,

                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalSettingPublisherRoleEventAsync(
                subscription: new EventSubscription
                {
                    Id =
                        EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnHardRemovingApprovalSettingPublisherRoleByIdSubscriptionId,

                    Name =
                        EventBrokerIdentifiers
                            .ApprovalSettingPublisherRoleOnHardRemovingApprovalSettingPublisherRoleByIdSubscriptionName,

                    Description = "Handles hard-remove requests: permanently deletes the " +
                        "approval setting publisher role, publishes ApprovalSettingPublisherRoleHardRemoved on " +
                        "the removal address, and replies with the deleted entity."
                },
                operation: ApprovalSettingPublisherRoleEventOperation.HardRemovingById,

                approvalSettingPublisherRoleEventHandler:
                    this.approvalSettingPublisherRoleService.OnHardRemovingApprovalSettingPublisherRoleByIdAsync,

                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToApprovalSettingPublisherRoleEventAsync(
                subscription: new EventSubscription
                {
                    Id =
                        EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnRetrievingApprovalSettingPublisherRoleByIdSubscriptionId,

                    Name =
                        EventBrokerIdentifiers
                            .ApprovalSettingPublisherRoleOnRetrievingApprovalSettingPublisherRoleByIdSubscriptionName,

                    Description = "Handles retrieve requests: retrieves an approval setting " +
                        "role by id and replies with it on the delivery."
                },
                operation: ApprovalSettingPublisherRoleEventOperation.RetrievingById,

                approvalSettingPublisherRoleEventHandler:
                    this.approvalSettingPublisherRoleService.OnRetrievingApprovalSettingPublisherRoleByIdAsync,

                cancellationToken: cancellationToken);

            // ── ContentItemAssociation request handlers ──────────────────────────
            await this.eventBroker.SubscribeToContentItemAssociationEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ContentItemAssociationOnAddingContentItemAssociationSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ContentItemAssociationOnAddingContentItemAssociationSubscriptionName,

                    Description = "Handles add requests: stores the content item association, " +
                        "publishes ContentItemAssociation-Added, and replies with the added entity."
                },
                operation: ContentItemAssociationEventOperation.Adding,
                contentItemAssociationEventHandler:
                    this.contentItemAssociationService.OnAddingContentItemAssociationAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemAssociationEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ContentItemAssociationOnModifyingContentItemAssociationSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ContentItemAssociationOnModifyingContentItemAssociationSubscriptionName,

                    Description = "Handles modify requests: updates the content item association, " +
                        "publishes ContentItemAssociation-Modified, and replies with the updated entity."
                },
                operation: ContentItemAssociationEventOperation.Modifying,
                contentItemAssociationEventHandler:
                    this.contentItemAssociationService.OnModifyingContentItemAssociationAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemAssociationEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ContentItemAssociationOnRemovingContentItemAssociationByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ContentItemAssociationOnRemovingContentItemAssociationByIdSubscriptionName,

                    Description = "Handles remove requests: soft-deletes the content item " +
                        "association, publishes ContentItemAssociation-Removed, and replies " +
                        "with the removed entity."
                },
                operation: ContentItemAssociationEventOperation.RemovingById,
                contentItemAssociationEventHandler:
                    this.contentItemAssociationService.OnRemovingContentItemAssociationByIdAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemAssociationEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ContentItemAssociationOnHardRemovingContentItemAssociationByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ContentItemAssociationOnHardRemovingContentItemAssociationByIdSubscriptionName,

                    Description = "Handles hard-remove requests: permanently deletes the " +
                        "content item association, publishes ContentItemAssociationHardRemoved " +
                        "on the removal address, and replies with the deleted entity."
                },
                operation: ContentItemAssociationEventOperation.HardRemovingById,
                contentItemAssociationEventHandler:
                    this.contentItemAssociationService.OnHardRemovingContentItemAssociationByIdAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemAssociationEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers
                        .ContentItemAssociationOnRetrievingContentItemAssociationByIdSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ContentItemAssociationOnRetrievingContentItemAssociationByIdSubscriptionName,

                    Description = "Handles retrieve requests: retrieves a content item " +
                        "association by id and replies with it on the delivery."
                },
                operation: ContentItemAssociationEventOperation.RetrievingById,
                contentItemAssociationEventHandler:
                    this.contentItemAssociationService.OnRetrievingContentItemAssociationByIdAsync,
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
                    this.contentItemSettingService.OnAddingContentItemSettingAsync,
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
                    this.contentItemSettingService.OnModifyingContentItemSettingAsync,
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
                    this.contentItemSettingService.OnRemovingContentItemSettingByIdAsync,
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
                    this.contentItemSettingService.OnHardRemovingContentItemSettingByIdAsync,
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
                    this.contentItemSettingService.OnRetrievingContentItemSettingByIdAsync,
                cancellationToken: cancellationToken);
        }
    }
}
