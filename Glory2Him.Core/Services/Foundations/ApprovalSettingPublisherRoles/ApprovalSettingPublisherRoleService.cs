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
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;

namespace Glory2Him.Core.Services.Foundations.ApprovalSettingPublisherRoles
{
    /// <summary>
    /// Foundation service for approval setting publisher roles. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain.
    /// </summary>
    internal partial class ApprovalSettingPublisherRoleService : IApprovalSettingPublisherRoleService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public ApprovalSettingPublisherRoleService(
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

        public ValueTask<ApprovalSettingPublisherRole> AddApprovalSettingPublisherRoleAsync(
            ApprovalSettingPublisherRole approvalSettingPublisherRole,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalSettingPublisherRoleIsNotNull(approvalSettingPublisherRole);

                EventEnvelope<ApprovalSettingPublisherRole> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: approvalSettingPublisherRole);

                return await DoAddApprovalSettingPublisherRoleAsync(
                    approvalSettingPublisherRole: approvalSettingPublisherRole,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<ApprovalSettingPublisherRole>> RetrieveAllApprovalSettingPublisherRolesAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return await this.storageBroker.SelectAllApprovalSettingPublisherRolesAsync(cancellationToken);
            });

        public ValueTask<ApprovalSettingPublisherRole> RetrieveApprovalSettingPublisherRoleByIdAsync(
            Guid approvalSettingPublisherRoleId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveApprovalSettingPublisherRoleById(approvalSettingPublisherRoleId);

                ApprovalSettingPublisherRole maybeApprovalSettingPublisherRole =
                    await this.storageBroker.SelectApprovalSettingPublisherRoleByIdAsync(approvalSettingPublisherRoleId, cancellationToken);

                ValidateStorageApprovalSettingPublisherRole(maybeApprovalSettingPublisherRole, approvalSettingPublisherRoleId);

                return maybeApprovalSettingPublisherRole;
            });

        public ValueTask<ApprovalSettingPublisherRole> ModifyApprovalSettingPublisherRoleAsync(
            ApprovalSettingPublisherRole approvalSettingPublisherRole,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalSettingPublisherRoleIsNotNull(approvalSettingPublisherRole);

                EventEnvelope<ApprovalSettingPublisherRole> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: approvalSettingPublisherRole);

                return await DoModifyApprovalSettingPublisherRoleAsync(
                    approvalSettingPublisherRole: approvalSettingPublisherRole,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalSettingPublisherRole> RemoveApprovalSettingPublisherRoleByIdAsync(
            Guid approvalSettingPublisherRoleId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new ApprovalSettingPublisherRole
                {
                    Id = approvalSettingPublisherRoleId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<ApprovalSettingPublisherRole> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: removeRequest);

                return await DoRemoveApprovalSettingPublisherRoleByIdAsync(
                    approvalSettingPublisherRoleId: approvalSettingPublisherRoleId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalSettingPublisherRole> HardRemoveApprovalSettingPublisherRoleByIdAsync(
            Guid approvalSettingPublisherRoleId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new ApprovalSettingPublisherRole
                {
                    Id = approvalSettingPublisherRoleId
                };

                EventEnvelope<ApprovalSettingPublisherRole> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveApprovalSettingPublisherRoleByIdAsync(
                    approvalSettingPublisherRoleId: approvalSettingPublisherRoleId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<ApprovalSettingPublisherRole> DoAddApprovalSettingPublisherRoleAsync(
            ApprovalSettingPublisherRole approvalSettingPublisherRole,
            EventEnvelope<ApprovalSettingPublisherRole> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            approvalSettingPublisherRole = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(entity: approvalSettingPublisherRole, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddApprovalSettingPublisherRoleAsync(
                approvalSettingPublisherRole: approvalSettingPublisherRole,
                securityContext: inboundEnvelope.SecurityContext);

            ApprovalSettingPublisherRole addedApprovalSettingPublisherRole =
                await this.storageBroker.InsertApprovalSettingPublisherRoleAsync(approvalSettingPublisherRole, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnAddingApprovalSettingPublisherRoleSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalSettingPublisherRole> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedApprovalSettingPublisherRole);

            await this.eventBroker.PublishApprovalSettingPublisherRoleAsync(
                envelope: outboundEnvelope,
                operation: ApprovalSettingPublisherRoleEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnAddingApprovalSettingPublisherRoleSubscriptionName,
                cancellationToken: cancellationToken);

            return addedApprovalSettingPublisherRole;
        }

        private async ValueTask<ApprovalSettingPublisherRole> DoModifyApprovalSettingPublisherRoleAsync(
            ApprovalSettingPublisherRole approvalSettingPublisherRole,
            EventEnvelope<ApprovalSettingPublisherRole> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            approvalSettingPublisherRole = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: approvalSettingPublisherRole, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyApprovalSettingPublisherRoleAsync(
                approvalSettingPublisherRole: approvalSettingPublisherRole,
                securityContext: inboundEnvelope.SecurityContext);

            ApprovalSettingPublisherRole maybeApprovalSettingPublisherRole = await this.storageBroker.SelectApprovalSettingPublisherRoleByIdAsync(
                approvalSettingPublisherRoleId: approvalSettingPublisherRole.Id,
                cancellationToken: cancellationToken);

            ValidateStorageApprovalSettingPublisherRole(maybeApprovalSettingPublisherRole, approvalSettingPublisherRoleId: approvalSettingPublisherRole.Id);

            approvalSettingPublisherRole = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: approvalSettingPublisherRole,
                    storageEntity: maybeApprovalSettingPublisherRole);

            ValidateAgainstStorageApprovalSettingPublisherRoleOnModify(
                inputApprovalSettingPublisherRole: approvalSettingPublisherRole,
                storageApprovalSettingPublisherRole: maybeApprovalSettingPublisherRole);

            ApprovalSettingPublisherRole updatedApprovalSettingPublisherRole =
                await this.storageBroker.UpdateApprovalSettingPublisherRoleAsync(approvalSettingPublisherRole, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalSettingPublisherRole> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedApprovalSettingPublisherRole);

            await this.eventBroker.PublishApprovalSettingPublisherRoleAsync(
                envelope: outboundEnvelope,
                operation: ApprovalSettingPublisherRoleEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedApprovalSettingPublisherRole;
        }

        private async ValueTask<ApprovalSettingPublisherRole> DoRemoveApprovalSettingPublisherRoleByIdAsync(
            Guid approvalSettingPublisherRoleId,
            string? deletionReason,
            EventEnvelope<ApprovalSettingPublisherRole> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRemoveApprovalSettingPublisherRoleById(approvalSettingPublisherRoleId);

            ApprovalSettingPublisherRole maybeApprovalSettingPublisherRole =
                await this.storageBroker.SelectApprovalSettingPublisherRoleByIdAsync(approvalSettingPublisherRoleId, cancellationToken);

            ValidateStorageApprovalSettingPublisherRole(maybeApprovalSettingPublisherRole, approvalSettingPublisherRoleId);

            if (maybeApprovalSettingPublisherRole.IsDeleted)
                return maybeApprovalSettingPublisherRole;

            if (deletionReason is not null)
                maybeApprovalSettingPublisherRole.DeletionReason = deletionReason;

            ApprovalSettingPublisherRole auditedApprovalSettingPublisherRole =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeApprovalSettingPublisherRole,
                    securityContext: inboundEnvelope.SecurityContext);

            ApprovalSettingPublisherRole removedApprovalSettingPublisherRole = await this.storageBroker.UpdateApprovalSettingPublisherRoleAsync(
                approvalSettingPublisherRole: auditedApprovalSettingPublisherRole,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnRemovingApprovalSettingPublisherRoleByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalSettingPublisherRole> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedApprovalSettingPublisherRole);

            await this.eventBroker.PublishApprovalSettingPublisherRoleAsync(
                envelope: outboundEnvelope,
                operation: ApprovalSettingPublisherRoleEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnRemovingApprovalSettingPublisherRoleByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedApprovalSettingPublisherRole;
        }

        private async ValueTask<ApprovalSettingPublisherRole> DoHardRemoveApprovalSettingPublisherRoleByIdAsync(
            Guid approvalSettingPublisherRoleId,
            EventEnvelope<ApprovalSettingPublisherRole> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnHardRemoveApprovalSettingPublisherRoleById(approvalSettingPublisherRoleId);

            ApprovalSettingPublisherRole maybeApprovalSettingPublisherRole =
                await this.storageBroker.SelectApprovalSettingPublisherRoleByIdAsync(approvalSettingPublisherRoleId, cancellationToken);

            ValidateStorageApprovalSettingPublisherRole(maybeApprovalSettingPublisherRole, approvalSettingPublisherRoleId);

            ApprovalSettingPublisherRole deletedApprovalSettingPublisherRole =
                await this.storageBroker.DeleteApprovalSettingPublisherRoleAsync(maybeApprovalSettingPublisherRole, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnHardRemovingApprovalSettingPublisherRoleByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalSettingPublisherRole> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedApprovalSettingPublisherRole);

            await this.eventBroker.PublishApprovalSettingPublisherRoleAsync(
                envelope: outboundEnvelope,
                operation: ApprovalSettingPublisherRoleEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnHardRemovingApprovalSettingPublisherRoleByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedApprovalSettingPublisherRole;
        }
    }
}
