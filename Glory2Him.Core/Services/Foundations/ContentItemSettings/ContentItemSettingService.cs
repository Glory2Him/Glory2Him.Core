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
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.ContentItemSettings.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ContentItemSettings
{
    /// <summary>
    /// Foundation service for content item settings. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain. Per design §14.6 the foundation enforces security itself — content
    /// item settings are administrator-authored display configuration, so every write
    /// (including hard removal) is Administrators only, while reads are public: the settings drive
    /// anonymous page rendering, so every non-deleted row is readable by anyone and only a
    /// soft-deleted row answers not-found — never assuming an upstream orchestration
    /// already gated the caller.
    ///
    /// <para>One row shape is exempt from removal entirely. Every content type must always have a
    /// live default — the row where <c>ContentItemId</c> is null — so both removal paths refuse
    /// one, soft and hard alike (design §12.5.2 business rule 5). Overrides stay freely
    /// removable.</para>
    /// </summary>
    internal partial class ContentItemSettingService : IContentItemSettingService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly IEnvelopeIntegrityBroker envelopeIntegrityBroker;
        private readonly ILoggingBroker loggingBroker;

        public ContentItemSettingService(
            IStorageBroker storageBroker,
            IDateTimeBroker dateTimeBroker,
            IIdentifierBroker identifierBroker,
            IEventBroker eventBroker,
            IEventEnvelopeBroker eventEnvelopeBroker,
            ISecurityAuditBroker securityAuditBroker,
            IEnvelopeIntegrityBroker envelopeIntegrityBroker,
            ILoggingBroker loggingBroker)
        {
            this.storageBroker = storageBroker;
            this.dateTimeBroker = dateTimeBroker;
            this.identifierBroker = identifierBroker;
            this.eventBroker = eventBroker;
            this.eventEnvelopeBroker = eventEnvelopeBroker;
            this.securityAuditBroker = securityAuditBroker;
            this.envelopeIntegrityBroker = envelopeIntegrityBroker;
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

                IQueryable<ContentItemSetting> allContentItemSettings =
                    await this.storageBroker.SelectAllContentItemSettingsAsync(cancellationToken);

                return ApplyCollectionReadVisibilityFilter(contentItemSettings: allContentItemSettings);
            });

        public ValueTask<ContentItemSetting> RetrieveContentItemSettingByIdAsync(
            Guid contentItemSettingId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return await DoRetrieveContentItemSettingByIdAsync(
                    contentItemSettingId: contentItemSettingId,
                    cancellationToken: cancellationToken);
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

        // the shared read posture of design §14.1/§14.5/§14.6, shared by the direct and the
        // event path: a content item setting configures how a page renders for anonymous
        // visitors, so every non-deleted row is public and the posture takes no security
        // context at all — only a soft-deleted row is denied, and it answers not-found —
        // never unauthorized — to everyone including an administrator, with the true denial reason
        // logged server-side only
        private async ValueTask<ContentItemSetting> DoRetrieveContentItemSettingByIdAsync(
            Guid contentItemSettingId,
            CancellationToken cancellationToken)
        {
            ValidateOnRetrieveContentItemSettingById(contentItemSettingId);

            ContentItemSetting maybeContentItemSetting = await this.storageBroker.SelectContentItemSettingByIdAsync(
                contentItemSettingId: contentItemSettingId,
                cancellationToken: cancellationToken);

            ValidateStorageContentItemSetting(maybeContentItemSetting, contentItemSettingId);

            if (maybeContentItemSetting.IsDeleted)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Content item setting read denied. Content item setting {contentItemSettingId} is " +
                        "soft-deleted; reported to the caller as not found.");

                throw new NotFoundContentItemSettingException(
                    message: $"Content item setting not found with id: {contentItemSettingId}.");
            }

            return maybeContentItemSetting;
        }

        // the collection twin of the single-row posture: a row the caller may not see drops
        // out of the set instead of erroring. Every caller sees the same set here — public
        // settings for everyone — so only the soft-deleted rows are filtered away
        private static IQueryable<ContentItemSetting> ApplyCollectionReadVisibilityFilter(
            IQueryable<ContentItemSetting> contentItemSettings) =>
            contentItemSettings.Where(contentItemSetting =>
                contentItemSetting.IsDeleted == false);

        private async ValueTask<ContentItemSetting> DoAddContentItemSettingAsync(
            ContentItemSetting contentItemSetting,
            EventEnvelope<ContentItemSetting> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserMayWriteContentItemSettings(inboundEnvelope.SecurityContext);

            // Asked before the row-shaped gate so a null payload is reported as one, rather than
            // being read as a scope-less row and refused as a permission problem. The public
            // entry point already asks it; the event path reaches here without having done so.
            ValidateContentItemSettingIsNotNull(contentItemSetting);

            // Nothing is stored yet, so the row in hand IS the row being authored — the one case
            // where the caller's copy is the right thing to decide against.
            ValidateUserMayWriteContentItemSettingScope(
                securityContext: inboundEnvelope.SecurityContext,
                contentItemId: contentItemSetting.ContentItemId,
                contentType: contentItemSetting.ContentType);

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
            ValidateUserMayWriteContentItemSettings(inboundEnvelope.SecurityContext);

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

            // THE STORED ROW DECIDES, never the caller's copy: whether this is a default or an
            // override is a fact about what is already there, and a caller who could answer it
            // for themselves could promote their own override into a default they may not write.
            // The scope is pinned below as well, so the two can never disagree — but the gate
            // does not depend on that pin holding.
            ValidateUserMayWriteContentItemSettingScope(
                securityContext: inboundEnvelope.SecurityContext,
                contentItemId: maybeContentItemSetting.ContentItemId,
                contentType: maybeContentItemSetting.ContentType);

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
            // the gate comes before the idempotent short-circuit, so an unauthorized
            // caller learns nothing about the row's deletion state
            ValidateUserMayWriteContentItemSettings(inboundEnvelope.SecurityContext);
            ValidateOnRemoveContentItemSettingById(contentItemSettingId, deletionReason);

            ContentItemSetting maybeContentItemSetting =
                await this.storageBroker.SelectContentItemSettingByIdAsync(contentItemSettingId, cancellationToken);

            ValidateStorageContentItemSetting(maybeContentItemSetting, contentItemSettingId);

            // the tier refusal comes before the idempotent short-circuit, so the caller is told
            // the rule rather than handed a silent success on a row that may never be removed
            ValidateStorageContentItemSettingIsNotADefault(maybeContentItemSetting);

            // Asked after the not-a-default rule, which refuses a default to EVERYONE — so what
            // is left for this to decide is whether the caller may remove an override of THIS
            // content type. Called rather than inlined so one rule lives in one place.
            ValidateUserMayWriteContentItemSettingScope(
                securityContext: inboundEnvelope.SecurityContext,
                contentItemId: maybeContentItemSetting.ContentItemId,
                contentType: maybeContentItemSetting.ContentType);

            if (maybeContentItemSetting.IsDeleted)
                return maybeContentItemSetting;

            ContentItemSetting auditedContentItemSetting =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeContentItemSetting,
                    securityContext: inboundEnvelope.SecurityContext,
                    deletionReason: deletionReason);

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
            ValidateUserCanHardRemoveContentItemSetting(inboundEnvelope.SecurityContext);
            ValidateOnHardRemoveContentItemSettingById(contentItemSettingId);

            ContentItemSetting maybeContentItemSetting =
                await this.storageBroker.SelectContentItemSettingByIdAsync(contentItemSettingId, cancellationToken);

            ValidateStorageContentItemSetting(maybeContentItemSetting, contentItemSettingId);
            ValidateStorageContentItemSettingIsNotADefault(maybeContentItemSetting);

            ValidateUserMayWriteContentItemSettingScope(
                securityContext: inboundEnvelope.SecurityContext,
                contentItemId: maybeContentItemSetting.ContentItemId,
                contentType: maybeContentItemSetting.ContentType);

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
