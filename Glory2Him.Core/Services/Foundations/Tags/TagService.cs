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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;

namespace Glory2Him.Core.Services.Foundations.Tags
{
    /// <summary>
    /// Foundation service for tags. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain. Per design §14.6 the foundation enforces security itself — the
    /// contribution gate on writes, owner-or-moderation-role write permission (removal by
    /// owner or Admin, hard removal by Admin only), and the §14.1/§14.5 read visibility
    /// posture — never assuming an upstream orchestration already gated the caller.
    /// </summary>
    internal partial class TagService : ITagService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly IAccessBroker accessBroker;
        private readonly IEnvelopeIntegrityBroker envelopeIntegrityBroker;
        private readonly ILoggingBroker loggingBroker;

        public TagService(
            IStorageBroker storageBroker,
            IDateTimeBroker dateTimeBroker,
            IIdentifierBroker identifierBroker,
            IEventBroker eventBroker,
            IEventEnvelopeBroker eventEnvelopeBroker,
            ISecurityAuditBroker securityAuditBroker,
            IAccessBroker accessBroker,
            IEnvelopeIntegrityBroker envelopeIntegrityBroker,
            ILoggingBroker loggingBroker)
        {
            this.storageBroker = storageBroker;
            this.dateTimeBroker = dateTimeBroker;
            this.identifierBroker = identifierBroker;
            this.eventBroker = eventBroker;
            this.eventEnvelopeBroker = eventEnvelopeBroker;
            this.securityAuditBroker = securityAuditBroker;
            this.accessBroker = accessBroker;
            this.envelopeIntegrityBroker = envelopeIntegrityBroker;
            this.loggingBroker = loggingBroker;
        }

        public ValueTask<Tag> AddTagAsync(
            Tag tag,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateTagIsNotNull(tag);

                EventEnvelope<Tag> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: tag);

                return await DoAddTagAsync(
                    tag: tag,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<Tag>> RetrieveAllTagsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the envelope exists to capture the ambient security context the
                // visibility filter runs against — the request payload is empty
                EventEnvelope<Tag> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new Tag());

                IQueryable<Tag> allTags =
                    await this.storageBroker.SelectAllTagsAsync(cancellationToken);

                return await ApplyCollectionReadVisibilityFilterAsync(
                    tags: allTags,
                    securityContext: envelope.SecurityContext);
            });

        public ValueTask<Tag> RetrieveTagByIdAsync(
            Guid tagId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new Tag
                {
                    Id = tagId
                };

                EventEnvelope<Tag> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveTagByIdAsync(
                    tagId: tagId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Tag> ModifyTagAsync(
            Tag tag,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateTagIsNotNull(tag);

                EventEnvelope<Tag> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: tag);

                return await DoModifyTagAsync(
                    tag: tag,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Tag> RemoveTagByIdAsync(
            Guid tagId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new Tag
                {
                    Id = tagId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<Tag> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: removeRequest);

                return await DoRemoveTagByIdAsync(
                    tagId: tagId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Tag> HardRemoveTagByIdAsync(
            Guid tagId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new Tag
                {
                    Id = tagId
                };

                EventEnvelope<Tag> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveTagByIdAsync(
                    tagId: tagId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        // the shared read posture of design §14.1/§14.5/§14.6: a publicly visible version
        // is readable by anyone; a non-public version answers not-found — never
        // unauthorized — to everyone but the owner and the review roles, with the true
        // denial reason logged server-side only
        private async ValueTask<Tag> DoRetrieveTagByIdAsync(
            Guid tagId,
            EventEnvelope<Tag> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRetrieveTagById(tagId);

            Tag maybeTag = await this.storageBroker.SelectTagByIdAsync(
                tagId: tagId,
                cancellationToken: cancellationToken);

            ValidateStorageTag(maybeTag, tagId);

            if (maybeTag.IsDeleted)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Tag read denied. Tag {tagId} is " +
                        "soft-deleted; reported to the caller as not found.");

                throw new NotFoundTagException(
                    message: $"Tag not found with id: {tagId}.");
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            bool isPubliclyVisible =
                maybeTag.ApprovalStatus == ApprovalStatus.Approved
                    && maybeTag.IsPublished
                    && (maybeTag.PublishDate is null
                        || maybeTag.PublishDate <= currentDateTime);

            if (isPubliclyVisible)
            {
                return maybeTag;
            }

            SecurityContext? securityContext = inboundEnvelope.SecurityContext;

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Tag read denied. Tag {tagId} is not " +
                        "publicly visible and the caller is not authenticated; reported to " +
                        "the caller as not found.");

                throw new NotFoundTagException(
                    message: $"Tag not found with id: {tagId}.");
            }

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && maybeTag.CreatedBy == actorUserId;

            if (isOwner is false && HasReviewRole(securityContext) is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Tag read denied. Tag {tagId} " +
                        $"is not publicly visible and user \"{actorUserId}\" is neither the " +
                        "owner nor in a review role; reported to the caller as not found.");

                throw new NotFoundTagException(
                    message: $"Tag not found with id: {tagId}.");
            }

            return maybeTag;
        }

        // the collection twin of the single-row posture: a row the caller may not see
        // drops out of the set instead of erroring, so a collection read never reveals
        // how many non-public rows exist
        private async ValueTask<IQueryable<Tag>> ApplyCollectionReadVisibilityFilterAsync(
            IQueryable<Tag> tags,
            SecurityContext? securityContext)
        {
            IQueryable<Tag> visibleTags = tags.Where(tag =>
                tag.IsDeleted == false);

            bool isAuthenticated =
                securityContext is not null && securityContext.IsAuthenticated;

            if (isAuthenticated && HasReviewRole(securityContext!))
            {
                return visibleTags;
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            string? actorUserId = isAuthenticated
                ? await this.securityAuditBroker.GetUserIdAsync(securityContext: securityContext!)
                : null;

            bool includeOwnTags = string.IsNullOrWhiteSpace(actorUserId) is false;

            return visibleTags.Where(tag =>
                (tag.ApprovalStatus == ApprovalStatus.Approved
                    && tag.IsPublished
                    && (tag.PublishDate == null
                        || tag.PublishDate <= currentDateTime))
                || (includeOwnTags && tag.CreatedBy == actorUserId));
        }

        private async ValueTask<Tag> DoAddTagAsync(
            Tag tag,
            EventEnvelope<Tag> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

            tag = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(entity: tag, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddTagAsync(
                tag: tag,
                securityContext: inboundEnvelope.SecurityContext);

            Tag addedTag =
                await this.storageBroker.InsertTagAsync(tag, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Tag> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedTag);

            await this.eventBroker.PublishTagAsync(
                envelope: outboundEnvelope,
                operation: TagEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,
                cancellationToken: cancellationToken);

            return addedTag;
        }

        private async ValueTask<Tag> DoModifyTagAsync(
            Tag tag,
            EventEnvelope<Tag> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

            tag = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: tag, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyTagAsync(
                tag: tag,
                securityContext: inboundEnvelope.SecurityContext);

            Tag maybeTag = await this.storageBroker.SelectTagByIdAsync(
                tagId: tag.Id,
                cancellationToken: cancellationToken);

            ValidateStorageTag(maybeTag, tagId: tag.Id);

            bool mayTransitionApprovalStatus =
                await ValidateUserCanModifyStorageTagAsync(
                    storageTag: maybeTag,
                    securityContext: inboundEnvelope.SecurityContext);

            // Checked AFTER write permission so the refusal cannot be used to read a row's
            // approval state without the standing to see it.
            ValidateStorageTagIsNotTerminal(maybeTag);

            tag = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: tag,
                    storageEntity: maybeTag);

            ValidateAgainstStorageTagOnModify(
                inputTag: tag,
                storageTag: maybeTag,
                mayTransitionApprovalStatus: mayTransitionApprovalStatus);

            Tag updatedTag =
                await this.storageBroker.UpdateTagAsync(tag, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.TagOnModifyingTagSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Tag> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedTag);

            await this.eventBroker.PublishTagAsync(
                envelope: outboundEnvelope,
                operation: TagEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.TagOnModifyingTagSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedTag;
        }

        private async ValueTask<Tag> DoRemoveTagByIdAsync(
            Guid tagId,
            string? deletionReason,
            EventEnvelope<Tag> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
            ValidateOnRemoveTagById(tagId, deletionReason);

            Tag maybeTag =
                await this.storageBroker.SelectTagByIdAsync(tagId, cancellationToken);

            ValidateStorageTag(maybeTag, tagId);

            // permission comes before the idempotent short-circuit, so an unauthorized
            // caller learns nothing about the row's deletion state
            await ValidateUserCanRemoveStorageTagAsync(
                storageTag: maybeTag,
                securityContext: inboundEnvelope.SecurityContext);

            if (maybeTag.IsDeleted)
                return maybeTag;

            Tag auditedTag =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeTag,
                    securityContext: inboundEnvelope.SecurityContext,
                    deletionReason: deletionReason);

            Tag removedTag = await this.storageBroker.UpdateTagAsync(
                tag: auditedTag,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.TagOnRemovingTagByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Tag> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedTag);

            await this.eventBroker.PublishTagAsync(
                envelope: outboundEnvelope,
                operation: TagEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.TagOnRemovingTagByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedTag;
        }

        private async ValueTask<Tag> DoHardRemoveTagByIdAsync(
            Guid tagId,
            EventEnvelope<Tag> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserCanHardRemoveTag(inboundEnvelope.SecurityContext);
            ValidateOnHardRemoveTagById(tagId);

            Tag maybeTag =
                await this.storageBroker.SelectTagByIdAsync(tagId, cancellationToken);

            ValidateStorageTag(maybeTag, tagId);

            Tag deletedTag =
                await this.storageBroker.DeleteTagAsync(maybeTag, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.TagOnHardRemovingTagByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Tag> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedTag);

            await this.eventBroker.PublishTagAsync(
                envelope: outboundEnvelope,
                operation: TagEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.TagOnHardRemovingTagByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedTag;
        }
    }
}
