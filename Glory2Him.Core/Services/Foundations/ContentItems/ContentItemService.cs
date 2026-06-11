// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
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
                contentItem = await this.securityAuditBroker.ApplyAddAuditValuesAsync(contentItem);
                await ValidateOnAddContentItem(contentItem);

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

                ContentItem maybeContentItem =
                    await this.storageBroker.SelectContentItemByIdAsync(
                        contentItemId,
                        cancellationToken);

                ValidateStorageContentItem(maybeContentItem, contentItemId);

                return maybeContentItem;
            });

        public ValueTask<ContentItem> ModifyContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                contentItem = await this.securityAuditBroker.ApplyModifyAuditValuesAsync(contentItem);
                await ValidateOnModifyContentItem(contentItem);

                ContentItem maybeContentItem =
                    await this.storageBroker.SelectContentItemByIdAsync(contentItem.Id, cancellationToken);

                ValidateStorageContentItem(maybeContentItem, contentItem.Id);

                contentItem = await this.securityAuditBroker
                    .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(contentItem, maybeContentItem);

                ValidateAgainstStorageContentItemOnModify(
                    inputContentItem: contentItem,
                    storageContentItem: maybeContentItem);

                ContentItem updatedContentItem =
                    await this.storageBroker.UpdateContentItemAsync(contentItem, cancellationToken);

                var envelope = new EventEnvelope<ContentItem> { Content = updatedContentItem };
                await this.eventBroker.PublishContentItemAsync(envelope, "ContentItemModified");

                return updatedContentItem;
            });

        public ValueTask<ContentItem> RemoveContentItemByIdAsync(
            Guid contentItemId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRemoveContentItemById(contentItemId);

                ContentItem maybeContentItem =
                    await this.storageBroker.SelectContentItemByIdAsync(contentItemId, cancellationToken);

                ValidateStorageContentItem(maybeContentItem, contentItemId);

                if (maybeContentItem.IsDeleted)
                    return maybeContentItem;

                maybeContentItem = await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(maybeContentItem);
                maybeContentItem.IsDeleted = true;
                maybeContentItem.DeletionReason = deletionReason;

                ContentItem deletedContentItem =
                    await this.storageBroker.UpdateContentItemAsync(maybeContentItem, cancellationToken);

                var envelope = new EventEnvelope<ContentItem> { Content = deletedContentItem };
                await this.eventBroker.PublishContentItemAsync(envelope, "ContentItemRemoved");

                return deletedContentItem;
            });

        public ValueTask<ContentItem> HardRemoveContentItemByIdAsync(
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
