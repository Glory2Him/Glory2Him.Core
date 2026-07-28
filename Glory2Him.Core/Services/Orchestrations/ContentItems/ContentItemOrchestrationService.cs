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
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Orchestrations.ContentItems;
using Glory2Him.Core.Services.Foundations.ContentItems;

namespace Glory2Him.Core.Services.Orchestrations.ContentItems
{
    internal partial class ContentItemOrchestrationService : IContentItemOrchestrationService
    {
        private const string ThankYouForYourSubmissionMessage = "Thank you for your submission.";

        private readonly IContentItemService contentItemService;
        private readonly ISecurityBroker securityBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly ILoggingBroker loggingBroker;

        public ContentItemOrchestrationService(
            IContentItemService contentItemService,
            ISecurityBroker securityBroker,
            IIdentifierBroker identifierBroker,
            ILoggingBroker loggingBroker)
        {
            this.contentItemService = contentItemService;
            this.securityBroker = securityBroker;
            this.identifierBroker = identifierBroker;
            this.loggingBroker = loggingBroker;
        }

        public ValueTask<ContentItemSubmissionResult> AddContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateUserIsAllowedToContributeAsync();
                ValidateContentItemIsNotNull(contentItem);
                ValidateOnAddContentItem(contentItem);
                string contentHash = ComputeContentHash(contentItem.Content);

                bool duplicateContentExists = await CheckDuplicateContentExistsAsync(
                    contentTypeId: contentItem.ContentTypeId,
                    contentHash: contentHash,
                    cancellationToken: cancellationToken);

                if (duplicateContentExists)
                {
                    return new ContentItemSubmissionResult
                    {
                        IsCreated = false,
                        ContentItem = null,
                        Message = ThankYouForYourSubmissionMessage
                    };
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

                return new ContentItemSubmissionResult
                {
                    IsCreated = true,
                    ContentItem = addedContentItem,
                    Message = ThankYouForYourSubmissionMessage
                };
            });

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
