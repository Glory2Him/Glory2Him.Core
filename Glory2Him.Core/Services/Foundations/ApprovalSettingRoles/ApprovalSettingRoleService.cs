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
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;

namespace Glory2Him.Core.Services.Foundations.ApprovalSettingRoles
{
    /// <summary>
    /// Foundation service for approval setting roles. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain.
    /// </summary>
    public partial class ApprovalSettingRoleService : IApprovalSettingRoleService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeFactory eventEnvelopeFactory;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public ApprovalSettingRoleService(
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

        public ValueTask<ApprovalSettingRole> AddApprovalSettingRoleAsync(
            ApprovalSettingRole approvalSettingRole,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalSettingRoleIsNotNull(approvalSettingRole);

                EventEnvelope<ApprovalSettingRole> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: approvalSettingRole);

                return await DoAddApprovalSettingRoleAsync(
                    approvalSettingRole: approvalSettingRole,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<ApprovalSettingRole>> RetrieveAllApprovalSettingRolesAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return await this.storageBroker.SelectAllApprovalSettingRolesAsync(cancellationToken);
            });

        public ValueTask<ApprovalSettingRole> RetrieveApprovalSettingRoleByIdAsync(
            Guid approvalSettingRoleId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveApprovalSettingRoleById(approvalSettingRoleId);

                ApprovalSettingRole maybeApprovalSettingRole =
                    await this.storageBroker.SelectApprovalSettingRoleByIdAsync(approvalSettingRoleId, cancellationToken);

                ValidateStorageApprovalSettingRole(maybeApprovalSettingRole, approvalSettingRoleId);

                return maybeApprovalSettingRole;
            });

        public ValueTask<ApprovalSettingRole> ModifyApprovalSettingRoleAsync(
            ApprovalSettingRole approvalSettingRole,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalSettingRoleIsNotNull(approvalSettingRole);

                EventEnvelope<ApprovalSettingRole> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: approvalSettingRole);

                return await DoModifyApprovalSettingRoleAsync(
                    approvalSettingRole: approvalSettingRole,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalSettingRole> RemoveApprovalSettingRoleByIdAsync(
            Guid approvalSettingRoleId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new ApprovalSettingRole
                {
                    Id = approvalSettingRoleId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<ApprovalSettingRole> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: removeRequest);

                return await DoRemoveApprovalSettingRoleByIdAsync(
                    approvalSettingRoleId: approvalSettingRoleId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalSettingRole> HardRemoveApprovalSettingRoleByIdAsync(
            Guid approvalSettingRoleId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new ApprovalSettingRole
                {
                    Id = approvalSettingRoleId
                };

                EventEnvelope<ApprovalSettingRole> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveApprovalSettingRoleByIdAsync(
                    approvalSettingRoleId: approvalSettingRoleId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<ApprovalSettingRole> DoAddApprovalSettingRoleAsync(
            ApprovalSettingRole approvalSettingRole,
            EventEnvelope<ApprovalSettingRole> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            approvalSettingRole = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(entity: approvalSettingRole, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddApprovalSettingRoleAsync(
                approvalSettingRole: approvalSettingRole,
                securityContext: inboundEnvelope.SecurityContext);

            ApprovalSettingRole addedApprovalSettingRole =
                await this.storageBroker.InsertApprovalSettingRoleAsync(approvalSettingRole, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingRoleOnAddingApprovalSettingRoleSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalSettingRole> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedApprovalSettingRole);

            await this.eventBroker.PublishApprovalSettingRoleAsync(
                envelope: outboundEnvelope,
                operation: ApprovalSettingRoleEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingRoleOnAddingApprovalSettingRoleSubscriptionName,
                cancellationToken: cancellationToken);

            return addedApprovalSettingRole;
        }

        private async ValueTask<ApprovalSettingRole> DoModifyApprovalSettingRoleAsync(
            ApprovalSettingRole approvalSettingRole,
            EventEnvelope<ApprovalSettingRole> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            approvalSettingRole = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: approvalSettingRole, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyApprovalSettingRoleAsync(
                approvalSettingRole: approvalSettingRole,
                securityContext: inboundEnvelope.SecurityContext);

            ApprovalSettingRole maybeApprovalSettingRole = await this.storageBroker.SelectApprovalSettingRoleByIdAsync(
                approvalSettingRoleId: approvalSettingRole.Id,
                cancellationToken: cancellationToken);

            ValidateStorageApprovalSettingRole(maybeApprovalSettingRole, approvalSettingRoleId: approvalSettingRole.Id);

            approvalSettingRole = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: approvalSettingRole,
                    storageEntity: maybeApprovalSettingRole);

            ValidateAgainstStorageApprovalSettingRoleOnModify(
                inputApprovalSettingRole: approvalSettingRole,
                storageApprovalSettingRole: maybeApprovalSettingRole);

            ApprovalSettingRole updatedApprovalSettingRole =
                await this.storageBroker.UpdateApprovalSettingRoleAsync(approvalSettingRole, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingRoleOnModifyingApprovalSettingRoleSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalSettingRole> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedApprovalSettingRole);

            await this.eventBroker.PublishApprovalSettingRoleAsync(
                envelope: outboundEnvelope,
                operation: ApprovalSettingRoleEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingRoleOnModifyingApprovalSettingRoleSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedApprovalSettingRole;
        }

        private async ValueTask<ApprovalSettingRole> DoRemoveApprovalSettingRoleByIdAsync(
            Guid approvalSettingRoleId,
            string? deletionReason,
            EventEnvelope<ApprovalSettingRole> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRemoveApprovalSettingRoleById(approvalSettingRoleId);

            ApprovalSettingRole maybeApprovalSettingRole =
                await this.storageBroker.SelectApprovalSettingRoleByIdAsync(approvalSettingRoleId, cancellationToken);

            ValidateStorageApprovalSettingRole(maybeApprovalSettingRole, approvalSettingRoleId);

            if (maybeApprovalSettingRole.IsDeleted)
                return maybeApprovalSettingRole;

            if (deletionReason is not null)
                maybeApprovalSettingRole.DeletionReason = deletionReason;

            ApprovalSettingRole auditedApprovalSettingRole =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeApprovalSettingRole,
                    securityContext: inboundEnvelope.SecurityContext);

            ApprovalSettingRole removedApprovalSettingRole = await this.storageBroker.UpdateApprovalSettingRoleAsync(
                approvalSettingRole: auditedApprovalSettingRole,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingRoleOnRemovingApprovalSettingRoleByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalSettingRole> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedApprovalSettingRole);

            await this.eventBroker.PublishApprovalSettingRoleAsync(
                envelope: outboundEnvelope,
                operation: ApprovalSettingRoleEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingRoleOnRemovingApprovalSettingRoleByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedApprovalSettingRole;
        }

        private async ValueTask<ApprovalSettingRole> DoHardRemoveApprovalSettingRoleByIdAsync(
            Guid approvalSettingRoleId,
            EventEnvelope<ApprovalSettingRole> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnHardRemoveApprovalSettingRoleById(approvalSettingRoleId);

            ApprovalSettingRole maybeApprovalSettingRole =
                await this.storageBroker.SelectApprovalSettingRoleByIdAsync(approvalSettingRoleId, cancellationToken);

            ValidateStorageApprovalSettingRole(maybeApprovalSettingRole, approvalSettingRoleId);

            ApprovalSettingRole deletedApprovalSettingRole =
                await this.storageBroker.DeleteApprovalSettingRoleAsync(maybeApprovalSettingRole, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingRoleOnHardRemovingApprovalSettingRoleByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalSettingRole> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedApprovalSettingRole);

            await this.eventBroker.PublishApprovalSettingRoleAsync(
                envelope: outboundEnvelope,
                operation: ApprovalSettingRoleEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingRoleOnHardRemovingApprovalSettingRoleByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedApprovalSettingRole;
        }
    }
}
