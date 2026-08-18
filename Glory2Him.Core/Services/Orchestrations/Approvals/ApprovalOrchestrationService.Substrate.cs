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
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Tag>?> OnTagModifiedAsync(
            EventEnvelope<Tag> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Tag,
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<ContentItem>?> OnContentItemAddedAsync(
            EventEnvelope<ContentItem> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.ContentItem,
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<ContentItem>?> OnContentItemModifiedAsync(
            EventEnvelope<ContentItem> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.ContentItem,
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Link>?> OnLinkAddedAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Link,
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Link>?> OnLinkModifiedAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Link,
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Comment>?> OnCommentAddedAsync(
            EventEnvelope<Comment> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Comment,
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Comment>?> OnCommentModifiedAsync(
            EventEnvelope<Comment> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Comment,
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Reaction>?> OnReactionAddedAsync(
            EventEnvelope<Reaction> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Reaction,
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Reaction>?> OnReactionModifiedAsync(
            EventEnvelope<Reaction> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Reaction,
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<BibleReference>?> OnBibleReferenceAddedAsync(
            EventEnvelope<BibleReference> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.BibleReference,
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<BibleReference>?> OnBibleReferenceModifiedAsync(
            EventEnvelope<BibleReference> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.BibleReference,
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Association>?> OnAssociationAddedAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Association,
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Association>?> OnAssociationModifiedAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Association,
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        // The shared body. A fact is a notification, so nothing is replied with: returning the
        // inbound envelope would put this service's name on a fact another service published.
        private async ValueTask<EventEnvelope<TEntity>?> ReactToEntityFactAsync<TEntity>(
            EventEnvelope<TEntity> envelope,
            EntityType entityType,
            Func<EntityType, Guid, CancellationToken, ValueTask<ApprovalOutcome>> react,
            CancellationToken cancellationToken)
            where TEntity : IKey
        {
            ValidateEntityFactEnvelope(envelope);

            await react(entityType, envelope.Content.Id, cancellationToken);

            return null;
        }
    }
}
