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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ApprovalSettings
{
    /// <summary>
    /// Foundation service for approval settings. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain. Per design §14.6 the foundation enforces security itself — every
    /// write is Admin only, and reads require an authenticated caller (any role), with
    /// soft-deleted rows answering not-found and dropping out of collection reads — never
    /// assuming an upstream orchestration already gated the caller.
    /// </summary>
    internal partial class ApprovalSettingService : IApprovalSettingService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly IEnvelopeIntegrityBroker envelopeIntegrityBroker;
        private readonly ILoggingBroker loggingBroker;

        public ApprovalSettingService(
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

        public ValueTask<ApprovalSetting> AddApprovalSettingAsync(
            ApprovalSetting approvalSetting,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalSettingIsNotNull(approvalSetting);

                EventEnvelope<ApprovalSetting> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: approvalSetting);

                return await DoAddApprovalSettingAsync(
                    approvalSetting: approvalSetting,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<ApprovalSetting>> RetrieveAllApprovalSettingsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the envelope exists to capture the ambient security context the
                // visibility filter runs against — the request payload is empty
                EventEnvelope<ApprovalSetting> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new ApprovalSetting());

                IQueryable<ApprovalSetting> allApprovalSettings =
                    await this.storageBroker.SelectAllApprovalSettingsAsync(cancellationToken);

                return ApplyCollectionReadVisibilityFilter(
                    approvalSettings: allApprovalSettings,
                    securityContext: envelope.SecurityContext);
            });

        public ValueTask<ApprovalSetting> RetrieveApprovalSettingByIdAsync(
            Guid approvalSettingId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new ApprovalSetting
                {
                    Id = approvalSettingId
                };

                EventEnvelope<ApprovalSetting> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveApprovalSettingByIdAsync(
                    approvalSettingId: approvalSettingId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalSetting> ModifyApprovalSettingAsync(
            ApprovalSetting approvalSetting,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalSettingIsNotNull(approvalSetting);

                EventEnvelope<ApprovalSetting> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: approvalSetting);

                return await DoModifyApprovalSettingAsync(
                    approvalSetting: approvalSetting,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalSetting> RemoveApprovalSettingByIdAsync(
            Guid approvalSettingId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new ApprovalSetting
                {
                    Id = approvalSettingId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<ApprovalSetting> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: removeRequest);

                return await DoRemoveApprovalSettingByIdAsync(
                    approvalSettingId: approvalSettingId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalSetting> HardRemoveApprovalSettingByIdAsync(
            Guid approvalSettingId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new ApprovalSetting
                {
                    Id = approvalSettingId
                };

                EventEnvelope<ApprovalSetting> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveApprovalSettingByIdAsync(
                    approvalSettingId: approvalSettingId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        // the shared read posture of design §14.1/§14.5/§14.6: approval settings are the
        // published rules of the submission process, so any signed-in caller may read them;
        // an anonymous caller — like a soft-deleted row — answers not-found, never
        // unauthorized, with the true denial reason logged server-side only
        private async ValueTask<ApprovalSetting> DoRetrieveApprovalSettingByIdAsync(
            Guid approvalSettingId,
            EventEnvelope<ApprovalSetting> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRetrieveApprovalSettingById(approvalSettingId);

            ApprovalSetting maybeApprovalSetting =
                await this.storageBroker.SelectApprovalSettingByIdAsync(approvalSettingId, cancellationToken);

            ValidateStorageApprovalSetting(maybeApprovalSetting, approvalSettingId);

            if (maybeApprovalSetting.IsDeleted)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Approval setting read denied. Approval setting {approvalSettingId} is " +
                        "soft-deleted; reported to the caller as not found.");

                throw new NotFoundApprovalSettingException(
                    message: $"Approval setting not found with id: {approvalSettingId}.");
            }

            SecurityContext? securityContext = inboundEnvelope.SecurityContext;

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Approval setting read denied. Approval setting {approvalSettingId} is " +
                        "visible to authenticated callers only and the caller is not authenticated; " +
                        "reported to the caller as not found.");

                throw new NotFoundApprovalSettingException(
                    message: $"Approval setting not found with id: {approvalSettingId}.");
            }

            return maybeApprovalSetting;
        }

        // the collection twin of the single-row posture: a row the caller may not see
        // drops out of the set instead of erroring, so a collection read never reveals
        // how many settings exist — an anonymous caller simply reads nothing
        private static IQueryable<ApprovalSetting> ApplyCollectionReadVisibilityFilter(
            IQueryable<ApprovalSetting> approvalSettings,
            SecurityContext? securityContext)
        {
            IQueryable<ApprovalSetting> visibleApprovalSettings = approvalSettings.Where(approvalSetting =>
                approvalSetting.IsDeleted == false);

            bool isAuthenticated =
                securityContext is not null && securityContext.IsAuthenticated;

            if (isAuthenticated is false)
            {
                return visibleApprovalSettings.Where(approvalSetting => false);
            }

            // every signed-in caller sees the whole policy — settings carry no per-row
            // ownership or approval state to narrow the set by
            return visibleApprovalSettings;
        }

        private async ValueTask<ApprovalSetting> DoAddApprovalSettingAsync(
            ApprovalSetting approvalSetting,
            EventEnvelope<ApprovalSetting> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToAdministerApprovalSettings(inboundEnvelope.SecurityContext);

            approvalSetting = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(entity: approvalSetting, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddApprovalSettingAsync(
                approvalSetting: approvalSetting,
                securityContext: inboundEnvelope.SecurityContext);

            ApprovalSetting addedApprovalSetting =
                await this.storageBroker.InsertApprovalSettingAsync(approvalSetting, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingOnAddingApprovalSettingSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalSetting> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedApprovalSetting);

            await this.eventBroker.PublishApprovalSettingAsync(
                envelope: outboundEnvelope,
                operation: ApprovalSettingEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingOnAddingApprovalSettingSubscriptionName,
                cancellationToken: cancellationToken);

            return addedApprovalSetting;
        }

        private async ValueTask<ApprovalSetting> DoModifyApprovalSettingAsync(
            ApprovalSetting approvalSetting,
            EventEnvelope<ApprovalSetting> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToAdministerApprovalSettings(inboundEnvelope.SecurityContext);

            approvalSetting = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: approvalSetting, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyApprovalSettingAsync(
                approvalSetting: approvalSetting,
                securityContext: inboundEnvelope.SecurityContext);

            ApprovalSetting maybeApprovalSetting = await this.storageBroker.SelectApprovalSettingByIdAsync(
                approvalSettingId: approvalSetting.Id,
                cancellationToken: cancellationToken);

            ValidateStorageApprovalSetting(maybeApprovalSetting, approvalSettingId: approvalSetting.Id);

            approvalSetting = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: approvalSetting,
                    storageEntity: maybeApprovalSetting);

            ValidateAgainstStorageApprovalSettingOnModify(
                inputApprovalSetting: approvalSetting,
                storageApprovalSetting: maybeApprovalSetting);

            ApprovalSetting updatedApprovalSetting =
                await this.storageBroker.UpdateApprovalSettingAsync(approvalSetting, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingOnModifyingApprovalSettingSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalSetting> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedApprovalSetting);

            await this.eventBroker.PublishApprovalSettingAsync(
                envelope: outboundEnvelope,
                operation: ApprovalSettingEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingOnModifyingApprovalSettingSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedApprovalSetting;
        }

        private async ValueTask<ApprovalSetting> DoRemoveApprovalSettingByIdAsync(
            Guid approvalSettingId,
            string? deletionReason,
            EventEnvelope<ApprovalSetting> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            // permission comes before the idempotent short-circuit, so an unauthorized
            // caller learns nothing about the row's deletion state
            ValidateUserIsAllowedToAdministerApprovalSettings(inboundEnvelope.SecurityContext);
            ValidateOnRemoveApprovalSettingById(approvalSettingId, deletionReason);

            ApprovalSetting maybeApprovalSetting =
                await this.storageBroker.SelectApprovalSettingByIdAsync(approvalSettingId, cancellationToken);

            ValidateStorageApprovalSetting(maybeApprovalSetting, approvalSettingId);

            if (maybeApprovalSetting.IsDeleted)
                return maybeApprovalSetting;

            ApprovalSetting auditedApprovalSetting =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeApprovalSetting,
                    securityContext: inboundEnvelope.SecurityContext,
                    deletionReason: deletionReason);

            ApprovalSetting removedApprovalSetting = await this.storageBroker.UpdateApprovalSettingAsync(
                approvalSetting: auditedApprovalSetting,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingOnRemovingApprovalSettingByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalSetting> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedApprovalSetting);

            await this.eventBroker.PublishApprovalSettingAsync(
                envelope: outboundEnvelope,
                operation: ApprovalSettingEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingOnRemovingApprovalSettingByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedApprovalSetting;
        }

        private async ValueTask<ApprovalSetting> DoHardRemoveApprovalSettingByIdAsync(
            Guid approvalSettingId,
            EventEnvelope<ApprovalSetting> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToAdministerApprovalSettings(inboundEnvelope.SecurityContext);
            ValidateOnHardRemoveApprovalSettingById(approvalSettingId);

            ApprovalSetting maybeApprovalSetting =
                await this.storageBroker.SelectApprovalSettingByIdAsync(approvalSettingId, cancellationToken);

            ValidateStorageApprovalSetting(maybeApprovalSetting, approvalSettingId);

            ApprovalSetting deletedApprovalSetting =
                await this.storageBroker.DeleteApprovalSettingAsync(maybeApprovalSetting, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingOnHardRemovingApprovalSettingByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalSetting> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedApprovalSetting);

            await this.eventBroker.PublishApprovalSettingAsync(
                envelope: outboundEnvelope,
                operation: ApprovalSettingEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingOnHardRemovingApprovalSettingByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedApprovalSetting;
        }
    }
}
