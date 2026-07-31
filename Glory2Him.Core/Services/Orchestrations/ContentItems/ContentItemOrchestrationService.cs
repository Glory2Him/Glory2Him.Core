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
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Hashes;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Orchestrations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Orchestrations.ContentItems.Exceptions;
using Glory2Him.Core.Services.Foundations.ContentItems;

namespace Glory2Him.Core.Services.Orchestrations.ContentItems
{
    internal partial class ContentItemOrchestrationService : IContentItemOrchestrationService
    {
        private readonly IContentItemService contentItemService;
        private readonly IHashBroker hashBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly IEventBroker eventBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public ContentItemOrchestrationService(
            IContentItemService contentItemService,
            IHashBroker hashBroker,
            IIdentifierBroker identifierBroker,
            IEventEnvelopeBroker eventEnvelopeBroker,
            IEventBroker eventBroker,
            ISecurityAuditBroker securityAuditBroker,
            ILoggingBroker loggingBroker)
        {
            this.contentItemService = contentItemService;
            this.hashBroker = hashBroker;
            this.identifierBroker = identifierBroker;
            this.eventEnvelopeBroker = eventEnvelopeBroker;
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
                ValidateContentItemIsNotNull(contentItem);

                EventEnvelope<ContentItem> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: contentItem);

                return await DoAddContentItemAsync(
                    contentItem: contentItem,
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

        private async ValueTask<ContentItem> DoAddContentItemAsync(
            ContentItem contentItem,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnAddContentItem(contentItem, inboundEnvelope.SecurityContext);
            string contentHash = await ComputeContentHashAsync(contentItem.Content);

            bool duplicateContentExists = await CheckDuplicateContentExistsAsync(
                contentTypeId: contentItem.ContentTypeId,
                contentHash: contentHash,
                cancellationToken: cancellationToken);

            if (duplicateContentExists)
            {
                throw new AlreadyExistsContentItemOrchestrationException(
                    message: "A content item already exists with the same content.");
            }

            ContentItem newContentItem = new ContentItem
            {
                Id = await this.identifierBroker.GetIdentifierAsync(),
                ContentTypeId = contentItem.ContentTypeId,
                Title = contentItem.Title,
                Author = contentItem.Author,
                Content = contentItem.Content,
                PublishDate = contentItem.PublishDate,
                ContentHash = contentHash,
                ContentItemGroupId = await this.identifierBroker.GetIdentifierAsync(),
                Version = 1,
                IsLatestVersion = true,
                IsPublished = false,
                ApprovalStatus = ApprovalStatus.Draft,
                IsDeleted = false
            };

            ContentItem addedContentItem = await this.contentItemService.AddContentItemAsync(
                contentItem: newContentItem,
                cancellationToken: cancellationToken);

            await PublishContentItemOrchestrationFactAsync(
                inboundEnvelope: inboundEnvelope,
                contentItem: addedContentItem,
                operation: ContentItemOrchestrationEventOperation.Added);

            return addedContentItem;
        }

        private async ValueTask<ContentItem> DoModifyContentItemAsync(
            ContentItem contentItem,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnModifyContentItem(contentItem, inboundEnvelope.SecurityContext);

            ContentItem currentContentItem = await this.contentItemService.RetrieveContentItemByIdAsync(
                contentItemId: contentItem.Id,
                cancellationToken: cancellationToken);

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: inboundEnvelope.SecurityContext);

            ValidateCurrentContentItemIsModifiable(
                currentContentItem: currentContentItem,
                actorUserId: actorUserId,
                securityContext: inboundEnvelope.SecurityContext);

            string contentHash = await ComputeContentHashAsync(contentItem.Content);

            bool duplicateContentExists = await CheckDuplicateContentExistsAsync(
                contentTypeId: contentItem.ContentTypeId,
                contentHash: contentHash,
                excludedContentItemGroupId: currentContentItem.ContentItemGroupId,
                cancellationToken: cancellationToken);

            if (duplicateContentExists)
            {
                throw new AlreadyExistsContentItemOrchestrationException(
                    message: "A content item already exists with the same content.");
            }

            // an approved item is immutable in place — the owner's modify forks a new version
            bool shouldForkNewVersion = currentContentItem.ApprovalStatus == ApprovalStatus.Approved;

            ContentItem modifiedContentItem = shouldForkNewVersion
                ? await ForkContentItemVersionAsync(
                    contentItem: contentItem,
                    currentContentItem: currentContentItem,
                    contentHash: contentHash,
                    cancellationToken: cancellationToken)

                : await ModifyContentItemInPlaceAsync(
                    contentItem: contentItem,
                    currentContentItem: currentContentItem,
                    contentHash: contentHash,
                    cancellationToken: cancellationToken);

            // one fact per completed process: a fork writes two foundation rows, but the
            // orchestration announces the amend exactly once, after both writes have landed
            await PublishContentItemOrchestrationFactAsync(
                inboundEnvelope: inboundEnvelope,
                contentItem: modifiedContentItem,
                operation: ContentItemOrchestrationEventOperation.Modified);

            return modifiedContentItem;
        }

        private async ValueTask<ContentItem> DoRemoveContentItemByIdAsync(
            Guid contentItemId,
            string? deletionReason,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRemoveContentItemById(contentItemId, inboundEnvelope.SecurityContext);

            ContentItem currentContentItem = await this.contentItemService.RetrieveContentItemByIdAsync(
                contentItemId: contentItemId,
                cancellationToken: cancellationToken);

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: inboundEnvelope.SecurityContext);

            ValidateCurrentContentItemIsRemovable(
                currentContentItem: currentContentItem,
                actorUserId: actorUserId,
                securityContext: inboundEnvelope.SecurityContext);

            // the foundation owns the soft-delete control fields (IsDeleted, DeletedBy,
            // DeletedWhen, DeletionReason) and leaves ApprovalStatus alone
            ContentItem removedContentItem = await this.contentItemService.RemoveContentItemByIdAsync(
                contentItemId: contentItemId,
                deletionReason: deletionReason,
                cancellationToken: cancellationToken);

            await PublishContentItemOrchestrationFactAsync(
                inboundEnvelope: inboundEnvelope,
                contentItem: removedContentItem,
                operation: ContentItemOrchestrationEventOperation.Removed);

            return removedContentItem;
        }

        // the orchestration's own completion fact, distinct from the foundation's entity
        // fact: it asserts that this process finished with its gates passed and its
        // invariants restored, which is what downstream processes chain off
        private async ValueTask PublishContentItemOrchestrationFactAsync(
            EventEnvelope<ContentItem> inboundEnvelope,
            ContentItem contentItem,
            ContentItemOrchestrationEventOperation operation)
        {
            EventEnvelope<ContentItem> outboundEnvelope = await this.eventEnvelopeBroker.CreateNextAsync(
                sourceEnvelope: inboundEnvelope,
                content: contentItem);

            await this.eventBroker.PublishContentItemOrchestrationAsync(
                envelope: outboundEnvelope,
                operation: operation);
        }

        private async ValueTask<ContentItem> ModifyContentItemInPlaceAsync(
            ContentItem contentItem,
            ContentItem currentContentItem,
            string contentHash,
            CancellationToken cancellationToken)
        {
            MapPermittedFields(
                targetContentItem: currentContentItem,
                sourceContentItem: contentItem,
                contentHash: contentHash);

            return await this.contentItemService.ModifyContentItemAsync(
                contentItem: currentContentItem,
                cancellationToken: cancellationToken);
        }

        private async ValueTask<ContentItem> ForkContentItemVersionAsync(
            ContentItem contentItem,
            ContentItem currentContentItem,
            string contentHash,
            CancellationToken cancellationToken)
        {
            var newVersionContentItem = new ContentItem
            {
                Id = await this.identifierBroker.GetIdentifierAsync(),
                ContentTypeId = contentItem.ContentTypeId,
                Title = contentItem.Title,
                Author = contentItem.Author,
                Content = contentItem.Content,
                PublishDate = contentItem.PublishDate,
                ContentHash = contentHash,
                ContentItemGroupId = currentContentItem.ContentItemGroupId,
                Version = currentContentItem.Version + 1,
                IsLatestVersion = true,
                IsPublished = false,
                ApprovalStatus = ApprovalStatus.Draft,
                IsDeleted = false
            };

            // the previous latest is demoted before the new row is inserted — the unique
            // filtered index allows only one IsLatestVersion = true per group at any time.
            // IsLatestVersion only marks the edit tip; IsPublished is untouched here, so the
            // previously published row stays publicly visible until the new version is
            // approved and published (§3.4.1)
            currentContentItem.IsLatestVersion = false;

            await this.contentItemService.ModifyContentItemAsync(
                contentItem: currentContentItem,
                cancellationToken: cancellationToken);

            return await this.contentItemService.AddContentItemAsync(
                contentItem: newVersionContentItem,
                cancellationToken: cancellationToken);
        }

        private static void MapPermittedFields(
            ContentItem targetContentItem,
            ContentItem sourceContentItem,
            string contentHash)
        {
            targetContentItem.ContentTypeId = sourceContentItem.ContentTypeId;
            targetContentItem.Title = sourceContentItem.Title;
            targetContentItem.Author = sourceContentItem.Author;
            targetContentItem.Content = sourceContentItem.Content;
            targetContentItem.PublishDate = sourceContentItem.PublishDate;
            targetContentItem.ContentHash = contentHash;
        }

        private ValueTask<bool> CheckDuplicateContentExistsAsync(
            Guid contentTypeId,
            string contentHash,
            CancellationToken cancellationToken) =>
            CheckDuplicateContentExistsAsync(
                contentTypeId: contentTypeId,
                contentHash: contentHash,
                excludedContentItemGroupId: null,
                cancellationToken: cancellationToken);

        private async ValueTask<bool> CheckDuplicateContentExistsAsync(
            Guid contentTypeId,
            string contentHash,
            Guid? excludedContentItemGroupId,
            CancellationToken cancellationToken)
        {
            IQueryable<ContentItem> allContentItems =
                await this.contentItemService.RetrieveAllContentItemsAsync(cancellationToken);

            return allContentItems.Any(existingContentItem =>
                existingContentItem.ContentTypeId == contentTypeId
                    && existingContentItem.ContentHash == contentHash
                    && existingContentItem.IsDeleted == false
                    && (excludedContentItemGroupId == null
                        || existingContentItem.ContentItemGroupId != excludedContentItemGroupId));
        }
    }
}
