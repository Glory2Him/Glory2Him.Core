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
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;

namespace Glory2Him.Core.Services.Foundations.Approvals
{
    /// <summary>
    /// Foundation service for approvals. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain. Per design §14.6 the foundation enforces security itself — the
    /// contribution gate on writes, owner-or-review-role write permission (removal by owner
    /// or Admin, hard removal by Admin only), and the §14.1/§14.5 read visibility posture
    /// (an approval is a workflow record, never public: only its owner and the review roles
    /// may read it) — never assuming an upstream orchestration already gated the caller.
    /// </summary>
    internal partial class ApprovalService : IApprovalService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly IAccessBroker accessBroker;
        private readonly IEnvelopeIntegrityBroker envelopeIntegrityBroker;
        private readonly ILoggingBroker loggingBroker;

        public ApprovalService(
            IStorageBroker storageBroker,
            IDateTimeBroker dateTimeBroker,
            IIdentifierBroker identifierBroker,
            IEventBroker eventBroker,
            IEventEnvelopeBroker eventEnvelopeBroker,
            ISecurityAuditBroker securityAuditBroker,
            IAccessBroker accessBroker,
            IEnvelopeIntegrityBroker envelopeIntegrityBroker,
            ILoggingBroker loggingBroker)
        {
            this.storageBroker = storageBroker;
            this.dateTimeBroker = dateTimeBroker;
            this.identifierBroker = identifierBroker;
            this.eventBroker = eventBroker;
            this.eventEnvelopeBroker = eventEnvelopeBroker;
            this.securityAuditBroker = securityAuditBroker;
            this.accessBroker = accessBroker;
            this.envelopeIntegrityBroker = envelopeIntegrityBroker;
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

                // the envelope exists to capture the ambient security context the
                // visibility filter runs against — the request payload is empty
                EventEnvelope<Approval> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new Approval());

                IQueryable<Approval> allApprovals =
                    await this.storageBroker.SelectAllApprovalsAsync(cancellationToken);

                return await ApplyCollectionReadVisibilityFilterAsync(
                    approvals: allApprovals,
                    securityContext: envelope.SecurityContext);
            });

        public ValueTask<Approval> RetrieveApprovalByIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new Approval
                {
                    Id = approvalId
                };

                EventEnvelope<Approval> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveApprovalByIdAsync(
                    approvalId: approvalId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
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

        // the shared read posture of design §14.1/§14.5/§14.6: an approval is a workflow
        // record and is never publicly visible — only the submitter who owns it and the
        // review roles may read it; everyone else answers not-found — never unauthorized —
        // with the true denial reason logged server-side only
        private async ValueTask<Approval> DoRetrieveApprovalByIdAsync(
            Guid approvalId,
            EventEnvelope<Approval> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRetrieveApprovalById(approvalId);

            Approval maybeApproval = await this.storageBroker.SelectApprovalByIdAsync(
                approvalId: approvalId,
                cancellationToken: cancellationToken);

            ValidateStorageApproval(maybeApproval, approvalId);

            if (maybeApproval.IsDeleted)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Approval read denied. Approval {approvalId} is " +
                        "soft-deleted; reported to the caller as not found.");

                throw new NotFoundApprovalException(
                    message: $"Approval not found with id: {approvalId}.");
            }

            SecurityContext? securityContext = inboundEnvelope.SecurityContext;

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Approval read denied. Approval {approvalId} is not " +
                        "publicly visible and the caller is not authenticated; reported to " +
                        "the caller as not found.");

                throw new NotFoundApprovalException(
                    message: $"Approval not found with id: {approvalId}.");
            }

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && maybeApproval.CreatedBy == actorUserId;

            if (isOwner is false && HasReviewRole(securityContext) is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Approval read denied. Approval {approvalId} " +
                        $"is not publicly visible and user \"{actorUserId}\" is neither the " +
                        "owner nor in a review role; reported to the caller as not found.");

                throw new NotFoundApprovalException(
                    message: $"Approval not found with id: {approvalId}.");
            }

            return maybeApproval;
        }

        // the collection twin of the single-row posture: a row the caller may not see
        // drops out of the set instead of erroring, so a collection read never reveals
        // how many approvals exist
        private async ValueTask<IQueryable<Approval>> ApplyCollectionReadVisibilityFilterAsync(
            IQueryable<Approval> approvals,
            SecurityContext? securityContext)
        {
            bool isAuthenticated =
                securityContext is not null && securityContext.IsAuthenticated;

            // nothing here is public, so an anonymous caller sees nothing at all
            if (isAuthenticated is false)
            {
                return Enumerable.Empty<Approval>().AsQueryable();
            }

            IQueryable<Approval> visibleApprovals = approvals.Where(approval =>
                approval.IsDeleted == false);

            if (HasReviewRole(securityContext!))
            {
                return visibleApprovals;
            }

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: securityContext!);

            bool includeOwnApprovals = string.IsNullOrWhiteSpace(actorUserId) is false;

            return visibleApprovals.Where(approval =>
                includeOwnApprovals && approval.CreatedBy == actorUserId);
        }

        private async ValueTask<Approval> DoAddApprovalAsync(
            Approval approval,
            EventEnvelope<Approval> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

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
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

            approval = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: approval, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyApprovalAsync(
                approval: approval,
                securityContext: inboundEnvelope.SecurityContext);

            Approval maybeApproval = await this.storageBroker.SelectApprovalByIdAsync(
                approvalId: approval.Id,
                cancellationToken: cancellationToken);

            ValidateStorageApproval(maybeApproval, approvalId: approval.Id);

            await ValidateUserCanModifyStorageApprovalAsync(
                storageApproval: maybeApproval,
                securityContext: inboundEnvelope.SecurityContext);

            // and that tier narrowed to the entity actually under approval, which the row-local
            // check above cannot see — a Tag-Reviewer clears it for any approval at all. Asked
            // about the STORED row, so a payload naming a different entity cannot move the
            // question onto something the caller does hold a role for.
            await ValidateUserMayAmendStorageApprovalAsync(
                storageApproval: maybeApproval,
                securityContext: inboundEnvelope.SecurityContext,
                cancellationToken: cancellationToken);

            // Null unless the payload moves the status into Approved or Rejected; the §8.6.1
            // verdict otherwise, which the derivation below records.
            AccessVerdict outcomeVerdict = await ValidateUserMayDecideStorageApprovalAsync(
                inputApproval: approval,
                storageApproval: maybeApproval,
                securityContext: inboundEnvelope.SecurityContext,
                cancellationToken: cancellationToken);

            approval = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: approval,
                    storageEntity: maybeApproval);

            ValidateAgainstStorageApprovalOnModify(
                inputApproval: approval,
                storageApproval: maybeApproval);

            ValidateBypassPairAgainstStorageOnModify(
                inputApproval: approval,
                storageApproval: maybeApproval);

            if (outcomeVerdict is not null)
            {
                // DERIVED from the verdict, never copied from the payload. The verdict can come
                // back IsBypassUsed = false even when a bypass was requested — the conditions
                // happened to be met, so nothing was waived — and recording the request instead
                // of the outcome would manufacture a waiver that never happened (§9.7.5).
                //
                // On BOTH outcomes, exactly as the entity transitions do. A rejection waives
                // nothing, so its verdict is always a plain permit and this clears the pair:
                // deriving only on approval would leave a row that was bypass-approved, reopened
                // and then rejected asserting a waiver no verb could ever clear.
                approval.IsApprovedByBypass = outcomeVerdict.IsBypassUsed;

                approval.ApprovedByBypassReason = outcomeVerdict.IsBypassUsed
                    ? approval.ApprovedByBypassReason
                    : null;
            }

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
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
            ValidateOnRemoveApprovalById(approvalId, deletionReason);

            Approval maybeApproval =
                await this.storageBroker.SelectApprovalByIdAsync(approvalId, cancellationToken);

            ValidateStorageApproval(maybeApproval, approvalId);

            // permission comes before the idempotent short-circuit, so an unauthorized
            // caller learns nothing about the row's deletion state
            await ValidateUserCanRemoveStorageApprovalAsync(
                storageApproval: maybeApproval,
                securityContext: inboundEnvelope.SecurityContext);

            if (maybeApproval.IsDeleted)
                return maybeApproval;

            Approval auditedApproval =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeApproval,
                    securityContext: inboundEnvelope.SecurityContext,
                    deletionReason: deletionReason);

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
            ValidateUserCanHardRemoveApproval(inboundEnvelope.SecurityContext);
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
