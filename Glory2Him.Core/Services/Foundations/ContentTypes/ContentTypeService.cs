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
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Foundations.ContentTypes.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ContentTypes
{
    /// <summary>
    /// Foundation service for content types. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain. Per design §14.6 the foundation enforces security itself — content
    /// types are reference data, so every write (including hard removal) is Admin only and
    /// the §14.1/§14.5 read visibility posture answers not-found for non-public rows to
    /// everyone but an Admin — never assuming an upstream orchestration already gated the
    /// caller.
    /// </summary>
    internal partial class ContentTypeService : IContentTypeService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public ContentTypeService(
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

        public ValueTask<ContentType> AddContentTypeAsync(
            ContentType contentType,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateContentTypeIsNotNull(contentType);

                EventEnvelope<ContentType> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: contentType);

                return await DoAddContentTypeAsync(
                    contentType: contentType,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<ContentType>> RetrieveAllContentTypesAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the envelope exists to capture the ambient security context the
                // visibility filter runs against — the request payload is empty
                EventEnvelope<ContentType> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new ContentType());

                IQueryable<ContentType> allContentTypes =
                    await this.storageBroker.SelectAllContentTypesAsync(cancellationToken);

                return await ApplyCollectionReadVisibilityFilterAsync(
                    contentTypes: allContentTypes,
                    securityContext: envelope.SecurityContext);
            });

        public ValueTask<ContentType> RetrieveContentTypeByIdAsync(
            Guid contentTypeId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new ContentType
                {
                    Id = contentTypeId
                };

                EventEnvelope<ContentType> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveContentTypeByIdAsync(
                    contentTypeId: contentTypeId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ContentType> ModifyContentTypeAsync(
            ContentType contentType,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateContentTypeIsNotNull(contentType);

                EventEnvelope<ContentType> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: contentType);

                return await DoModifyContentTypeAsync(
                    contentType: contentType,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ContentType> RemoveContentTypeByIdAsync(
            Guid contentTypeId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new ContentType
                {
                    Id = contentTypeId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<ContentType> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: removeRequest);

                return await DoRemoveContentTypeByIdAsync(
                    contentTypeId: contentTypeId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ContentType> HardRemoveContentTypeByIdAsync(
            Guid contentTypeId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new ContentType
                {
                    Id = contentTypeId
                };

                EventEnvelope<ContentType> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveContentTypeByIdAsync(
                    contentTypeId: contentTypeId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        // the shared read posture of design §14.1/§14.5/§14.6: a publicly visible content
        // type is readable by anyone; a non-public one answers not-found — never
        // unauthorized — to everyone but an Admin, with the true denial reason logged
        // server-side only (no owner branch: only admins author reference data)
        private async ValueTask<ContentType> DoRetrieveContentTypeByIdAsync(
            Guid contentTypeId,
            EventEnvelope<ContentType> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRetrieveContentTypeById(contentTypeId);

            ContentType maybeContentType = await this.storageBroker.SelectContentTypeByIdAsync(
                contentTypeId: contentTypeId,
                cancellationToken: cancellationToken);

            ValidateStorageContentType(maybeContentType, contentTypeId);

            if (maybeContentType.IsDeleted)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Content type read denied. Content type {contentTypeId} is " +
                        "soft-deleted; reported to the caller as not found.");

                throw new NotFoundContentTypeException(
                    message: $"Content type not found with id: {contentTypeId}.");
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            bool isPubliclyVisible =
                maybeContentType.ApprovalStatus == ApprovalStatus.Approved
                    && maybeContentType.IsPublished
                    && (maybeContentType.PublishDate is null
                        || maybeContentType.PublishDate <= currentDateTime);

            if (isPubliclyVisible)
            {
                return maybeContentType;
            }

            SecurityContext? securityContext = inboundEnvelope.SecurityContext;

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Content type read denied. Content type {contentTypeId} is not " +
                        "publicly visible and the caller is not authenticated; reported to " +
                        "the caller as not found.");

                throw new NotFoundContentTypeException(
                    message: $"Content type not found with id: {contentTypeId}.");
            }

            if (HasAdminRole(securityContext) is false)
            {
                string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                    securityContext: securityContext);

                await this.loggingBroker.LogWarningAsync(
                    message: $"Content type read denied. Content type {contentTypeId} " +
                        $"is not publicly visible and user \"{actorUserId}\" is not an " +
                        "Admin; reported to the caller as not found.");

                throw new NotFoundContentTypeException(
                    message: $"Content type not found with id: {contentTypeId}.");
            }

            return maybeContentType;
        }

        // the collection twin of the single-row posture: a row the caller may not see
        // drops out of the set instead of erroring, so a collection read never reveals
        // how many non-public rows exist
        private async ValueTask<IQueryable<ContentType>> ApplyCollectionReadVisibilityFilterAsync(
            IQueryable<ContentType> contentTypes,
            SecurityContext? securityContext)
        {
            IQueryable<ContentType> visibleContentTypes = contentTypes.Where(contentType =>
                contentType.IsDeleted == false);

            bool isAuthenticated =
                securityContext is not null && securityContext.IsAuthenticated;

            if (isAuthenticated && HasAdminRole(securityContext!))
            {
                return visibleContentTypes;
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            return visibleContentTypes.Where(contentType =>
                contentType.ApprovalStatus == ApprovalStatus.Approved
                    && contentType.IsPublished
                    && (contentType.PublishDate == null
                        || contentType.PublishDate <= currentDateTime));
        }

        private async ValueTask<ContentType> DoAddContentTypeAsync(
            ContentType contentType,
            EventEnvelope<ContentType> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToAdministerContentTypes(inboundEnvelope.SecurityContext);

            contentType = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(entity: contentType, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddContentTypeAsync(
                contentType: contentType,
                securityContext: inboundEnvelope.SecurityContext);

            ContentType addedContentType =
                await this.storageBroker.InsertContentTypeAsync(contentType, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentTypeOnAddingContentTypeSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ContentType> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedContentType);

            await this.eventBroker.PublishContentTypeAsync(
                envelope: outboundEnvelope,
                operation: ContentTypeEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentTypeOnAddingContentTypeSubscriptionName,
                cancellationToken: cancellationToken);

            return addedContentType;
        }

        private async ValueTask<ContentType> DoModifyContentTypeAsync(
            ContentType contentType,
            EventEnvelope<ContentType> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToAdministerContentTypes(inboundEnvelope.SecurityContext);

            contentType = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: contentType, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyContentTypeAsync(
                contentType: contentType,
                securityContext: inboundEnvelope.SecurityContext);

            ContentType maybeContentType = await this.storageBroker.SelectContentTypeByIdAsync(
                contentTypeId: contentType.Id,
                cancellationToken: cancellationToken);

            ValidateStorageContentType(maybeContentType, contentTypeId: contentType.Id);

            contentType = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: contentType,
                    storageEntity: maybeContentType);

            ValidateAgainstStorageContentTypeOnModify(
                inputContentType: contentType,
                storageContentType: maybeContentType);

            ContentType updatedContentType =
                await this.storageBroker.UpdateContentTypeAsync(contentType, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentTypeOnModifyingContentTypeSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ContentType> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedContentType);

            await this.eventBroker.PublishContentTypeAsync(
                envelope: outboundEnvelope,
                operation: ContentTypeEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentTypeOnModifyingContentTypeSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedContentType;
        }

        private async ValueTask<ContentType> DoRemoveContentTypeByIdAsync(
            Guid contentTypeId,
            string? deletionReason,
            EventEnvelope<ContentType> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            // the gate comes before the idempotent short-circuit, so an unauthorized
            // caller learns nothing about the row's deletion state
            ValidateUserIsAllowedToAdministerContentTypes(inboundEnvelope.SecurityContext);
            ValidateOnRemoveContentTypeById(contentTypeId);

            ContentType maybeContentType =
                await this.storageBroker.SelectContentTypeByIdAsync(contentTypeId, cancellationToken);

            ValidateStorageContentType(maybeContentType, contentTypeId);

            if (maybeContentType.IsDeleted)
                return maybeContentType;

            if (deletionReason is not null)
                maybeContentType.DeletionReason = deletionReason;

            ContentType auditedContentType =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeContentType,
                    securityContext: inboundEnvelope.SecurityContext);

            ContentType removedContentType = await this.storageBroker.UpdateContentTypeAsync(
                contentType: auditedContentType,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentTypeOnRemovingContentTypeByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ContentType> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedContentType);

            await this.eventBroker.PublishContentTypeAsync(
                envelope: outboundEnvelope,
                operation: ContentTypeEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentTypeOnRemovingContentTypeByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedContentType;
        }

        private async ValueTask<ContentType> DoHardRemoveContentTypeByIdAsync(
            Guid contentTypeId,
            EventEnvelope<ContentType> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserCanHardRemoveContentType(inboundEnvelope.SecurityContext);
            ValidateOnHardRemoveContentTypeById(contentTypeId);

            ContentType maybeContentType =
                await this.storageBroker.SelectContentTypeByIdAsync(contentTypeId, cancellationToken);

            ValidateStorageContentType(maybeContentType, contentTypeId);

            ContentType deletedContentType =
                await this.storageBroker.DeleteContentTypeAsync(maybeContentType, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentTypeOnHardRemovingContentTypeByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ContentType> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedContentType);

            await this.eventBroker.PublishContentTypeAsync(
                envelope: outboundEnvelope,
                operation: ContentTypeEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentTypeOnHardRemovingContentTypeByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedContentType;
        }
    }
}
