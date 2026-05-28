// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems;

namespace Glory2Him.Core.Services.Foundations.ContentItems
{
    public partial class ContentItemService : IContentItemService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IEventBroker eventBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public ContentItemService(
            IStorageBroker storageBroker,
            IDateTimeBroker dateTimeBroker,
            IEventBroker eventBroker,
            ISecurityAuditBroker securityAuditBroker,
            ILoggingBroker loggingBroker)
        {
            this.storageBroker = storageBroker;
            this.dateTimeBroker = dateTimeBroker;
            this.eventBroker = eventBroker;
            this.securityAuditBroker = securityAuditBroker;
            this.loggingBroker = loggingBroker;
        }

        public ValueTask<ContentItem> AddContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnAddContentItem(contentItem);
                contentItem = await this.securityAuditBroker.ApplyAddAuditValuesAsync(contentItem);

                ContentItem addedContentItem =
                    await this.storageBroker.InsertContentItemAsync(contentItem, cancellationToken);

                var envelope = new EventEnvelope<ContentItem> { Content = addedContentItem };
                await this.eventBroker.PublishContentItemAsync(envelope, "ContentItemAdded");

                return addedContentItem;
            });

        public ValueTask<IQueryable<ContentItem>> RetrieveAllContentItemsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return await this.storageBroker.SelectAllContentItemsAsync();
            });

        public ValueTask<ContentItem> RetrieveContentItemByIdAsync(
            Guid contentItemId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveContentItemById(contentItemId);

                return await this.storageBroker.SelectContentItemByIdAsync(
                    contentItemId,
                    cancellationToken);
            });

        public ValueTask<ContentItem> ModifyContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnModifyContentItem(contentItem);
                contentItem = await this.securityAuditBroker.ApplyModifyAuditValuesAsync(contentItem);

                ContentItem updatedContentItem =
                    await this.storageBroker.UpdateContentItemAsync(contentItem, cancellationToken);

                var envelope = new EventEnvelope<ContentItem> { Content = updatedContentItem };
                await this.eventBroker.PublishContentItemAsync(envelope, "ContentItemModified");

                return updatedContentItem;
            });

        public ValueTask<ContentItem> RemoveContentItemByIdAsync(
            Guid contentItemId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRemoveContentItemById(contentItemId);

                ContentItem maybeContentItem =
                    await this.storageBroker.SelectContentItemByIdAsync(contentItemId, cancellationToken);

                ValidateStorageContentItem(maybeContentItem, contentItemId);

                ContentItem deletedContentItem =
                    await this.storageBroker.DeleteContentItemAsync(maybeContentItem, cancellationToken);

                var envelope = new EventEnvelope<ContentItem> { Content = deletedContentItem };
                await this.eventBroker.PublishContentItemAsync(envelope, "ContentItemRemoved");

                return deletedContentItem;
            });
    }
}
