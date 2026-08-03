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
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ContentItemAssociations
{
    /// <summary>
    /// Foundation service for content item associations. Every operation is both callable
    /// directly (the non-event path: object in → request envelope → shared do-work) and
    /// reachable through the event substrate (the event path in the <c>.Substrate</c> partial:
    /// request envelope in → shared do-work). The private <c>DoXAsync</c> methods own auditing,
    /// validation, storage, and publishing the past-tense fact, so the two paths cannot
    /// diverge; the inbound envelope carries the original caller's <c>SecurityContext</c> and
    /// anchors the causation chain. Per design §14.6 the foundation enforces security itself
    /// — the contribution gate on writes, owner-or-moderation-role write permission (removal
    /// by owner or Admin, hard removal by Admin only), and the §14.1/§14.5 read visibility
    /// posture — never assuming an upstream orchestration already gated the caller.
    /// </summary>
    internal partial class ContentItemAssociationService : IContentItemAssociationService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public ContentItemAssociationService(
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

        public ValueTask<ContentItemAssociation> AddContentItemAssociationAsync(
            ContentItemAssociation contentItemAssociation,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateContentItemAssociationIsNotNull(contentItemAssociation);

                EventEnvelope<ContentItemAssociation> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: contentItemAssociation);

                return await DoAddContentItemAssociationAsync(
                    contentItemAssociation: contentItemAssociation,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<ContentItemAssociation>> RetrieveAllContentItemAssociationsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the envelope exists to capture the ambient security context the
                // visibility filter runs against — the request payload is empty
                EventEnvelope<ContentItemAssociation> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new ContentItemAssociation());

                IQueryable<ContentItemAssociation> allContentItemAssociations =
                    await this.storageBroker.SelectAllContentItemAssociationsAsync(cancellationToken);

                return await ApplyCollectionReadVisibilityFilterAsync(
                    contentItemAssociations: allContentItemAssociations,
                    securityContext: envelope.SecurityContext);
            });

        public ValueTask<ContentItemAssociation> RetrieveContentItemAssociationByIdAsync(
            Guid contentItemAssociationId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new ContentItemAssociation
                {
                    Id = contentItemAssociationId
                };

                EventEnvelope<ContentItemAssociation> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveContentItemAssociationByIdAsync(
                    contentItemAssociationId: contentItemAssociationId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ContentItemAssociation> ModifyContentItemAssociationAsync(
            ContentItemAssociation contentItemAssociation,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateContentItemAssociationIsNotNull(contentItemAssociation);

                EventEnvelope<ContentItemAssociation> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: contentItemAssociation);

                return await DoModifyContentItemAssociationAsync(
                    contentItemAssociation: contentItemAssociation,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ContentItemAssociation> RemoveContentItemAssociationByIdAsync(
            Guid contentItemAssociationId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new ContentItemAssociation
                {
                    Id = contentItemAssociationId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<ContentItemAssociation> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: removeRequest);

                return await DoRemoveContentItemAssociationByIdAsync(
                    contentItemAssociationId: contentItemAssociationId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ContentItemAssociation> HardRemoveContentItemAssociationByIdAsync(
            Guid contentItemAssociationId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new ContentItemAssociation
                {
                    Id = contentItemAssociationId
                };

                EventEnvelope<ContentItemAssociation> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveContentItemAssociationByIdAsync(
                    contentItemAssociationId: contentItemAssociationId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        // the shared read posture of design §14.1/§14.5/§14.6: a publicly visible version
        // is readable by anyone; a non-public version answers not-found — never
        // unauthorized — to everyone but the owner and the review roles, with the true
        // denial reason logged server-side only
        private async ValueTask<ContentItemAssociation> DoRetrieveContentItemAssociationByIdAsync(
            Guid contentItemAssociationId,
            EventEnvelope<ContentItemAssociation> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRetrieveContentItemAssociationById(contentItemAssociationId);

            ContentItemAssociation maybeContentItemAssociation =
                await this.storageBroker.SelectContentItemAssociationByIdAsync(
                    contentItemAssociationId: contentItemAssociationId,
                    cancellationToken: cancellationToken);

            ValidateStorageContentItemAssociation(maybeContentItemAssociation, contentItemAssociationId);

            if (maybeContentItemAssociation.IsDeleted)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Content item association read denied. Content item association " +
                        $"{contentItemAssociationId} is soft-deleted; reported to the caller as not found.");

                throw new NotFoundContentItemAssociationException(
                    message: $"Content item association not found with id: {contentItemAssociationId}.");
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            bool isPubliclyVisible =
                maybeContentItemAssociation.ApprovalStatus == ApprovalStatus.Approved
                    && maybeContentItemAssociation.IsPublished
                    && (maybeContentItemAssociation.PublishDate is null
                        || maybeContentItemAssociation.PublishDate <= currentDateTime);

            if (isPubliclyVisible)
            {
                return maybeContentItemAssociation;
            }

            SecurityContext? securityContext = inboundEnvelope.SecurityContext;

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Content item association read denied. Content item association " +
                        $"{contentItemAssociationId} is not publicly visible and the caller is not " +
                        "authenticated; reported to the caller as not found.");

                throw new NotFoundContentItemAssociationException(
                    message: $"Content item association not found with id: {contentItemAssociationId}.");
            }

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && maybeContentItemAssociation.CreatedBy == actorUserId;

            if (isOwner is false && HasReviewRole(securityContext) is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Content item association read denied. Content item association " +
                        $"{contentItemAssociationId} is not publicly visible and user \"{actorUserId}\" " +
                        "is neither the owner nor in a review role; reported to the caller as not found.");

                throw new NotFoundContentItemAssociationException(
                    message: $"Content item association not found with id: {contentItemAssociationId}.");
            }

            return maybeContentItemAssociation;
        }

        // the collection twin of the single-row posture: a row the caller may not see
        // drops out of the set instead of erroring, so a collection read never reveals
        // how many non-public rows exist
        private async ValueTask<IQueryable<ContentItemAssociation>> ApplyCollectionReadVisibilityFilterAsync(
            IQueryable<ContentItemAssociation> contentItemAssociations,
            SecurityContext? securityContext)
        {
            IQueryable<ContentItemAssociation> visibleContentItemAssociations =
                contentItemAssociations.Where(contentItemAssociation =>
                    contentItemAssociation.IsDeleted == false);

            bool isAuthenticated =
                securityContext is not null && securityContext.IsAuthenticated;

            if (isAuthenticated && HasReviewRole(securityContext!))
            {
                return visibleContentItemAssociations;
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            string? actorUserId = isAuthenticated
                ? await this.securityAuditBroker.GetUserIdAsync(securityContext: securityContext!)
                : null;

            bool includeOwnContentItemAssociations = string.IsNullOrWhiteSpace(actorUserId) is false;

            return visibleContentItemAssociations.Where(contentItemAssociation =>
                (contentItemAssociation.ApprovalStatus == ApprovalStatus.Approved
                    && contentItemAssociation.IsPublished
                    && (contentItemAssociation.PublishDate == null
                        || contentItemAssociation.PublishDate <= currentDateTime))
                || (includeOwnContentItemAssociations
                    && contentItemAssociation.CreatedBy == actorUserId));
        }

        private async ValueTask<ContentItemAssociation> DoAddContentItemAssociationAsync(
            ContentItemAssociation contentItemAssociation,
            EventEnvelope<ContentItemAssociation> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

            contentItemAssociation = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(
                    entity: contentItemAssociation,
                    securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddContentItemAssociationAsync(
                contentItemAssociation: contentItemAssociation,
                securityContext: inboundEnvelope.SecurityContext);

            ContentItemAssociation addedContentItemAssociation =
                await this.storageBroker.InsertContentItemAssociationAsync(
                    contentItemAssociation,
                    cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ContentItemAssociationOnAddingContentItemAssociationSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ContentItemAssociation> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedContentItemAssociation);

            await this.eventBroker.PublishContentItemAssociationAsync(
                envelope: outboundEnvelope,
                operation: ContentItemAssociationEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ContentItemAssociationOnAddingContentItemAssociationSubscriptionName,
                cancellationToken: cancellationToken);

            return addedContentItemAssociation;
        }

        private async ValueTask<ContentItemAssociation> DoModifyContentItemAssociationAsync(
            ContentItemAssociation contentItemAssociation,
            EventEnvelope<ContentItemAssociation> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

            contentItemAssociation = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(
                    entity: contentItemAssociation,
                    securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyContentItemAssociationAsync(
                contentItemAssociation: contentItemAssociation,
                securityContext: inboundEnvelope.SecurityContext);

            ContentItemAssociation maybeContentItemAssociation =
                await this.storageBroker.SelectContentItemAssociationByIdAsync(
                    contentItemAssociationId: contentItemAssociation.Id,
                    cancellationToken: cancellationToken);

            ValidateStorageContentItemAssociation(
                maybeContentItemAssociation,
                contentItemAssociationId: contentItemAssociation.Id);

            await ValidateUserCanModifyStorageContentItemAssociationAsync(
                storageContentItemAssociation: maybeContentItemAssociation,
                securityContext: inboundEnvelope.SecurityContext);

            contentItemAssociation = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: contentItemAssociation,
                    storageEntity: maybeContentItemAssociation);

            ValidateAgainstStorageContentItemAssociationOnModify(
                inputContentItemAssociation: contentItemAssociation,
                storageContentItemAssociation: maybeContentItemAssociation);

            ContentItemAssociation updatedContentItemAssociation =
                await this.storageBroker.UpdateContentItemAssociationAsync(
                    contentItemAssociation,
                    cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ContentItemAssociationOnModifyingContentItemAssociationSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ContentItemAssociation> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedContentItemAssociation);

            await this.eventBroker.PublishContentItemAssociationAsync(
                envelope: outboundEnvelope,
                operation: ContentItemAssociationEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ContentItemAssociationOnModifyingContentItemAssociationSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedContentItemAssociation;
        }

        private async ValueTask<ContentItemAssociation> DoRemoveContentItemAssociationByIdAsync(
            Guid contentItemAssociationId,
            string? deletionReason,
            EventEnvelope<ContentItemAssociation> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
            ValidateOnRemoveContentItemAssociationById(contentItemAssociationId);

            ContentItemAssociation maybeContentItemAssociation =
                await this.storageBroker.SelectContentItemAssociationByIdAsync(
                    contentItemAssociationId: contentItemAssociationId,
                    cancellationToken: cancellationToken);

            ValidateStorageContentItemAssociation(maybeContentItemAssociation, contentItemAssociationId);

            // permission comes before the idempotent short-circuit, so an unauthorized
            // caller learns nothing about the row's deletion state
            await ValidateUserCanRemoveStorageContentItemAssociationAsync(
                storageContentItemAssociation: maybeContentItemAssociation,
                securityContext: inboundEnvelope.SecurityContext);

            if (maybeContentItemAssociation.IsDeleted)
                return maybeContentItemAssociation;

            if (deletionReason is not null)
                maybeContentItemAssociation.DeletionReason = deletionReason;

            ContentItemAssociation auditedContentItemAssociation =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeContentItemAssociation,
                    securityContext: inboundEnvelope.SecurityContext);

            ContentItemAssociation removedContentItemAssociation =
                await this.storageBroker.UpdateContentItemAssociationAsync(
                    contentItemAssociation: auditedContentItemAssociation,
                    cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ContentItemAssociationOnRemovingContentItemAssociationByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ContentItemAssociation> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedContentItemAssociation);

            await this.eventBroker.PublishContentItemAssociationAsync(
                envelope: outboundEnvelope,
                operation: ContentItemAssociationEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ContentItemAssociationOnRemovingContentItemAssociationByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedContentItemAssociation;
        }

        private async ValueTask<ContentItemAssociation> DoHardRemoveContentItemAssociationByIdAsync(
            Guid contentItemAssociationId,
            EventEnvelope<ContentItemAssociation> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserCanHardRemoveContentItemAssociation(inboundEnvelope.SecurityContext);
            ValidateOnHardRemoveContentItemAssociationById(contentItemAssociationId);

            ContentItemAssociation maybeContentItemAssociation =
                await this.storageBroker.SelectContentItemAssociationByIdAsync(
                    contentItemAssociationId: contentItemAssociationId,
                    cancellationToken: cancellationToken);

            ValidateStorageContentItemAssociation(maybeContentItemAssociation, contentItemAssociationId);

            ContentItemAssociation deletedContentItemAssociation =
                await this.storageBroker.DeleteContentItemAssociationAsync(
                    maybeContentItemAssociation,
                    cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ContentItemAssociationOnHardRemovingContentItemAssociationByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ContentItemAssociation> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedContentItemAssociation);

            await this.eventBroker.PublishContentItemAssociationAsync(
                envelope: outboundEnvelope,
                operation: ContentItemAssociationEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ContentItemAssociationOnHardRemovingContentItemAssociationByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedContentItemAssociation;
        }
    }
}
