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
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Processings.ContentItems.Exceptions;
using Glory2Him.Core.Services.Foundations.ContentItems;

namespace Glory2Him.Core.Services.Processings.ContentItems
{
    internal partial class ContentItemProcessingService : IContentItemProcessingService
    {
        private readonly IContentItemService contentItemService;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IHashBroker hashBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly IEventBroker eventBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly IEnvelopeIntegrityBroker envelopeIntegrityBroker;
        private readonly ILoggingBroker loggingBroker;

        public ContentItemProcessingService(
            IContentItemService contentItemService,
            IDateTimeBroker dateTimeBroker,
            IHashBroker hashBroker,
            IIdentifierBroker identifierBroker,
            IEventEnvelopeBroker eventEnvelopeBroker,
            IEventBroker eventBroker,
            ISecurityAuditBroker securityAuditBroker,
            IEnvelopeIntegrityBroker envelopeIntegrityBroker,
            ILoggingBroker loggingBroker)
        {
            this.contentItemService = contentItemService;
            this.dateTimeBroker = dateTimeBroker;
            this.hashBroker = hashBroker;
            this.identifierBroker = identifierBroker;
            this.eventEnvelopeBroker = eventEnvelopeBroker;
            this.eventBroker = eventBroker;
            this.securityAuditBroker = securityAuditBroker;
            this.envelopeIntegrityBroker = envelopeIntegrityBroker;
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
            Guid groupId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new ContentItem
                {
                    GroupId = groupId
                };

                EventEnvelope<ContentItem> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveContentItemsByGroupIdAsync(
                    groupId: groupId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ContentItem> RetrieveLatestContentItemByGroupIdAsync(
            Guid groupId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new ContentItem
                {
                    GroupId = groupId
                };

                EventEnvelope<ContentItem> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveLatestContentItemByGroupIdAsync(
                    groupId: groupId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ContentItem> RetrievePublishedContentItemByGroupIdAsync(
            Guid groupId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new ContentItem
                {
                    GroupId = groupId
                };

                EventEnvelope<ContentItem> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrievePublishedContentItemByGroupIdAsync(
                    groupId: groupId,
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
                contentType: contentItem.ContentType,
                contentHash: contentHash,
                cancellationToken: cancellationToken);

            if (duplicateContentExists)
            {
                throw new AlreadyExistsContentItemProcessingException(
                    message: "A content item already exists with the same content.");
            }

            // PublishDate is deliberately absent, for the same reason it is absent from the
            // version fork below. It is an IApproval member (§9.7.1 rule 2), and the add
            // surface may carry an ApprovalStatus of Draft or Submitted and nothing else —
            // never IsPublished, never PublishDate (rule 1). Taking it from the caller here
            // would let them schedule their own publication on the way in, on a row that is
            // otherwise landed unpublished and in Draft precisely so it cannot.
            ContentItem newContentItem = new ContentItem
            {
                Id = await this.identifierBroker.GetIdentifierAsync(),
                ContentType = contentItem.ContentType,
                Title = contentItem.Title,
                Author = contentItem.Author,
                Content = contentItem.Content,
                ContentHash = contentHash,
                GroupId = await this.identifierBroker.GetIdentifierAsync(),
                Version = 1,
                IsLatestVersion = true,
                IsPublished = false,
                ApprovalStatus = ApprovalStatus.Draft,
                IsDeleted = false
            };

            ContentItem addedContentItem = await this.contentItemService.AddContentItemAsync(
                contentItem: newContentItem,
                cancellationToken: cancellationToken);

            await PublishContentItemProcessingFactAsync(
                inboundEnvelope: inboundEnvelope,
                contentItem: addedContentItem,
                operation: ContentItemProcessingEventOperation.Added);

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
                contentType: contentItem.ContentType,
                contentHash: contentHash,
                excludedGroupId: currentContentItem.GroupId,
                cancellationToken: cancellationToken);

            if (duplicateContentExists)
            {
                throw new AlreadyExistsContentItemProcessingException(
                    message: "A content item already exists with the same content.");
            }

            // a terminal row is immutable in place — the owner's modify forks a new version
            // (§3.4 rules 7–8, rule 16). Rejected forks for the same reason Approved does:
            // the row is the record of a decision, and editing it would rewrite what was
            // decided. A fork off a Rejected row leaves the group with no published row
            // until the new version is approved, which is correct — a rejected row was
            // never published. Dismissed is deliberately absent: it is not a decision this
            // service may fork off, and refusing it belongs to the foundation's modify.
            bool shouldForkNewVersion =
                currentContentItem.ApprovalStatus == ApprovalStatus.Approved
                    || currentContentItem.ApprovalStatus == ApprovalStatus.Rejected;

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
            // processing service announces the amend exactly once, after both writes have landed
            await PublishContentItemProcessingFactAsync(
                inboundEnvelope: inboundEnvelope,
                contentItem: modifiedContentItem,
                operation: ContentItemProcessingEventOperation.Modified);

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

            await PublishContentItemProcessingFactAsync(
                inboundEnvelope: inboundEnvelope,
                contentItem: removedContentItem,
                operation: ContentItemProcessingEventOperation.Removed);

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

                throw new NotFoundContentItemProcessingException(
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
            Guid groupId,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateGroupIdOnRetrieve(groupId);

            IQueryable<ContentItem> allContentItems =
                await this.contentItemService.RetrieveAllContentItemsAsync(cancellationToken);

            IQueryable<ContentItem> groupContentItems = allContentItems.Where(contentItem =>
                contentItem.GroupId == groupId);

            return await ApplyCollectionReadVisibilityFilterAsync(
                contentItems: groupContentItems,
                securityContext: inboundEnvelope.SecurityContext);
        }

        private async ValueTask<ContentItem> DoRetrieveLatestContentItemByGroupIdAsync(
            Guid groupId,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateGroupIdOnRetrieve(groupId);

            IQueryable<ContentItem> allContentItems =
                await this.contentItemService.RetrieveAllContentItemsAsync(cancellationToken);

            // the edit tip of the group (§3.4.1) — at most one non-deleted row per group
            // carries IsLatestVersion under the unique filtered index
            ContentItem? latestContentItem = allContentItems.FirstOrDefault(contentItem =>
                contentItem.GroupId == groupId
                    && contentItem.IsLatestVersion
                    && contentItem.IsDeleted == false);

            if (latestContentItem is null)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Content item read denied. Group {groupId} has no " +
                        "non-deleted latest version; reported to the caller as not found.");

                throw new NotFoundContentItemProcessingException(
                    message: "The content item was not found.");
            }

            return await ApplySingleReadVisibilityPostureAsync(
                contentItem: latestContentItem,
                securityContext: inboundEnvelope.SecurityContext);
        }

        private async ValueTask<ContentItem> DoRetrievePublishedContentItemByGroupIdAsync(
            Guid groupId,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateGroupIdOnRetrieve(groupId);

            IQueryable<ContentItem> allContentItems =
                await this.contentItemService.RetrieveAllContentItemsAsync(cancellationToken);

            // the row the public currently reads — it stays published while a newer draft
            // moves through review, so it is found independently of IsLatestVersion
            ContentItem? publishedContentItem = allContentItems.FirstOrDefault(contentItem =>
                contentItem.GroupId == groupId
                    && contentItem.IsPublished
                    && contentItem.IsDeleted == false);

            if (publishedContentItem is null)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Content item read denied. Group {groupId} has no " +
                        "non-deleted published version; reported to the caller as not found.");

                throw new NotFoundContentItemProcessingException(
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

                throw new NotFoundContentItemProcessingException(
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

            // a broad review-role caller audits the whole pipeline: every non-deleted row,
            // including drafts and future-scheduled rows — the clock and the caller's
            // identity are never consulted
            if (isAuthenticated && HasBroadReviewRole(securityContext!))
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

            // a narrow-tier caller audits the content types they hold and no others (§18.6
            // rule 4) — the broad branch above already returned for anyone holding a role
            // that spans every type, so this set is only ever the narrow grants
            ContentType[] reviewableContentTypes = isAuthenticated
                ? ReviewableContentTypes(securityContext!)
                : Array.Empty<ContentType>();

            return visibleContentItems.Where(contentItem =>
                (contentItem.ApprovalStatus == ApprovalStatus.Approved
                    && contentItem.IsPublished
                    && (contentItem.PublishDate == null
                        || contentItem.PublishDate <= currentDateTime))
                || (includeOwnContentItems && contentItem.CreatedBy == actorUserId)
                || reviewableContentTypes.Contains(contentItem.ContentType));
        }

        // this service's own completion fact, distinct from the foundation's entity
        // fact: it asserts that this process finished with its gates passed and its
        // invariants restored, which is what downstream processes chain off
        private async ValueTask PublishContentItemProcessingFactAsync(
            EventEnvelope<ContentItem> inboundEnvelope,
            ContentItem contentItem,
            ContentItemProcessingEventOperation operation)
        {
            EventEnvelope<ContentItem> outboundEnvelope = await this.eventEnvelopeBroker.CreateNextAsync(
                sourceEnvelope: inboundEnvelope,
                content: contentItem);

            await this.eventBroker.PublishContentItemProcessingAsync(
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
            // PublishDate is deliberately absent, so the new version starts with none. It is
            // an IApproval member (§9.7.1 rule 2) and the fork is still the modify operation,
            // so taking it from the caller here would simply reopen the door MapPermittedFields
            // just closed: edit an approved item and your publish date rides in on the fork.
            // A fresh draft has no publish date until the approve operation grants one, which
            // is the same reason IsPublished starts false and the status starts Draft.
            var newVersionContentItem = new ContentItem
            {
                Id = await this.identifierBroker.GetIdentifierAsync(),
                ContentType = contentItem.ContentType,
                Title = contentItem.Title,
                Author = contentItem.Author,
                Content = contentItem.Content,
                ContentHash = contentHash,
                GroupId = currentContentItem.GroupId,
                Version = currentContentItem.Version + 1,
                IsLatestVersion = true,
                IsPublished = false,
                ApprovalStatus = ApprovalStatus.Draft,
                IsDeleted = false
            };

            // The previous latest is demoted before the new row is inserted — the unique
            // filtered index allows only one IsLatestVersion = true per group at any time.
            // IsLatestVersion only marks the edit tip; IsPublished is untouched here, so the
            // previously published row stays publicly visible until the new version is
            // approved and published (§3.4.1).
            //
            // Through the narrow demote verb rather than the general modify: IsLatestVersion is
            // an IVersion member and ContentItemService PINS it against storage (§9.7.1 rule 2),
            // so demoting through the modify was refused outright and this fork could not
            // complete. Nothing caught it because this service's tests mock the foundation.
            await this.contentItemService.DemoteContentItemVersionAsync(
                contentItemId: currentContentItem.Id,
                cancellationToken: cancellationToken);

            return await this.contentItemService.AddContentItemAsync(
                contentItem: newVersionContentItem,
                cancellationToken: cancellationToken);
        }

        // The content fields, and only those. Under §9.7.1 rule 2's subtraction rule every
        // IApproval member — ApprovalStatus, IsPublished and PublishDate — belongs to the
        // approve operation as one unit, so none of them is carried here. PublishDate is the
        // one that looks like content and is not: a caller who could set it through the
        // general modify would schedule their own publication without ever meeting the gate
        // that owns it. The foundation pins all three against storage as well (§8.6.1 — a
        // rule enforced only at the processing layer is not enforced).
        private static void MapPermittedFields(
            ContentItem targetContentItem,
            ContentItem sourceContentItem,
            string contentHash)
        {
            targetContentItem.ContentType = sourceContentItem.ContentType;
            targetContentItem.Title = sourceContentItem.Title;
            targetContentItem.Author = sourceContentItem.Author;
            targetContentItem.Content = sourceContentItem.Content;
            targetContentItem.ContentHash = contentHash;
        }

        private ValueTask<bool> CheckDuplicateContentExistsAsync(
            ContentType contentType,
            string contentHash,
            CancellationToken cancellationToken) =>
            CheckDuplicateContentExistsAsync(
                contentType: contentType,
                contentHash: contentHash,
                excludedGroupId: null,
                cancellationToken: cancellationToken);

        // the foundation's boolean probe runs over the UNFILTERED store (§3.4.2/§14.6), so
        // the duplicate rule stays global even though the entity-returning reads are
        // visibility-filtered per caller
        private async ValueTask<bool> CheckDuplicateContentExistsAsync(
            ContentType contentType,
            string contentHash,
            Guid? excludedGroupId,
            CancellationToken cancellationToken) =>
            await this.contentItemService.CheckContentItemContentExistsAsync(
                contentType: contentType,
                contentHash: contentHash,
                excludedGroupId: excludedGroupId,
                cancellationToken: cancellationToken);
    }
}
