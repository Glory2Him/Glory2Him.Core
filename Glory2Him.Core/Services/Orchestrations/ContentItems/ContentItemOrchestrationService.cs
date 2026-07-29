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
using Glory2Him.Core.Brokers.Hashes;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Factories.Events;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
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
        private readonly IEventEnvelopeFactory eventEnvelopeFactory;
        private readonly ILoggingBroker loggingBroker;

        public ContentItemOrchestrationService(
            IContentItemService contentItemService,
            IHashBroker hashBroker,
            IIdentifierBroker identifierBroker,
            IEventEnvelopeFactory eventEnvelopeFactory,
            ILoggingBroker loggingBroker)
        {
            this.contentItemService = contentItemService;
            this.hashBroker = hashBroker;
            this.identifierBroker = identifierBroker;
            this.eventEnvelopeFactory = eventEnvelopeFactory;
            this.loggingBroker = loggingBroker;
        }

        public ValueTask<ContentItem> SubmitContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateContentItemIsNotNull(contentItem);

                EventEnvelope<ContentItem> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: contentItem);

                return await DoSubmitContentItemAsync(
                    contentItem: contentItem,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<ContentItem> DoSubmitContentItemAsync(
            ContentItem contentItem,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnSubmitContentItem(contentItem, inboundEnvelope.SecurityContext);
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

            return await this.contentItemService.AddContentItemAsync(
                contentItem: newContentItem,
                cancellationToken: cancellationToken);
        }

        private async ValueTask<bool> CheckDuplicateContentExistsAsync(
            Guid contentTypeId,
            string contentHash,
            CancellationToken cancellationToken)
        {
            IQueryable<ContentItem> allContentItems =
                await this.contentItemService.RetrieveAllContentItemsAsync(cancellationToken);

            return allContentItems.Any(existingContentItem =>
                existingContentItem.ContentTypeId == contentTypeId
                    && existingContentItem.ContentHash == contentHash
                    && existingContentItem.IsDeleted == false);
        }
    }
}
