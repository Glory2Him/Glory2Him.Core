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
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Factories.Events;
using Glory2Him.Core.Models.Events;
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

                EventEnvelope<ContentType> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(contentType);

                return await DoAddContentTypeAsync(contentType, envelope, cancellationToken);
            });

        public ValueTask<IQueryable<ContentType>> RetrieveAllContentTypesAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return await this.storageBroker.SelectAllContentTypesAsync();
            });

        public async ValueTask<ContentType> RetrieveContentTypeByIdAsync(
            Guid contentTypeId,
            CancellationToken cancellationToken = default) =>
            await TryCatch(async () =>
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

                EventEnvelope<ContentType> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(contentType);

                return await DoModifyContentTypeAsync(contentType, envelope, cancellationToken);
            });

        public ValueTask<ContentType> RemoveContentTypeByIdAsync(
            Guid contentTypeId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the request payload is the remove instruction: the id and optional reason
                var removeRequest = new ContentType
                {
                    Id = contentTypeId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<ContentType> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(removeRequest);

                return await DoRemoveContentTypeByIdAsync(
                    contentTypeId,
                    deletionReason,
                    envelope,
                    cancellationToken);
            });

        public ValueTask<ContentType> HardRemoveContentTypeByIdAsync(
            Guid contentTypeId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveContentTypeById(contentTypeId);

                ContentType maybeContentType =
                    await this.storageBroker.SelectContentTypeByIdAsync(contentTypeId, cancellationToken);

                ValidateStorageContentType(maybeContentType, contentTypeId);

                EventEnvelope<ContentType> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(maybeContentType);

                return await DoHardRemoveContentTypeAsync(maybeContentType, envelope, cancellationToken);
            });

        private async ValueTask<ContentType> DoAddContentTypeAsync(
            ContentType contentType,
            EventEnvelope<ContentType> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            contentType = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(contentType, inboundEnvelope.SecurityContext);

            await ValidateOnAddContentTypeAsync(contentType, inboundEnvelope.SecurityContext);

            ContentType addedContentType =
                await this.storageBroker.InsertContentTypeAsync(contentType, cancellationToken);

            EventEnvelope<ContentType> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(inboundEnvelope, addedContentType);

            await this.eventBroker.PublishContentTypeAsync(
                outboundEnvelope,
                ContentTypeEventOperation.Added);

            return addedContentType;
        }

        private async ValueTask<ContentType> DoModifyContentTypeAsync(
            ContentType contentType,
            EventEnvelope<ContentType> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            contentType = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(contentType, inboundEnvelope.SecurityContext);

            await ValidateOnModifyContentTypeAsync(contentType, inboundEnvelope.SecurityContext);

            ContentType maybeContentType =
                await this.storageBroker.SelectContentTypeByIdAsync(contentType.Id, cancellationToken);

            ValidateStorageContentType(maybeContentType, contentType.Id);

            contentType = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(contentType, maybeContentType);

            ValidateAgainstStorageContentTypeOnModify(
                inputContentType: contentType,
                storageContentType: maybeContentType);

            ContentType updatedContentType =
                await this.storageBroker.UpdateContentTypeAsync(contentType, cancellationToken);

            EventEnvelope<ContentType> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(inboundEnvelope, updatedContentType);

            await this.eventBroker.PublishContentTypeAsync(
                outboundEnvelope,
                ContentTypeEventOperation.Modified);

            return updatedContentType;
        }

        private async ValueTask<ContentType> DoRemoveContentTypeByIdAsync(
            Guid contentTypeId,
            string? deletionReason,
            EventEnvelope<ContentType> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRetrieveContentTypeById(contentTypeId);

            ContentType maybeContentType =
                await this.storageBroker.SelectContentTypeByIdAsync(contentTypeId, cancellationToken);

            ValidateStorageContentType(maybeContentType, contentTypeId);

            if (maybeContentType.IsDeleted)
                return maybeContentType;

            if (deletionReason is not null)
                maybeContentType.DeletionReason = deletionReason;

            ContentType auditedContentType =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(maybeContentType, inboundEnvelope.SecurityContext);

            ContentType removedContentType =
                await this.storageBroker.UpdateContentTypeAsync(auditedContentType, cancellationToken);

            EventEnvelope<ContentType> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(inboundEnvelope, removedContentType);

            await this.eventBroker.PublishContentTypeAsync(
                outboundEnvelope,
                ContentTypeEventOperation.Removed);

            return removedContentType;
        }

        private async ValueTask<ContentType> DoHardRemoveContentTypeAsync(
            ContentType contentType,
            EventEnvelope<ContentType> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ContentType deletedContentType =
                await this.storageBroker.DeleteContentTypeAsync(contentType, cancellationToken);

            EventEnvelope<ContentType> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(inboundEnvelope, deletedContentType);

            await this.eventBroker.PublishContentTypeAsync(
                outboundEnvelope,
                ContentTypeEventOperation.Removed);

            return deletedContentType;
        }
    }
}
