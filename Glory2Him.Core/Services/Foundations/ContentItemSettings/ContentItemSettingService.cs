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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;

namespace Glory2Him.Core.Services.Foundations.ContentItemSettings
{
    /// <summary>
    /// Foundation service for content item settings. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain.
    /// </summary>
    internal partial class ContentItemSettingService : IContentItemSettingService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public ContentItemSettingService(
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

        public ValueTask<ContentItemSetting> AddContentItemSettingAsync(
            ContentItemSetting contentItemSetting,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateContentItemSettingIsNotNull(contentItemSetting);

                EventEnvelope<ContentItemSetting> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: contentItemSetting);

                return await DoAddContentItemSettingAsync(
                    contentItemSetting: contentItemSetting,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<ContentItemSetting>> RetrieveAllContentItemSettingsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return await this.storageBroker.SelectAllContentItemSettingsAsync(cancellationToken);
            });

        public ValueTask<ContentItemSetting> RetrieveContentItemSettingByIdAsync(
            Guid contentItemSettingId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveContentItemSettingById(contentItemSettingId);

                ContentItemSetting maybeContentItemSetting =
                    await this.storageBroker.SelectContentItemSettingByIdAsync(contentItemSettingId, cancellationToken);

                ValidateStorageContentItemSetting(maybeContentItemSetting, contentItemSettingId);

                return maybeContentItemSetting;
            });

        public ValueTask<ContentItemSetting> ModifyContentItemSettingAsync(
            ContentItemSetting contentItemSetting,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateContentItemSettingIsNotNull(contentItemSetting);

                EventEnvelope<ContentItemSetting> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: contentItemSetting);

                return await DoModifyContentItemSettingAsync(
                    contentItemSetting: contentItemSetting,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ContentItemSetting> RemoveContentItemSettingByIdAsync(
            Guid contentItemSettingId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new ContentItemSetting
                {
                    Id = contentItemSettingId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<ContentItemSetting> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: removeRequest);

                return await DoRemoveContentItemSettingByIdAsync(
                    contentItemSettingId: contentItemSettingId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ContentItemSetting> HardRemoveContentItemSettingByIdAsync(
            Guid contentItemSettingId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new ContentItemSetting
                {
                    Id = contentItemSettingId
                };

                EventEnvelope<ContentItemSetting> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveContentItemSettingByIdAsync(
                    contentItemSettingId: contentItemSettingId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<ContentItemSetting> DoAddContentItemSettingAsync(
            ContentItemSetting contentItemSetting,
            EventEnvelope<ContentItemSetting> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            contentItemSetting = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(entity: contentItemSetting, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddContentItemSettingAsync(
                contentItemSetting: contentItemSetting,
                securityContext: inboundEnvelope.SecurityContext);

            ContentItemSetting addedContentItemSetting =
                await this.storageBroker.InsertContentItemSettingAsync(contentItemSetting, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentItemSettingOnAddingContentItemSettingSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ContentItemSetting> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedContentItemSetting);

            await this.eventBroker.PublishContentItemSettingAsync(
                envelope: outboundEnvelope,
                operation: ContentItemSettingEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentItemSettingOnAddingContentItemSettingSubscriptionName,
                cancellationToken: cancellationToken);

            return addedContentItemSetting;
        }

        private async ValueTask<ContentItemSetting> DoModifyContentItemSettingAsync(
            ContentItemSetting contentItemSetting,
            EventEnvelope<ContentItemSetting> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            contentItemSetting = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(
                    entity: contentItemSetting,
                    securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyContentItemSettingAsync(
                contentItemSetting: contentItemSetting,
                securityContext: inboundEnvelope.SecurityContext);

            ContentItemSetting maybeContentItemSetting = await this.storageBroker.SelectContentItemSettingByIdAsync(
                contentItemSettingId: contentItemSetting.Id,
                cancellationToken: cancellationToken);

            ValidateStorageContentItemSetting(maybeContentItemSetting, contentItemSettingId: contentItemSetting.Id);

            contentItemSetting = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: contentItemSetting,
                    storageEntity: maybeContentItemSetting);

            ValidateAgainstStorageContentItemSettingOnModify(
                inputContentItemSetting: contentItemSetting,
                storageContentItemSetting: maybeContentItemSetting);

            ContentItemSetting updatedContentItemSetting =
                await this.storageBroker.UpdateContentItemSettingAsync(contentItemSetting, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentItemSettingOnModifyingContentItemSettingSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ContentItemSetting> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedContentItemSetting);

            await this.eventBroker.PublishContentItemSettingAsync(
                envelope: outboundEnvelope,
                operation: ContentItemSettingEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentItemSettingOnModifyingContentItemSettingSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedContentItemSetting;
        }

        private async ValueTask<ContentItemSetting> DoRemoveContentItemSettingByIdAsync(
            Guid contentItemSettingId,
            string? deletionReason,
            EventEnvelope<ContentItemSetting> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRemoveContentItemSettingById(contentItemSettingId);

            ContentItemSetting maybeContentItemSetting =
                await this.storageBroker.SelectContentItemSettingByIdAsync(contentItemSettingId, cancellationToken);

            ValidateStorageContentItemSetting(maybeContentItemSetting, contentItemSettingId);

            if (maybeContentItemSetting.IsDeleted)
                return maybeContentItemSetting;

            if (deletionReason is not null)
                maybeContentItemSetting.DeletionReason = deletionReason;

            ContentItemSetting auditedContentItemSetting =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeContentItemSetting,
                    securityContext: inboundEnvelope.SecurityContext);

            ContentItemSetting removedContentItemSetting = await this.storageBroker.UpdateContentItemSettingAsync(
                contentItemSetting: auditedContentItemSetting,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentItemSettingOnRemovingContentItemSettingByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ContentItemSetting> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedContentItemSetting);

            await this.eventBroker.PublishContentItemSettingAsync(
                envelope: outboundEnvelope,
                operation: ContentItemSettingEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ContentItemSettingOnRemovingContentItemSettingByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedContentItemSetting;
        }

        private async ValueTask<ContentItemSetting> DoHardRemoveContentItemSettingByIdAsync(
            Guid contentItemSettingId,
            EventEnvelope<ContentItemSetting> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnHardRemoveContentItemSettingById(contentItemSettingId);

            ContentItemSetting maybeContentItemSetting =
                await this.storageBroker.SelectContentItemSettingByIdAsync(contentItemSettingId, cancellationToken);

            ValidateStorageContentItemSetting(maybeContentItemSetting, contentItemSettingId);

            ContentItemSetting deletedContentItemSetting =
                await this.storageBroker.DeleteContentItemSettingAsync(maybeContentItemSetting, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ContentItemSettingOnHardRemovingContentItemSettingByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ContentItemSetting> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedContentItemSetting);

            await this.eventBroker.PublishContentItemSettingAsync(
                envelope: outboundEnvelope,
                operation: ContentItemSettingEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ContentItemSettingOnHardRemovingContentItemSettingByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedContentItemSetting;
        }
    }
}
