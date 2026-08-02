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
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IHashBroker hashBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly IEventBroker eventBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public ContentItemOrchestrationService(
            IContentItemService contentItemService,
            IDateTimeBroker dateTimeBroker,
            IHashBroker hashBroker,
            IIdentifierBroker identifierBroker,
            IEventEnvelopeBroker eventEnvelopeBroker,
            IEventBroker eventBroker,
            ISecurityAuditBroker securityAuditBroker,
            ILoggingBroker loggingBroker)
        {
            this.contentItemService = contentItemService;
            this.dateTimeBroker = dateTimeBroker;
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

        public ValueTask<IQueryable<ContentItem>> RetrieveAllContentItemsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // an unfiltered collection read carries no instruction beyond the caller's
                // identity, so the request payload is empty — the envelope exists to capture
                // the ambient security context the visibility filter runs against
                EventEnvelope<ContentItem> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new ContentItem());

                return await DoRetrieveAllContentItemsAsync(
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<ContentItem>> RetrieveAllPublicContentItemsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the public projection is caller-independent, so no envelope is minted —
                // there is no security context to capture and nothing downstream reads one
                return await DoRetrieveAllPublicContentItemsAsync(cancellationToken);
            });

        public ValueTask<IQueryable<ContentItem>> RetrieveContentItemsByGroupIdAsync(
            Guid contentItemGroupId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new ContentItem
                {
                    ContentItemGroupId = contentItemGroupId
                };

                EventEnvelope<ContentItem> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveContentItemsByGroupIdAsync(
                    contentItemGroupId: contentItemGroupId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ContentItem> RetrieveLatestContentItemByGroupIdAsync(
            Guid contentItemGroupId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new ContentItem
                {
                    ContentItemGroupId = contentItemGroupId
                };

                EventEnvelope<ContentItem> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveLatestContentItemByGroupIdAsync(
                    contentItemGroupId: contentItemGroupId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ContentItem> RetrievePublishedContentItemByGroupIdAsync(
            Guid contentItemGroupId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new ContentItem
                {
                    ContentItemGroupId = contentItemGroupId
                };

                EventEnvelope<ContentItem> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrievePublishedContentItemByGroupIdAsync(
                    contentItemGroupId: contentItemGroupId,
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

        private async ValueTask<ContentItem> DoRetrieveContentItemByIdAsync(
            Guid contentItemId,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateContentItemIdOnRetrieve(contentItemId);

            ContentItem contentItem = await this.contentItemService.RetrieveContentItemByIdAsync(
                contentItemId: contentItemId,
                cancellationToken: cancellationToken);

            // a removed row is gone for every caller, privileged or not — review and audit
            // reads cover the approval workflow, not takedowns
            if (contentItem.IsDeleted)
            {
                // the caller-facing error stays a reason-free not-found (no existence
                // leak), so the true reason is recorded server-side before the throw
                await this.loggingBroker.LogInformationAsync(
                    message: $"Content item read denied. Content item {contentItemId} is " +
                        "soft-deleted; reported to the caller as not found.");

                throw new NotFoundContentItemOrchestrationException(
                    message: "The content item was not found.");
            }

            return await ApplySingleReadVisibilityPostureAsync(
                contentItem: contentItem,
                securityContext: inboundEnvelope.SecurityContext);
        }

        private async ValueTask<IQueryable<ContentItem>> DoRetrieveAllContentItemsAsync(
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            IQueryable<ContentItem> allContentItems =
                await this.contentItemService.RetrieveAllContentItemsAsync(cancellationToken);

            return await ApplyCollectionReadVisibilityFilterAsync(
                contentItems: allContentItems,
                securityContext: inboundEnvelope.SecurityContext);
        }

        private async ValueTask<IQueryable<ContentItem>> DoRetrieveAllPublicContentItemsAsync(
            CancellationToken cancellationToken)
        {
            IQueryable<ContentItem> allContentItems =
                await this.contentItemService.RetrieveAllContentItemsAsync(cancellationToken);

            // running the collection filter without a security context yields exactly the
            // canonical visible set (§14.1) — a privileged caller reads the same set an
            // anonymous visitor would
            return await ApplyCollectionReadVisibilityFilterAsync(
                contentItems: allContentItems,
                securityContext: null);
        }

        private async ValueTask<IQueryable<ContentItem>> DoRetrieveContentItemsByGroupIdAsync(
            Guid contentItemGroupId,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateContentItemGroupIdOnRetrieve(contentItemGroupId);

            IQueryable<ContentItem> allContentItems =
                await this.contentItemService.RetrieveAllContentItemsAsync(cancellationToken);

            IQueryable<ContentItem> groupContentItems = allContentItems.Where(contentItem =>
                contentItem.ContentItemGroupId == contentItemGroupId);

            return await ApplyCollectionReadVisibilityFilterAsync(
                contentItems: groupContentItems,
                securityContext: inboundEnvelope.SecurityContext);
        }

        private async ValueTask<ContentItem> DoRetrieveLatestContentItemByGroupIdAsync(
            Guid contentItemGroupId,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateContentItemGroupIdOnRetrieve(contentItemGroupId);

            IQueryable<ContentItem> allContentItems =
                await this.contentItemService.RetrieveAllContentItemsAsync(cancellationToken);

            // the edit tip of the group (§3.4.1) — at most one non-deleted row per group
            // carries IsLatestVersion under the unique filtered index
            ContentItem? latestContentItem = allContentItems.FirstOrDefault(contentItem =>
                contentItem.ContentItemGroupId == contentItemGroupId
                    && contentItem.IsLatestVersion
                    && contentItem.IsDeleted == false);

            if (latestContentItem is null)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Content item read denied. Group {contentItemGroupId} has no " +
                        "non-deleted latest version; reported to the caller as not found.");

                throw new NotFoundContentItemOrchestrationException(
                    message: "The content item was not found.");
            }

            return await ApplySingleReadVisibilityPostureAsync(
                contentItem: latestContentItem,
                securityContext: inboundEnvelope.SecurityContext);
        }

        private async ValueTask<ContentItem> DoRetrievePublishedContentItemByGroupIdAsync(
            Guid contentItemGroupId,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateContentItemGroupIdOnRetrieve(contentItemGroupId);

            IQueryable<ContentItem> allContentItems =
                await this.contentItemService.RetrieveAllContentItemsAsync(cancellationToken);

            // the row the public currently reads — it stays published while a newer draft
            // moves through review, so it is found independently of IsLatestVersion
            ContentItem? publishedContentItem = allContentItems.FirstOrDefault(contentItem =>
                contentItem.ContentItemGroupId == contentItemGroupId
                    && contentItem.IsPublished
                    && contentItem.IsDeleted == false);

            if (publishedContentItem is null)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Content item read denied. Group {contentItemGroupId} has no " +
                        "non-deleted published version; reported to the caller as not found.");

                throw new NotFoundContentItemOrchestrationException(
                    message: "The content item was not found.");
            }

            return await ApplySingleReadVisibilityPostureAsync(
                contentItem: publishedContentItem,
                securityContext: inboundEnvelope.SecurityContext);
        }

        // the shared read posture of design §14.1/§16.6 for single-row reads: a publicly
        // visible version is readable by anyone — reads carry no contribution gate and the
        // block roles only block contributions; a non-public version answers not-found —
        // never unauthorized — to everyone but the owner and the review roles
        private async ValueTask<ContentItem> ApplySingleReadVisibilityPostureAsync(
            ContentItem contentItem,
            SecurityContext securityContext)
        {
            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            bool isPubliclyVisible =
                contentItem.ApprovalStatus == ApprovalStatus.Approved
                    && contentItem.IsPublished
                    && (contentItem.PublishDate is null
                        || contentItem.PublishDate <= currentDateTime);

            if (isPubliclyVisible)
            {
                return contentItem;
            }

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                // the caller-facing error stays a reason-free not-found (no existence
                // leak), so the true reason is recorded server-side before the throw
                await this.loggingBroker.LogWarningAsync(
                    message: $"Content item read denied. Content item {contentItem.Id} is not " +
                        "publicly visible and the caller is not authenticated; reported to " +
                        "the caller as not found.");

                throw new NotFoundContentItemOrchestrationException(
                    message: "The content item was not found.");
            }

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: securityContext);

            await ValidateCurrentContentItemIsRetrievableAsync(
                currentContentItem: contentItem,
                actorUserId: actorUserId,
                securityContext: securityContext);

            return contentItem;
        }

        // the collection twin of the single-row posture: instead of throwing not-found, a
        // row the caller may not see simply drops out of the set, so a collection read never
        // reveals how many non-public versions exist
        private async ValueTask<IQueryable<ContentItem>> ApplyCollectionReadVisibilityFilterAsync(
            IQueryable<ContentItem> contentItems,
            SecurityContext? securityContext)
        {
            // a removed row is gone for every caller, privileged or not — review and audit
            // reads cover the approval workflow, not takedowns
            IQueryable<ContentItem> visibleContentItems = contentItems.Where(contentItem =>
                contentItem.IsDeleted == false);

            bool isAuthenticated =
                securityContext is not null && securityContext.IsAuthenticated;

            // a review-role caller audits the whole pipeline: every non-deleted row,
            // including drafts and future-scheduled rows — the clock and the caller's
            // identity are never consulted
            if (isAuthenticated && HasReviewRole(securityContext!))
            {
                return visibleContentItems;
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            string? actorUserId = isAuthenticated
                ? await this.securityAuditBroker.GetUserIdAsync(securityContext: securityContext!)
                : null;

            // an authenticated caller follows their own items through the workflow, so their
            // own rows join the publicly visible set; an anonymous caller (or one whose
            // identity cannot be resolved) sees the public set alone
            bool includeOwnContentItems = string.IsNullOrWhiteSpace(actorUserId) is false;

            return visibleContentItems.Where(contentItem =>
                (contentItem.ApprovalStatus == ApprovalStatus.Approved
                    && contentItem.IsPublished
                    && (contentItem.PublishDate == null
                        || contentItem.PublishDate <= currentDateTime))
                || (includeOwnContentItems && contentItem.CreatedBy == actorUserId));
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

        // the foundation's boolean probe runs over the UNFILTERED store (§3.4.2/§14.6), so
        // the duplicate rule stays global even though the entity-returning reads are
        // visibility-filtered per caller
        private async ValueTask<bool> CheckDuplicateContentExistsAsync(
            Guid contentTypeId,
            string contentHash,
            Guid? excludedContentItemGroupId,
            CancellationToken cancellationToken) =>
            await this.contentItemService.CheckContentItemContentExistsAsync(
                contentTypeId: contentTypeId,
                contentHash: contentHash,
                excludedContentItemGroupId: excludedContentItemGroupId,
                cancellationToken: cancellationToken);
    }
}
