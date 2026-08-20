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
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Services.Foundations.ApprovalComments;
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
        private readonly IAssociationService associationService;
        private readonly IContentItemSettingService contentItemSettingService;
        private readonly IContentItemProcessingService contentItemProcessingService;
        private readonly ILinkProcessingService linkProcessingService;
        private readonly IApprovalOrchestrationService approvalOrchestrationService;

        public EventSubscriptionRegistration(
            IEventBroker eventBroker,
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
            IAssociationService associationService,
            IContentItemSettingService contentItemSettingService,
            IContentItemProcessingService contentItemProcessingService,
            ILinkProcessingService linkProcessingService,
            IApprovalOrchestrationService approvalOrchestrationService)
        {
            this.eventBroker = eventBroker;
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
            this.associationService = associationService;
            this.contentItemSettingService = contentItemSettingService;
            this.contentItemProcessingService = contentItemProcessingService;
            this.linkProcessingService = linkProcessingService;
            this.approvalOrchestrationService = approvalOrchestrationService;
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
                contentItemEventHandler: this.contentItemService.OnSubmittingContentItemAsync,
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
                contentItemEventHandler: this.contentItemService.OnApprovingContentItemAsync,
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
                contentItemProcessingEventHandler: this.contentItemProcessingService.OnApprovingContentItemAsync,
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
                contentItemProcessingEventHandler: this.contentItemProcessingService.OnAddingContentItemAsync,
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
                contentItemProcessingEventHandler: this.contentItemProcessingService.OnModifyingContentItemAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToContentItemProcessingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ContentItemProcessingOnRemovingContentItemByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.ContentItemProcessingOnRemovingContentItemByIdSubscriptionName,

                    Description = "Handles remove requests: runs the contribution gate and the " +
                        "owner/Admin permission rule, then soft deletes the content item via " +
                        "the foundation service (which publishes ContentItem-Removed), and " +
                        "replies with the removed entity; ApprovalStatus is left untouched."
                },
                operation: ContentItemProcessingEventOperation.RemovingById,
                contentItemProcessingEventHandler: this.contentItemProcessingService.OnRemovingContentItemByIdAsync,
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
                    this.contentItemProcessingService.OnRetrievingContentItemByIdAsync,

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

            await this.eventBroker.SubscribeToBibleReferenceEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.BibleReferenceOnSubmittingBibleReferenceSubscriptionId,
                    Name = EventBrokerIdentifiers.BibleReferenceOnSubmittingBibleReferenceSubscriptionName,

                    Description = "Handles submit requests: moves a bibleReference Draft -> Submitted, " +
                        "publishes BibleReference-Submitted, and replies with the updated entity."
                },
                operation: BibleReferenceEventOperation.Submitting,
                bibleReferenceEventHandler: this.bibleReferenceService.OnSubmittingBibleReferenceAsync,
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
                bibleReferenceEventHandler: this.bibleReferenceService.OnApprovingBibleReferenceAsync,
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

            await this.eventBroker.SubscribeToTagEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.TagOnSubmittingTagSubscriptionId,
                    Name = EventBrokerIdentifiers.TagOnSubmittingTagSubscriptionName,

                    Description = "Handles submit requests: moves a tag Draft -> Submitted, " +
                        "publishes Tag-Submitted, and replies with the updated entity."
                },
                operation: TagEventOperation.Submitting,
                tagEventHandler: this.tagService.OnSubmittingTagAsync,
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
                tagEventHandler: this.tagService.OnApprovingTagAsync,
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

            await this.eventBroker.SubscribeToLinkEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.LinkOnSubmittingLinkSubscriptionId,
                    Name = EventBrokerIdentifiers.LinkOnSubmittingLinkSubscriptionName,

                    Description = "Handles submit requests: moves a link Draft -> Submitted, " +
                        "publishes Link-Submitted, and replies with the updated entity."
                },
                operation: LinkEventOperation.Submitting,
                linkEventHandler: this.linkService.OnSubmittingLinkAsync,
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
                linkEventHandler: this.linkService.OnApprovingLinkAsync,
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
                linkProcessingEventHandler: this.linkProcessingService.OnApprovingLinkAsync,
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
                linkProcessingEventHandler: this.linkProcessingService.OnAddingLinkAsync,
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
                linkProcessingEventHandler: this.linkProcessingService.OnModifyingLinkAsync,
                cancellationToken: cancellationToken);

            await this.eventBroker.SubscribeToLinkProcessingEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.LinkProcessingOnRemovingLinkByIdSubscriptionId,
                    Name = EventBrokerIdentifiers.LinkProcessingOnRemovingLinkByIdSubscriptionName,

                    Description = "Handles remove requests: runs the contribution gate and the " +
                        "owner/Admin permission rule, then soft deletes the link via the " +
                        "foundation service (which publishes Link-Removed), and replies with " +
                        "the removed entity; ApprovalStatus is left untouched."
                },
                operation: LinkProcessingEventOperation.RemovingById,
                linkProcessingEventHandler: this.linkProcessingService.OnRemovingLinkByIdAsync,
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
                linkProcessingEventHandler: this.linkProcessingService.OnRetrievingLinkByIdAsync,
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

            await this.eventBroker.SubscribeToReactionEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ReactionOnSubmittingReactionSubscriptionId,
                    Name = EventBrokerIdentifiers.ReactionOnSubmittingReactionSubscriptionName,

                    Description = "Handles submit requests: moves a reaction Draft -> Submitted, " +
                        "publishes Reaction-Submitted, and replies with the updated entity."
                },
                operation: ReactionEventOperation.Submitting,
                reactionEventHandler: this.reactionService.OnSubmittingReactionAsync,
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
                reactionEventHandler: this.reactionService.OnApprovingReactionAsync,
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

            await this.eventBroker.SubscribeToCommentEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.CommentOnSubmittingCommentSubscriptionId,
                    Name = EventBrokerIdentifiers.CommentOnSubmittingCommentSubscriptionName,

                    Description = "Handles submit requests: moves a comment Draft -> Submitted, " +
                        "publishes Comment-Submitted, and replies with the updated entity."
                },
                operation: CommentEventOperation.Submitting,
                commentEventHandler: this.commentService.OnSubmittingCommentAsync,
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
                commentEventHandler: this.commentService.OnApprovingCommentAsync,
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
                approvalCommentEventHandler: this.approvalCommentService.OnResolvingApprovalCommentAsync,
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
                    this.approvalOrchestrationService.OnApprovalReviewAddedAsync,
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
                    this.approvalOrchestrationService.OnApprovalReviewModifiedAsync,
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
                    this.approvalOrchestrationService.OnApprovalReviewRemovedAsync,
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
                    this.approvalOrchestrationService.OnApprovalReviewDismissedAsync,
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
                    this.approvalOrchestrationService.OnApprovalCommentAddedAsync,
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
                    this.approvalOrchestrationService.OnApprovalCommentModifiedAsync,
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
                    this.approvalOrchestrationService.OnApprovalCommentResolvedAsync,
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
                    this.approvalOrchestrationService.OnApprovalCommentRemovedAsync,
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

            await this.eventBroker.SubscribeToApprovalReviewEventAsync(
                subscription: new EventSubscription
                {
                    Id = EventBrokerIdentifiers.ApprovalReviewOnDismissingApprovalReviewSubscriptionId,

                    Name = EventBrokerIdentifiers
                        .ApprovalReviewOnDismissingApprovalReviewSubscriptionName,

                    Description = "Handles dismiss requests: drives a review's StatusId to " +
                        "Dismissed, publishes ApprovalReview-Dismissed, and replies with the " +
                        "updated entity."
                },
                operation: ApprovalReviewEventOperation.Dismissing,
                approvalReviewEventHandler: this.approvalReviewService.OnDismissingApprovalReviewAsync,
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
                    this.associationService.OnAddingAssociationAsync,
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
                    this.associationService.OnModifyingAssociationAsync,
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
                    this.associationService.OnRemovingAssociationByIdAsync,
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
                    this.associationService.OnHardRemovingAssociationByIdAsync,
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
                    this.associationService.OnRetrievingAssociationByIdAsync,
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
                    this.associationService.OnApprovingAssociationAsync,
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
                    this.associationService.OnSettingAssociationConfidenceAsync,
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
                    this.associationService.OnSettingAssociationScopeAsync,
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
                    this.approvalOrchestrationService.OnContentItemAddedAsync,
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
                    this.approvalOrchestrationService.OnContentItemModifiedAsync,
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
                    this.approvalOrchestrationService.OnLinkAddedAsync,
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
                    this.approvalOrchestrationService.OnLinkModifiedAsync,
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
                tagEventHandler: this.approvalOrchestrationService.OnTagAddedAsync,
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
                tagEventHandler: this.approvalOrchestrationService.OnTagModifiedAsync,
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
                commentEventHandler: this.approvalOrchestrationService.OnCommentAddedAsync,
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
                commentEventHandler: this.approvalOrchestrationService.OnCommentModifiedAsync,
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
                reactionEventHandler: this.approvalOrchestrationService.OnReactionAddedAsync,
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
                reactionEventHandler: this.approvalOrchestrationService.OnReactionModifiedAsync,
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
                    this.approvalOrchestrationService.OnBibleReferenceAddedAsync,
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
                    this.approvalOrchestrationService.OnBibleReferenceModifiedAsync,
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
                    this.approvalOrchestrationService.OnAssociationAddedAsync,
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
                    this.approvalOrchestrationService.OnAssociationModifiedAsync,
                cancellationToken: cancellationToken);
        }
    }
}
