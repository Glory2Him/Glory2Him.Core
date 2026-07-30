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
using Glory2Him.Core.Models.Foundations.Approvals;

namespace Glory2Him.Core.Services.Foundations.Approvals
{
    /// <summary>
    /// Foundation service for approvals. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain.
    /// </summary>
    internal partial class ApprovalService : IApprovalService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public ApprovalService(
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

        public ValueTask<Approval> AddApprovalAsync(
            Approval approval,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalIsNotNull(approval);

                EventEnvelope<Approval> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: approval);

                return await DoAddApprovalAsync(
                    approval: approval,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<Approval>> RetrieveAllApprovalsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return await this.storageBroker.SelectAllApprovalsAsync(cancellationToken);
            });

        public ValueTask<Approval> RetrieveApprovalByIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveApprovalById(approvalId);

                Approval maybeApproval =
                    await this.storageBroker.SelectApprovalByIdAsync(approvalId, cancellationToken);

                ValidateStorageApproval(maybeApproval, approvalId);

                return maybeApproval;
            });

        public ValueTask<Approval> ModifyApprovalAsync(
            Approval approval,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalIsNotNull(approval);

                EventEnvelope<Approval> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: approval);

                return await DoModifyApprovalAsync(
                    approval: approval,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Approval> RemoveApprovalByIdAsync(
            Guid approvalId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new Approval
                {
                    Id = approvalId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<Approval> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: removeRequest);

                return await DoRemoveApprovalByIdAsync(
                    approvalId: approvalId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Approval> HardRemoveApprovalByIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new Approval
                {
                    Id = approvalId
                };

                EventEnvelope<Approval> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveApprovalByIdAsync(
                    approvalId: approvalId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<Approval> DoAddApprovalAsync(
            Approval approval,
            EventEnvelope<Approval> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            approval = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(entity: approval, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddApprovalAsync(
                approval: approval,
                securityContext: inboundEnvelope.SecurityContext);

            Approval addedApproval =
                await this.storageBroker.InsertApprovalAsync(approval, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalOnAddingApprovalSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Approval> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedApproval);

            await this.eventBroker.PublishApprovalAsync(
                envelope: outboundEnvelope,
                operation: ApprovalEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalOnAddingApprovalSubscriptionName,
                cancellationToken: cancellationToken);

            return addedApproval;
        }

        private async ValueTask<Approval> DoModifyApprovalAsync(
            Approval approval,
            EventEnvelope<Approval> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            approval = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: approval, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyApprovalAsync(
                approval: approval,
                securityContext: inboundEnvelope.SecurityContext);

            Approval maybeApproval = await this.storageBroker.SelectApprovalByIdAsync(
                approvalId: approval.Id,
                cancellationToken: cancellationToken);

            ValidateStorageApproval(maybeApproval, approvalId: approval.Id);

            approval = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: approval,
                    storageEntity: maybeApproval);

            ValidateAgainstStorageApprovalOnModify(
                inputApproval: approval,
                storageApproval: maybeApproval);

            Approval updatedApproval =
                await this.storageBroker.UpdateApprovalAsync(approval, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalOnModifyingApprovalSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Approval> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedApproval);

            await this.eventBroker.PublishApprovalAsync(
                envelope: outboundEnvelope,
                operation: ApprovalEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalOnModifyingApprovalSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedApproval;
        }

        private async ValueTask<Approval> DoRemoveApprovalByIdAsync(
            Guid approvalId,
            string? deletionReason,
            EventEnvelope<Approval> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRemoveApprovalById(approvalId);

            Approval maybeApproval =
                await this.storageBroker.SelectApprovalByIdAsync(approvalId, cancellationToken);

            ValidateStorageApproval(maybeApproval, approvalId);

            if (maybeApproval.IsDeleted)
                return maybeApproval;

            if (deletionReason is not null)
                maybeApproval.DeletionReason = deletionReason;

            Approval auditedApproval =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeApproval,
                    securityContext: inboundEnvelope.SecurityContext);

            Approval removedApproval = await this.storageBroker.UpdateApprovalAsync(
                approval: auditedApproval,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalOnRemovingApprovalByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Approval> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedApproval);

            await this.eventBroker.PublishApprovalAsync(
                envelope: outboundEnvelope,
                operation: ApprovalEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalOnRemovingApprovalByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedApproval;
        }

        private async ValueTask<Approval> DoHardRemoveApprovalByIdAsync(
            Guid approvalId,
            EventEnvelope<Approval> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnHardRemoveApprovalById(approvalId);

            Approval maybeApproval =
                await this.storageBroker.SelectApprovalByIdAsync(approvalId, cancellationToken);

            ValidateStorageApproval(maybeApproval, approvalId);

            Approval deletedApproval =
                await this.storageBroker.DeleteApprovalAsync(maybeApproval, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalOnHardRemovingApprovalByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Approval> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedApproval);

            await this.eventBroker.PublishApprovalAsync(
                envelope: outboundEnvelope,
                operation: ApprovalEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalOnHardRemovingApprovalByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedApproval;
        }
    }
}
