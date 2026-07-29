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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;

namespace Glory2Him.Core.Services.Foundations.ContentItemAssociations
{
    /// <summary>
    /// Foundation service for content item associations. Every operation is both callable
    /// directly (the non-event path: object in → request envelope → shared do-work) and
    /// reachable through the event substrate (the event path in the <c>.Substrate</c> partial:
    /// request envelope in → shared do-work). The private <c>DoXAsync</c> methods own auditing,
    /// validation, storage, and publishing the past-tense fact, so the two paths cannot
    /// diverge; the inbound envelope carries the original caller's <c>SecurityContext</c> and
    /// anchors the causation chain.
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

                return await this.storageBroker.SelectAllContentItemAssociationsAsync(cancellationToken);
            });

        public ValueTask<ContentItemAssociation> RetrieveContentItemAssociationByIdAsync(
            Guid contentItemAssociationId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveContentItemAssociationById(contentItemAssociationId);

                ContentItemAssociation maybeContentItemAssociation =
                    await this.storageBroker.SelectContentItemAssociationByIdAsync(
                        contentItemAssociationId: contentItemAssociationId,
                        cancellationToken: cancellationToken);

                ValidateStorageContentItemAssociation(maybeContentItemAssociation, contentItemAssociationId);

                return maybeContentItemAssociation;
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

        private async ValueTask<ContentItemAssociation> DoAddContentItemAssociationAsync(
            ContentItemAssociation contentItemAssociation,
            EventEnvelope<ContentItemAssociation> inboundEnvelope,
            CancellationToken cancellationToken)
        {
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
            ValidateOnRemoveContentItemAssociationById(contentItemAssociationId);

            ContentItemAssociation maybeContentItemAssociation =
                await this.storageBroker.SelectContentItemAssociationByIdAsync(
                    contentItemAssociationId: contentItemAssociationId,
                    cancellationToken: cancellationToken);

            ValidateStorageContentItemAssociation(maybeContentItemAssociation, contentItemAssociationId);

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
