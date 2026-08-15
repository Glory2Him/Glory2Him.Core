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
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ApprovalComments
{
    /// <summary>
    /// Foundation service for approval comments. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain. Per design §14.6 the foundation enforces security itself — the
    /// commenting gate on writes, owner-or-review-role write permission (removal by author or
    /// Admin, hard removal by Admin only), and the §14.1/§14.5 read posture, under which a
    /// review thread is never public and answers not found to anyone but its author and the
    /// review roles — never assuming an upstream orchestration already gated the caller.
    /// <c>IsResolved</c> is writable through modify by the owner, and through the resolve
    /// transition in the <c>.Transitions</c> partial by the owner <i>or</i> an <c>Admin</c> —
    /// that widening is what the transition exists for, not exclusivity over the field.
    /// </summary>
    internal partial class ApprovalCommentService : IApprovalCommentService
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

        public ApprovalCommentService(
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

        public ValueTask<ApprovalComment> AddApprovalCommentAsync(
            ApprovalComment approvalComment,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalCommentIsNotNull(approvalComment);

                EventEnvelope<ApprovalComment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: approvalComment);

                return await DoAddApprovalCommentAsync(
                    approvalComment: approvalComment,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<ApprovalComment>> RetrieveAllApprovalCommentsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the envelope exists to capture the ambient security context the
                // visibility filter runs against — the request payload is empty
                EventEnvelope<ApprovalComment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new ApprovalComment());

                IQueryable<ApprovalComment> allApprovalComments =
                    await this.storageBroker.SelectAllApprovalCommentsAsync(cancellationToken);

                return await ApplyCollectionReadVisibilityFilterAsync(
                    approvalComments: allApprovalComments,
                    securityContext: envelope.SecurityContext);
            });

        public ValueTask<ApprovalComment> RetrieveApprovalCommentByIdAsync(
            Guid approvalCommentId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new ApprovalComment
                {
                    Id = approvalCommentId
                };

                EventEnvelope<ApprovalComment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveApprovalCommentByIdAsync(
                    approvalCommentId: approvalCommentId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalComment> ModifyApprovalCommentAsync(
            ApprovalComment approvalComment,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalCommentIsNotNull(approvalComment);

                EventEnvelope<ApprovalComment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: approvalComment);

                return await DoModifyApprovalCommentAsync(
                    approvalComment: approvalComment,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalComment> RemoveApprovalCommentByIdAsync(
            Guid approvalCommentId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new ApprovalComment
                {
                    Id = approvalCommentId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<ApprovalComment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: removeRequest);

                return await DoRemoveApprovalCommentByIdAsync(
                    approvalCommentId: approvalCommentId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalComment> HardRemoveApprovalCommentByIdAsync(
            Guid approvalCommentId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new ApprovalComment
                {
                    Id = approvalCommentId
                };

                EventEnvelope<ApprovalComment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveApprovalCommentByIdAsync(
                    approvalCommentId: approvalCommentId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        // the shared read posture of design §14.1/§14.5/§14.6: a review thread is never
        // public, so a comment answers not-found — never unauthorized — to everyone but its
        // author and the review roles, with the true denial reason logged server-side only
        private async ValueTask<ApprovalComment> DoRetrieveApprovalCommentByIdAsync(
            Guid approvalCommentId,
            EventEnvelope<ApprovalComment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRetrieveApprovalCommentById(approvalCommentId);

            ApprovalComment maybeApprovalComment =
                await this.storageBroker.SelectApprovalCommentByIdAsync(approvalCommentId, cancellationToken);

            ValidateStorageApprovalComment(maybeApprovalComment, approvalCommentId);

            if (maybeApprovalComment.IsDeleted)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Approval comment read denied. Approval comment {approvalCommentId} is " +
                        "soft-deleted; reported to the caller as not found.");

                throw new NotFoundApprovalCommentException(
                    message: $"Approval comment not found with id: {approvalCommentId}.");
            }

            SecurityContext? securityContext = inboundEnvelope.SecurityContext;

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Approval comment read denied. Approval comment {approvalCommentId} is not " +
                        "publicly readable and the caller is not authenticated; reported to " +
                        "the caller as not found.");

                throw new NotFoundApprovalCommentException(
                    message: $"Approval comment not found with id: {approvalCommentId}.");
            }

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && maybeApprovalComment.CreatedBy == actorUserId;

            if (isOwner is false && HasReviewRole(securityContext) is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Approval comment read denied. Approval comment {approvalCommentId} " +
                        $"is not publicly readable and user \"{actorUserId}\" is neither the " +
                        "author nor in a review role; reported to the caller as not found.");

                throw new NotFoundApprovalCommentException(
                    message: $"Approval comment not found with id: {approvalCommentId}.");
            }

            return maybeApprovalComment;
        }

        // the collection twin of the single-row posture: a row the caller may not see
        // drops out of the set instead of erroring, so a collection read never reveals
        // how many comments a review thread holds
        private async ValueTask<IQueryable<ApprovalComment>> ApplyCollectionReadVisibilityFilterAsync(
            IQueryable<ApprovalComment> approvalComments,
            SecurityContext? securityContext)
        {
            IQueryable<ApprovalComment> visibleApprovalComments = approvalComments.Where(approvalComment =>
                approvalComment.IsDeleted == false);

            bool isAuthenticated =
                securityContext is not null && securityContext.IsAuthenticated;

            // an anonymous caller owns nothing and reviews nothing, so the whole set is
            // filtered away rather than refused — a read still reveals no row count
            if (isAuthenticated is false)
            {
                return Enumerable.Empty<ApprovalComment>().AsQueryable();
            }

            if (HasReviewRole(securityContext!))
            {
                return visibleApprovalComments;
            }

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: securityContext!);

            bool includeOwnApprovalComments = string.IsNullOrWhiteSpace(actorUserId) is false;

            return visibleApprovalComments.Where(approvalComment =>
                includeOwnApprovalComments && approvalComment.CreatedBy == actorUserId);
        }

        private async ValueTask<ApprovalComment> DoAddApprovalCommentAsync(
            ApprovalComment approvalComment,
            EventEnvelope<ApprovalComment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToComment(inboundEnvelope.SecurityContext);

            approvalComment = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(entity: approvalComment, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddApprovalCommentAsync(
                approvalComment: approvalComment,
                securityContext: inboundEnvelope.SecurityContext);

            await ValidateUserMayRecordApprovalCommentAsync(
                approvalId: approvalComment.ApprovalId,
                securityContext: inboundEnvelope.SecurityContext,
                cancellationToken: cancellationToken);

            ApprovalComment addedApprovalComment =
                await this.storageBroker.InsertApprovalCommentAsync(approvalComment, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalCommentOnAddingApprovalCommentSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalComment> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedApprovalComment);

            await this.eventBroker.PublishApprovalCommentAsync(
                envelope: outboundEnvelope,
                operation: ApprovalCommentEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalCommentOnAddingApprovalCommentSubscriptionName,
                cancellationToken: cancellationToken);

            return addedApprovalComment;
        }

        private async ValueTask<ApprovalComment> DoModifyApprovalCommentAsync(
            ApprovalComment approvalComment,
            EventEnvelope<ApprovalComment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToComment(inboundEnvelope.SecurityContext);

            approvalComment = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: approvalComment, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyApprovalCommentAsync(
                approvalComment: approvalComment,
                securityContext: inboundEnvelope.SecurityContext);

            ApprovalComment maybeApprovalComment = await this.storageBroker.SelectApprovalCommentByIdAsync(
                approvalCommentId: approvalComment.Id,
                cancellationToken: cancellationToken);

            ValidateStorageApprovalComment(maybeApprovalComment, approvalCommentId: approvalComment.Id);

            await ValidateUserCanModifyStorageApprovalCommentAsync(
                storageApprovalComment: maybeApprovalComment,
                securityContext: inboundEnvelope.SecurityContext);

            await ValidateUserMayAmendApprovalCommentAsync(
                approvalId: maybeApprovalComment.ApprovalId,
                commentCreatedBy: maybeApprovalComment.CreatedBy,
                securityContext: inboundEnvelope.SecurityContext,
                cancellationToken: cancellationToken);

            approvalComment = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: approvalComment,
                    storageEntity: maybeApprovalComment);

            ValidateAgainstStorageApprovalCommentOnModify(
                inputApprovalComment: approvalComment,
                storageApprovalComment: maybeApprovalComment);

            ApprovalComment updatedApprovalComment =
                await this.storageBroker.UpdateApprovalCommentAsync(approvalComment, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalCommentOnModifyingApprovalCommentSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalComment> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedApprovalComment);

            await this.eventBroker.PublishApprovalCommentAsync(
                envelope: outboundEnvelope,
                operation: ApprovalCommentEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalCommentOnModifyingApprovalCommentSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedApprovalComment;
        }

        private async ValueTask<ApprovalComment> DoRemoveApprovalCommentByIdAsync(
            Guid approvalCommentId,
            string? deletionReason,
            EventEnvelope<ApprovalComment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToComment(inboundEnvelope.SecurityContext);
            ValidateOnRemoveApprovalCommentById(approvalCommentId, deletionReason);

            ApprovalComment maybeApprovalComment =
                await this.storageBroker.SelectApprovalCommentByIdAsync(approvalCommentId, cancellationToken);

            ValidateStorageApprovalComment(maybeApprovalComment, approvalCommentId);

            // permission comes before the idempotent short-circuit, so an unauthorized
            // caller learns nothing about the row's deletion state
            await ValidateUserCanRemoveStorageApprovalCommentAsync(
                storageApprovalComment: maybeApprovalComment,
                securityContext: inboundEnvelope.SecurityContext);

            await ValidateUserMayAmendApprovalCommentAsync(
                approvalId: maybeApprovalComment.ApprovalId,
                commentCreatedBy: maybeApprovalComment.CreatedBy,
                securityContext: inboundEnvelope.SecurityContext,
                cancellationToken: cancellationToken);

            if (maybeApprovalComment.IsDeleted)
                return maybeApprovalComment;

            ApprovalComment auditedApprovalComment =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeApprovalComment,
                    securityContext: inboundEnvelope.SecurityContext,
                    deletionReason: deletionReason);

            ApprovalComment removedApprovalComment = await this.storageBroker.UpdateApprovalCommentAsync(
                approvalComment: auditedApprovalComment,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalCommentOnRemovingApprovalCommentByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalComment> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedApprovalComment);

            await this.eventBroker.PublishApprovalCommentAsync(
                envelope: outboundEnvelope,
                operation: ApprovalCommentEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalCommentOnRemovingApprovalCommentByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedApprovalComment;
        }

        private async ValueTask<ApprovalComment> DoHardRemoveApprovalCommentByIdAsync(
            Guid approvalCommentId,
            EventEnvelope<ApprovalComment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserCanHardRemoveApprovalComment(inboundEnvelope.SecurityContext);
            ValidateOnHardRemoveApprovalCommentById(approvalCommentId);

            ApprovalComment maybeApprovalComment =
                await this.storageBroker.SelectApprovalCommentByIdAsync(approvalCommentId, cancellationToken);

            ValidateStorageApprovalComment(maybeApprovalComment, approvalCommentId);

            ApprovalComment deletedApprovalComment =
                await this.storageBroker.DeleteApprovalCommentAsync(maybeApprovalComment, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalCommentOnHardRemovingApprovalCommentByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalComment> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedApprovalComment);

            await this.eventBroker.PublishApprovalCommentAsync(
                envelope: outboundEnvelope,
                operation: ApprovalCommentEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalCommentOnHardRemovingApprovalCommentByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedApprovalComment;
        }
    }
}
