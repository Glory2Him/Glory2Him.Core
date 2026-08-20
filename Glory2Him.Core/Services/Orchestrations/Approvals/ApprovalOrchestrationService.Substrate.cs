// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Bases;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Orchestrations.Approvals;

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    internal partial class ApprovalOrchestrationService
    {
        // The workflow's ears. Each handler does one thing: name the entity type the fact came
        // from, and hand the row's identity to the flow that decides what it means.
        //
        // Typed per entity because the substrate is typed — an envelope carries one entity and
        // the address it arrived on is what says which. The EntityType is supplied HERE rather
        // than read off the payload, so a forged or mistyped body cannot make a Tag fact drive a
        // ContentItem's approval.
        //
        // -Added and -Modified only. Removal is not an approval state (§9.7.6): a takedown is not
        // a moderation step, and re-opening an approval because its subject was withdrawn is the
        // opposite of what should happen. The consequences of removal are handled where they
        // belong — the removing flow unpublishes, the queue filters, the transition refuses a
        // deleted subject.

        public ValueTask<EventEnvelope<Tag>?> OnTagAddedAsync(
            EventEnvelope<Tag> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Tag,
                eventName: "TagAdded",
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Tag>?> OnTagModifiedAsync(
            EventEnvelope<Tag> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Tag,
                eventName: "TagModified",
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        // The verified name is the name the PUBLISHER signed, and the publisher composes it as
        // entityName + operation where entityName belongs to the tier that owns the address this
        // subscription binds. ContentItem and Link take their top-layer fact from the PROCESSING
        // tier (§12.4.1 rules 6-7), and EventBroker.ContentItemProcessing/LinkProcessing sign
        // with "ContentItemProcessing"/"LinkProcessing" accordingly — so these four read
        // "...Processing..." while the five single-row entities below, whose fact comes from
        // their foundation, use the bare entity name.
        //
        // Getting this wrong is silent: the event name is bound INTO the HMAC, so a mismatch
        // does not misroute anything, it makes the receiver refuse a genuine envelope it was
        // correctly delivered.
        public ValueTask<EventEnvelope<ContentItem>?> OnContentItemAddedAsync(
            EventEnvelope<ContentItem> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.ContentItem,
                eventName: "ContentItemProcessingAdded",
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<ContentItem>?> OnContentItemModifiedAsync(
            EventEnvelope<ContentItem> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.ContentItem,
                eventName: "ContentItemProcessingModified",
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Link>?> OnLinkAddedAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Link,
                eventName: "LinkProcessingAdded",
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Link>?> OnLinkModifiedAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Link,
                eventName: "LinkProcessingModified",
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Comment>?> OnCommentAddedAsync(
            EventEnvelope<Comment> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Comment,
                eventName: "CommentAdded",
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Comment>?> OnCommentModifiedAsync(
            EventEnvelope<Comment> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Comment,
                eventName: "CommentModified",
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Reaction>?> OnReactionAddedAsync(
            EventEnvelope<Reaction> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Reaction,
                eventName: "ReactionAdded",
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Reaction>?> OnReactionModifiedAsync(
            EventEnvelope<Reaction> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Reaction,
                eventName: "ReactionModified",
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<BibleReference>?> OnBibleReferenceAddedAsync(
            EventEnvelope<BibleReference> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.BibleReference,
                eventName: "BibleReferenceAdded",
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<BibleReference>?> OnBibleReferenceModifiedAsync(
            EventEnvelope<BibleReference> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.BibleReference,
                eventName: "BibleReferenceModified",
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Association>?> OnAssociationAddedAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Association,
                eventName: "AssociationAdded",
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Association>?> OnAssociationModifiedAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Association,
                eventName: "AssociationModified",
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        // The review flow's ear. Keyed on the review's ApprovalId rather than an entity id:
        // a review names the round it belongs to directly, and the entity is whatever that
        // approval points at. Reaching for the entity here would be a second lookup for
        // something the flow resolves anyway.
        //
        // -Added only. A review is amended through its own verb and dismissed through
        // another; neither adds a verdict to the round, and re-evaluating on a dismissal
        // would run the round twice for one act — the flow that dismissed it already
        // re-evaluates (§9.7.4).
        public ValueTask<EventEnvelope<ApprovalReview>?> OnApprovalReviewAddedAsync(
            EventEnvelope<ApprovalReview> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToApprovalReviewFactAsync(envelope, cancellationToken);

        private async ValueTask<EventEnvelope<ApprovalReview>?> ReactToApprovalReviewFactAsync(
            EventEnvelope<ApprovalReview> envelope,
            CancellationToken cancellationToken)
        {
            await ValidateEntityFactEnvelopeAsync(envelope, "ApprovalReviewAdded");

            await ProcessApprovalReviewRecordedAsync(
                approvalId: envelope.Content.ApprovalId,
                cancellationToken: cancellationToken);

            return null;
        }

        // The shared body. A fact is a notification, so nothing is replied with: returning the
        // inbound envelope would put this service's name on a fact another service published.
        private async ValueTask<EventEnvelope<TEntity>?> ReactToEntityFactAsync<TEntity>(
            EventEnvelope<TEntity> envelope,
            EntityType entityType,
            string eventName,
            Func<EntityType, Guid, CancellationToken, ValueTask<ApprovalOutcome>> react,
            CancellationToken cancellationToken)
            where TEntity : IKey
        {
            await ValidateEntityFactEnvelopeAsync(envelope, eventName);

            await react(entityType, envelope.Content.Id, cancellationToken);

            return null;
        }
    }
}
