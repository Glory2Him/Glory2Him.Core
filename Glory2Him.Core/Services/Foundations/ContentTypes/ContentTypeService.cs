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
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Factories.Events;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ContentTypes;

namespace Glory2Him.Core.Services.Foundations.ContentTypes
{
    /// <summary>
    /// Foundation service for content types. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain.
    /// </summary>
    public partial class ContentTypeService : IContentTypeService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeFactory eventEnvelopeFactory;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public ContentTypeService(
            IStorageBroker storageBroker,
            IDateTimeBroker dateTimeBroker,
            IIdentifierBroker identifierBroker,
            IEventBroker eventBroker,
            IEventEnvelopeFactory eventEnvelopeFactory,
            ISecurityAuditBroker securityAuditBroker,
            ILoggingBroker loggingBroker)
        {
            this.storageBroker = storageBroker;
            this.dateTimeBroker = dateTimeBroker;
            this.identifierBroker = identifierBroker;
            this.eventBroker = eventBroker;
            this.eventEnvelopeFactory = eventEnvelopeFactory;
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
                    await this.eventEnvelopeFactory.CreateAsync(content: contentType);

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

                return await this.storageBroker.SelectAllContentTypesAsync(cancellationToken);
            });

        public ValueTask<ContentType> RetrieveContentTypeByIdAsync(
            Guid contentTypeId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveContentTypeById(contentTypeId);

                ContentType maybeContentType =
                    await this.storageBroker.SelectContentTypeByIdAsync(contentTypeId, cancellationToken);

                ValidateStorageContentType(maybeContentType, contentTypeId);

                return maybeContentType;
            });

        public ValueTask<ContentType> ModifyContentTypeAsync(
            ContentType contentType,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateContentTypeIsNotNull(contentType);

                EventEnvelope<ContentType> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: contentType);

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
                    await this.eventEnvelopeFactory.CreateAsync(content: removeRequest);

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
                    await this.eventEnvelopeFactory.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveContentTypeByIdAsync(
                    contentTypeId: contentTypeId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<ContentType> DoAddContentTypeAsync(
            ContentType contentType,
            EventEnvelope<ContentType> inboundEnvelope,
            CancellationToken cancellationToken)
        {
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
                await this.eventEnvelopeFactory.CreateNextAsync(
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
                await this.eventEnvelopeFactory.CreateNextAsync(
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
                await this.eventEnvelopeFactory.CreateNextAsync(
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
                await this.eventEnvelopeFactory.CreateNextAsync(
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
