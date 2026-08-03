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
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ApprovalSettingReviewerRoles
{
    /// <summary>
    /// Foundation service for approval setting reviewer roles. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain. Per design §14.6 the foundation enforces security itself — approval
    /// setting reviewer roles are approval policy configuration, so every write (including
    /// hard removal) is Admin only, while the §14.1/§14.5 read posture answers not-found for
    /// soft-deleted rows and for callers who are not authenticated — every signed-in caller
    /// may read the policy they submit under — never assuming an upstream orchestration
    /// already gated the caller.
    /// </summary>
    internal partial class ApprovalSettingReviewerRoleService : IApprovalSettingReviewerRoleService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public ApprovalSettingReviewerRoleService(
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

        public ValueTask<ApprovalSettingReviewerRole> AddApprovalSettingReviewerRoleAsync(
            ApprovalSettingReviewerRole approvalSettingReviewerRole,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalSettingReviewerRoleIsNotNull(approvalSettingReviewerRole);

                EventEnvelope<ApprovalSettingReviewerRole> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: approvalSettingReviewerRole);

                return await DoAddApprovalSettingReviewerRoleAsync(
                    approvalSettingReviewerRole: approvalSettingReviewerRole,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<ApprovalSettingReviewerRole>> RetrieveAllApprovalSettingReviewerRolesAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the envelope exists to capture the ambient security context the
                // visibility filter runs against — the request payload is empty
                EventEnvelope<ApprovalSettingReviewerRole> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new ApprovalSettingReviewerRole());

                IQueryable<ApprovalSettingReviewerRole> allApprovalSettingReviewerRoles =
                    await this.storageBroker.SelectAllApprovalSettingReviewerRolesAsync(cancellationToken);

                return ApplyCollectionReadVisibilityFilter(
                    approvalSettingReviewerRoles: allApprovalSettingReviewerRoles,
                    securityContext: envelope.SecurityContext);
            });

        public ValueTask<ApprovalSettingReviewerRole> RetrieveApprovalSettingReviewerRoleByIdAsync(
            Guid approvalSettingReviewerRoleId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new ApprovalSettingReviewerRole
                {
                    Id = approvalSettingReviewerRoleId
                };

                EventEnvelope<ApprovalSettingReviewerRole> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveApprovalSettingReviewerRoleByIdAsync(
                    approvalSettingReviewerRoleId: approvalSettingReviewerRoleId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalSettingReviewerRole> ModifyApprovalSettingReviewerRoleAsync(
            ApprovalSettingReviewerRole approvalSettingReviewerRole,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalSettingReviewerRoleIsNotNull(approvalSettingReviewerRole);

                EventEnvelope<ApprovalSettingReviewerRole> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: approvalSettingReviewerRole);

                return await DoModifyApprovalSettingReviewerRoleAsync(
                    approvalSettingReviewerRole: approvalSettingReviewerRole,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalSettingReviewerRole> RemoveApprovalSettingReviewerRoleByIdAsync(
            Guid approvalSettingReviewerRoleId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new ApprovalSettingReviewerRole
                {
                    Id = approvalSettingReviewerRoleId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<ApprovalSettingReviewerRole> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: removeRequest);

                return await DoRemoveApprovalSettingReviewerRoleByIdAsync(
                    approvalSettingReviewerRoleId: approvalSettingReviewerRoleId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalSettingReviewerRole> HardRemoveApprovalSettingReviewerRoleByIdAsync(
            Guid approvalSettingReviewerRoleId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new ApprovalSettingReviewerRole
                {
                    Id = approvalSettingReviewerRoleId
                };

                EventEnvelope<ApprovalSettingReviewerRole> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveApprovalSettingReviewerRoleByIdAsync(
                    approvalSettingReviewerRoleId: approvalSettingReviewerRoleId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        // the shared read posture of design §14.1/§14.5/§14.6: approval policy has no
        // public face — a soft-deleted row and an unauthenticated caller both answer
        // not-found, never unauthorized, with the true denial reason logged server-side
        // only; every authenticated caller may read the rules their submissions are
        // judged by, so there is no role branch here
        private async ValueTask<ApprovalSettingReviewerRole> DoRetrieveApprovalSettingReviewerRoleByIdAsync(
            Guid approvalSettingReviewerRoleId,
            EventEnvelope<ApprovalSettingReviewerRole> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRetrieveApprovalSettingReviewerRoleById(approvalSettingReviewerRoleId);

            ApprovalSettingReviewerRole maybeApprovalSettingReviewerRole =
                await this.storageBroker.SelectApprovalSettingReviewerRoleByIdAsync(approvalSettingReviewerRoleId, cancellationToken);

            ValidateStorageApprovalSettingReviewerRole(maybeApprovalSettingReviewerRole, approvalSettingReviewerRoleId);

            if (maybeApprovalSettingReviewerRole.IsDeleted)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: "Approval setting reviewer role read denied. Approval setting reviewer role " +
                        $"{approvalSettingReviewerRoleId} is soft-deleted; reported to the caller as not found.");

                throw new NotFoundApprovalSettingReviewerRoleException(
                    message: $"Approval setting reviewer role not found with id: {approvalSettingReviewerRoleId}.");
            }

            SecurityContext? securityContext = inboundEnvelope.SecurityContext;

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: "Approval setting reviewer role read denied. Approval setting reviewer role " +
                        $"{approvalSettingReviewerRoleId} is only readable by an authenticated caller and the " +
                        "caller is not authenticated; reported to the caller as not found.");

                throw new NotFoundApprovalSettingReviewerRoleException(
                    message: $"Approval setting reviewer role not found with id: {approvalSettingReviewerRoleId}.");
            }

            return maybeApprovalSettingReviewerRole;
        }

        // the collection twin of the single-row posture: a row the caller may not see
        // drops out of the set instead of erroring, so a collection read never reveals
        // how many rows exist — an anonymous caller reads an empty set. No broker work
        // is needed to decide this, so the filter stays synchronous.
        private static IQueryable<ApprovalSettingReviewerRole> ApplyCollectionReadVisibilityFilter(
            IQueryable<ApprovalSettingReviewerRole> approvalSettingReviewerRoles,
            SecurityContext? securityContext)
        {
            bool isAuthenticated =
                securityContext is not null && securityContext.IsAuthenticated;

            if (isAuthenticated is false)
            {
                return Enumerable.Empty<ApprovalSettingReviewerRole>().AsQueryable();
            }

            return approvalSettingReviewerRoles.Where(approvalSettingReviewerRole =>
                approvalSettingReviewerRole.IsDeleted == false);
        }

        private async ValueTask<ApprovalSettingReviewerRole> DoAddApprovalSettingReviewerRoleAsync(
            ApprovalSettingReviewerRole approvalSettingReviewerRole,
            EventEnvelope<ApprovalSettingReviewerRole> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToAdministerApprovalSettingReviewerRoles(inboundEnvelope.SecurityContext);

            approvalSettingReviewerRole = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(entity: approvalSettingReviewerRole, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddApprovalSettingReviewerRoleAsync(
                approvalSettingReviewerRole: approvalSettingReviewerRole,
                securityContext: inboundEnvelope.SecurityContext);

            ApprovalSettingReviewerRole addedApprovalSettingReviewerRole =
                await this.storageBroker.InsertApprovalSettingReviewerRoleAsync(approvalSettingReviewerRole, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnAddingApprovalSettingReviewerRoleSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalSettingReviewerRole> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedApprovalSettingReviewerRole);

            await this.eventBroker.PublishApprovalSettingReviewerRoleAsync(
                envelope: outboundEnvelope,
                operation: ApprovalSettingReviewerRoleEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnAddingApprovalSettingReviewerRoleSubscriptionName,
                cancellationToken: cancellationToken);

            return addedApprovalSettingReviewerRole;
        }

        private async ValueTask<ApprovalSettingReviewerRole> DoModifyApprovalSettingReviewerRoleAsync(
            ApprovalSettingReviewerRole approvalSettingReviewerRole,
            EventEnvelope<ApprovalSettingReviewerRole> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToAdministerApprovalSettingReviewerRoles(inboundEnvelope.SecurityContext);

            approvalSettingReviewerRole = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: approvalSettingReviewerRole, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyApprovalSettingReviewerRoleAsync(
                approvalSettingReviewerRole: approvalSettingReviewerRole,
                securityContext: inboundEnvelope.SecurityContext);

            ApprovalSettingReviewerRole maybeApprovalSettingReviewerRole = await this.storageBroker.SelectApprovalSettingReviewerRoleByIdAsync(
                approvalSettingReviewerRoleId: approvalSettingReviewerRole.Id,
                cancellationToken: cancellationToken);

            ValidateStorageApprovalSettingReviewerRole(maybeApprovalSettingReviewerRole, approvalSettingReviewerRoleId: approvalSettingReviewerRole.Id);

            approvalSettingReviewerRole = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: approvalSettingReviewerRole,
                    storageEntity: maybeApprovalSettingReviewerRole);

            ValidateAgainstStorageApprovalSettingReviewerRoleOnModify(
                inputApprovalSettingReviewerRole: approvalSettingReviewerRole,
                storageApprovalSettingReviewerRole: maybeApprovalSettingReviewerRole);

            ApprovalSettingReviewerRole updatedApprovalSettingReviewerRole =
                await this.storageBroker.UpdateApprovalSettingReviewerRoleAsync(approvalSettingReviewerRole, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnModifyingApprovalSettingReviewerRoleSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalSettingReviewerRole> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedApprovalSettingReviewerRole);

            await this.eventBroker.PublishApprovalSettingReviewerRoleAsync(
                envelope: outboundEnvelope,
                operation: ApprovalSettingReviewerRoleEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnModifyingApprovalSettingReviewerRoleSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedApprovalSettingReviewerRole;
        }

        private async ValueTask<ApprovalSettingReviewerRole> DoRemoveApprovalSettingReviewerRoleByIdAsync(
            Guid approvalSettingReviewerRoleId,
            string? deletionReason,
            EventEnvelope<ApprovalSettingReviewerRole> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            // the gate comes before the idempotent short-circuit, so an unauthorized
            // caller learns nothing about the row's deletion state
            ValidateUserIsAllowedToAdministerApprovalSettingReviewerRoles(inboundEnvelope.SecurityContext);
            ValidateOnRemoveApprovalSettingReviewerRoleById(approvalSettingReviewerRoleId);

            ApprovalSettingReviewerRole maybeApprovalSettingReviewerRole =
                await this.storageBroker.SelectApprovalSettingReviewerRoleByIdAsync(approvalSettingReviewerRoleId, cancellationToken);

            ValidateStorageApprovalSettingReviewerRole(maybeApprovalSettingReviewerRole, approvalSettingReviewerRoleId);

            if (maybeApprovalSettingReviewerRole.IsDeleted)
                return maybeApprovalSettingReviewerRole;

            if (deletionReason is not null)
                maybeApprovalSettingReviewerRole.DeletionReason = deletionReason;

            ApprovalSettingReviewerRole auditedApprovalSettingReviewerRole =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeApprovalSettingReviewerRole,
                    securityContext: inboundEnvelope.SecurityContext);

            ApprovalSettingReviewerRole removedApprovalSettingReviewerRole = await this.storageBroker.UpdateApprovalSettingReviewerRoleAsync(
                approvalSettingReviewerRole: auditedApprovalSettingReviewerRole,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalSettingReviewerRole> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedApprovalSettingReviewerRole);

            await this.eventBroker.PublishApprovalSettingReviewerRoleAsync(
                envelope: outboundEnvelope,
                operation: ApprovalSettingReviewerRoleEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedApprovalSettingReviewerRole;
        }

        private async ValueTask<ApprovalSettingReviewerRole> DoHardRemoveApprovalSettingReviewerRoleByIdAsync(
            Guid approvalSettingReviewerRoleId,
            EventEnvelope<ApprovalSettingReviewerRole> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserCanHardRemoveApprovalSettingReviewerRole(inboundEnvelope.SecurityContext);
            ValidateOnHardRemoveApprovalSettingReviewerRoleById(approvalSettingReviewerRoleId);

            ApprovalSettingReviewerRole maybeApprovalSettingReviewerRole =
                await this.storageBroker.SelectApprovalSettingReviewerRoleByIdAsync(approvalSettingReviewerRoleId, cancellationToken);

            ValidateStorageApprovalSettingReviewerRole(maybeApprovalSettingReviewerRole, approvalSettingReviewerRoleId);

            ApprovalSettingReviewerRole deletedApprovalSettingReviewerRole =
                await this.storageBroker.DeleteApprovalSettingReviewerRoleAsync(maybeApprovalSettingReviewerRole, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnHardRemovingApprovalSettingReviewerRoleByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalSettingReviewerRole> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedApprovalSettingReviewerRole);

            await this.eventBroker.PublishApprovalSettingReviewerRoleAsync(
                envelope: outboundEnvelope,
                operation: ApprovalSettingReviewerRoleEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnHardRemovingApprovalSettingReviewerRoleByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedApprovalSettingReviewerRole;
        }
    }
}
