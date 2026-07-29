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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ContentItems;

namespace Glory2Him.Core.Services.Foundations.ContentItems
{
    /// <summary>
    /// Foundation service for content items. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain.
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

                return await this.storageBroker.SelectAllContentItemsAsync(cancellationToken);
            });

        public ValueTask<ContentItem> RetrieveContentItemByIdAsync(
            Guid contentItemId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveContentItemById(contentItemId);

                ContentItem maybeContentItem = await this.storageBroker.SelectContentItemByIdAsync(
                    contentItemId: contentItemId,
                    cancellationToken: cancellationToken);

                ValidateStorageContentItem(maybeContentItem, contentItemId);

                return maybeContentItem;
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

        private async ValueTask<ContentItem> DoAddContentItemAsync(
            ContentItem contentItem,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken)
        {
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
            contentItem = await this.securityAuditBroker.ApplyModifyAuditValuesAsync(
                entity: contentItem,
                securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyContentItem(contentItem, inboundEnvelope.SecurityContext);

            ContentItem maybeContentItem = await this.storageBroker.SelectContentItemByIdAsync(
                contentItemId: contentItem.Id,
                cancellationToken: cancellationToken);

            ValidateStorageContentItem(maybeContentItem, contentItem.Id);

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
            ValidateOnRemoveContentItemById(contentItemId);

            ContentItem maybeContentItem = await this.storageBroker.SelectContentItemByIdAsync(
                contentItemId: contentItemId,
                cancellationToken: cancellationToken);

            ValidateStorageContentItem(maybeContentItem, contentItemId);

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
