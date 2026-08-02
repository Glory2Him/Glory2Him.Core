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
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ContentItems
{
    /// <summary>
    /// Foundation service for content items. Every operation is both callable directly (the
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
    internal partial class ContentItemService : IContentItemService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public ContentItemService(
            IStorageBroker storageBroker,
            IDateTimeBroker dateTimeBroker,
            IIdentifierBroker identifierBroker,
            IEventBroker eventBroker,
            IEventEnvelopeBroker eventEnvelopeBroker,
            ISecurityAuditBroker securityAuditBroker,
            ILoggingBroker loggingBroker)
        {
            this.storageBroker = storageBroker;
            this.dateTimeBroker = dateTimeBroker;
            this.identifierBroker = identifierBroker;
            this.eventBroker = eventBroker;
            this.eventEnvelopeBroker = eventEnvelopeBroker;
            this.securityAuditBroker = securityAuditBroker;
            this.loggingBroker = loggingBroker;
        }

        public ValueTask<ContentItem> AddContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateContentItemIsNotNull(contentItem);

                EventEnvelope<ContentItem> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: contentItem);

                return await DoAddContentItemAsync(
                    contentItem: contentItem,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<ContentItem>> RetrieveAllContentItemsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the envelope exists to capture the ambient security context the
                // visibility filter runs against — the request payload is empty
                EventEnvelope<ContentItem> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new ContentItem());

                IQueryable<ContentItem> allContentItems =
                    await this.storageBroker.SelectAllContentItemsAsync(cancellationToken);

                return await ApplyCollectionReadVisibilityFilterAsync(
                    contentItems: allContentItems,
                    securityContext: envelope.SecurityContext);
            });

        public ValueTask<ContentItem> RetrieveContentItemByIdAsync(
            Guid contentItemId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new ContentItem
                {
                    Id = contentItemId
                };

                EventEnvelope<ContentItem> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveContentItemByIdAsync(
                    contentItemId: contentItemId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ContentItem> ModifyContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateContentItemIsNotNull(contentItem);

                EventEnvelope<ContentItem> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: contentItem);

                return await DoModifyContentItemAsync(
                    contentItem: contentItem,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ContentItem> RemoveContentItemByIdAsync(
            Guid contentItemId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the request payload is the remove instruction: the id and optional reason
                var removeRequest = new ContentItem
                {
                    Id = contentItemId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<ContentItem> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: removeRequest);

                return await DoRemoveContentItemByIdAsync(
                    contentItemId: contentItemId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ContentItem> HardRemoveContentItemByIdAsync(
            Guid contentItemId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new ContentItem
                {
                    Id = contentItemId
                };

                EventEnvelope<ContentItem> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveContentItemByIdAsync(
                    contentItemId: contentItemId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        // the shared read posture of design §14.1/§14.5/§14.6: a publicly visible version
        // is readable by anyone; a non-public version answers not-found — never
        // unauthorized — to everyone but the owner and the review roles, with the true
        // denial reason logged server-side only
        private async ValueTask<ContentItem> DoRetrieveContentItemByIdAsync(
            Guid contentItemId,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRetrieveContentItemById(contentItemId);

            ContentItem maybeContentItem = await this.storageBroker.SelectContentItemByIdAsync(
                contentItemId: contentItemId,
                cancellationToken: cancellationToken);

            ValidateStorageContentItem(maybeContentItem, contentItemId);

            if (maybeContentItem.IsDeleted)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Content item read denied. Content item {contentItemId} is " +
                        "soft-deleted; reported to the caller as not found.");

                throw new NotFoundContentItemException(
                    message: $"Content item not found with id: {contentItemId}.");
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            bool isPubliclyVisible =
                maybeContentItem.ApprovalStatus == ApprovalStatus.Approved
                    && maybeContentItem.IsPublished
                    && (maybeContentItem.PublishDate is null
                        || maybeContentItem.PublishDate <= currentDateTime);

            if (isPubliclyVisible)
            {
                return maybeContentItem;
            }

            SecurityContext? securityContext = inboundEnvelope.SecurityContext;

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Content item read denied. Content item {contentItemId} is not " +
                        "publicly visible and the caller is not authenticated; reported to " +
                        "the caller as not found.");

                throw new NotFoundContentItemException(
                    message: $"Content item not found with id: {contentItemId}.");
            }

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && maybeContentItem.CreatedBy == actorUserId;

            if (isOwner is false && HasReviewRole(securityContext) is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Content item read denied. Content item {contentItemId} " +
                        $"is not publicly visible and user \"{actorUserId}\" is neither the " +
                        "owner nor in a review role; reported to the caller as not found.");

                throw new NotFoundContentItemException(
                    message: $"Content item not found with id: {contentItemId}.");
            }

            return maybeContentItem;
        }

        // the collection twin of the single-row posture: a row the caller may not see
        // drops out of the set instead of erroring, so a collection read never reveals
        // how many non-public rows exist
        private async ValueTask<IQueryable<ContentItem>> ApplyCollectionReadVisibilityFilterAsync(
            IQueryable<ContentItem> contentItems,
            SecurityContext? securityContext)
        {
            IQueryable<ContentItem> visibleContentItems = contentItems.Where(contentItem =>
                contentItem.IsDeleted == false);

            bool isAuthenticated =
                securityContext is not null && securityContext.IsAuthenticated;

            if (isAuthenticated && HasReviewRole(securityContext!))
            {
                return visibleContentItems;
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            string? actorUserId = isAuthenticated
                ? await this.securityAuditBroker.GetUserIdAsync(securityContext: securityContext!)
                : null;

            bool includeOwnContentItems = string.IsNullOrWhiteSpace(actorUserId) is false;

            return visibleContentItems.Where(contentItem =>
                (contentItem.ApprovalStatus == ApprovalStatus.Approved
                    && contentItem.IsPublished
                    && (contentItem.PublishDate == null
                        || contentItem.PublishDate <= currentDateTime))
                || (includeOwnContentItems && contentItem.CreatedBy == actorUserId));
        }

        private async ValueTask<ContentItem> DoAddContentItemAsync(
            ContentItem contentItem,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

            contentItem = await this.securityAuditBroker.ApplyAddAuditValuesAsync(
                entity: contentItem,
                securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddContentItem(contentItem, inboundEnvelope.SecurityContext);

            ContentItem addedContentItem = await this.storageBroker.InsertContentItemAsync(
                contentItem: contentItem,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentItemOnAddingContentItemSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ContentItem> outboundEnvelope = await this.eventEnvelopeBroker.CreateNextAsync(
                sourceEnvelope: inboundEnvelope,
                content: addedContentItem);

            await this.eventBroker.PublishContentItemAsync(
                envelope: outboundEnvelope,
                operation: ContentItemEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentItemOnAddingContentItemSubscriptionName,
                cancellationToken: cancellationToken);

            return addedContentItem;
        }

        private async ValueTask<ContentItem> DoModifyContentItemAsync(
            ContentItem contentItem,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

            contentItem = await this.securityAuditBroker.ApplyModifyAuditValuesAsync(
                entity: contentItem,
                securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyContentItem(contentItem, inboundEnvelope.SecurityContext);

            ContentItem maybeContentItem = await this.storageBroker.SelectContentItemByIdAsync(
                contentItemId: contentItem.Id,
                cancellationToken: cancellationToken);

            ValidateStorageContentItem(maybeContentItem, contentItem.Id);

            await ValidateUserCanModifyStorageContentItemAsync(
                storageContentItem: maybeContentItem,
                securityContext: inboundEnvelope.SecurityContext);

            contentItem = await this.securityAuditBroker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                entity: contentItem,
                storageEntity: maybeContentItem);

            ValidateAgainstStorageContentItemOnModify(
                inputContentItem: contentItem,
                storageContentItem: maybeContentItem);

            ContentItem updatedContentItem = await this.storageBroker.UpdateContentItemAsync(
                contentItem: contentItem,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentItemOnModifyingContentItemSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ContentItem> outboundEnvelope = await this.eventEnvelopeBroker.CreateNextAsync(
                sourceEnvelope: inboundEnvelope,
                content: updatedContentItem);

            await this.eventBroker.PublishContentItemAsync(
                envelope: outboundEnvelope,
                operation: ContentItemEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentItemOnModifyingContentItemSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedContentItem;
        }

        private async ValueTask<ContentItem> DoRemoveContentItemByIdAsync(
            Guid contentItemId,
            string? deletionReason,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
            ValidateOnRemoveContentItemById(contentItemId);

            ContentItem maybeContentItem = await this.storageBroker.SelectContentItemByIdAsync(
                contentItemId: contentItemId,
                cancellationToken: cancellationToken);

            ValidateStorageContentItem(maybeContentItem, contentItemId);

            // permission comes before the idempotent short-circuit, so an unauthorized
            // caller learns nothing about the row's deletion state
            await ValidateUserCanRemoveStorageContentItemAsync(
                storageContentItem: maybeContentItem,
                securityContext: inboundEnvelope.SecurityContext);

            if (maybeContentItem.IsDeleted)
                return maybeContentItem;

            maybeContentItem = await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                entity: maybeContentItem,
                securityContext: inboundEnvelope.SecurityContext);

            maybeContentItem.IsDeleted = true;
            maybeContentItem.DeletionReason = deletionReason;

            ContentItem deletedContentItem = await this.storageBroker.UpdateContentItemAsync(
                contentItem: maybeContentItem,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentItemOnRemovingContentItemByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ContentItem> outboundEnvelope = await this.eventEnvelopeBroker.CreateNextAsync(
                sourceEnvelope: inboundEnvelope,
                content: deletedContentItem);

            await this.eventBroker.PublishContentItemAsync(
                envelope: outboundEnvelope,
                operation: ContentItemEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentItemOnRemovingContentItemByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedContentItem;
        }

        private async ValueTask<ContentItem> DoHardRemoveContentItemByIdAsync(
            Guid contentItemId,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserCanHardRemoveContentItem(inboundEnvelope.SecurityContext);
            ValidateOnHardRemoveContentItemById(contentItemId);

            ContentItem maybeContentItem = await this.storageBroker.SelectContentItemByIdAsync(
                contentItemId: contentItemId,
                cancellationToken: cancellationToken);

            ValidateStorageContentItem(maybeContentItem, contentItemId);

            ContentItem deletedContentItem = await this.storageBroker.DeleteContentItemAsync(
                contentItem: maybeContentItem,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentItemOnHardRemovingContentItemByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ContentItem> outboundEnvelope = await this.eventEnvelopeBroker.CreateNextAsync(
                sourceEnvelope: inboundEnvelope,
                content: deletedContentItem);

            await this.eventBroker.PublishContentItemAsync(
                envelope: outboundEnvelope,
                operation: ContentItemEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentItemOnHardRemovingContentItemByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedContentItem;
        }
    }
}
