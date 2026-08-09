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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviews
{
    /// <summary>
    /// Foundation service for approval reviews. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain. Per design §14.6 the foundation enforces security itself — recording
    /// a verdict requires a review role (§8.9), a review is amended or withdrawn only by its
    /// author or an Admin, hard removal is Admin-only, and reads are never public: an
    /// approval review answers not-found to everyone but its owner and the review roles —
    /// never assuming an upstream orchestration already gated the caller.
    /// </summary>
    internal partial class ApprovalReviewService : IApprovalReviewService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly IAccessBroker accessBroker;
        private readonly ILoggingBroker loggingBroker;

        public ApprovalReviewService(
            IStorageBroker storageBroker,
            IDateTimeBroker dateTimeBroker,
            IIdentifierBroker identifierBroker,
            IEventBroker eventBroker,
            IEventEnvelopeBroker eventEnvelopeBroker,
            ISecurityAuditBroker securityAuditBroker,
            IAccessBroker accessBroker,
            ILoggingBroker loggingBroker)
        {
            this.storageBroker = storageBroker;
            this.dateTimeBroker = dateTimeBroker;
            this.identifierBroker = identifierBroker;
            this.eventBroker = eventBroker;
            this.eventEnvelopeBroker = eventEnvelopeBroker;
            this.securityAuditBroker = securityAuditBroker;
            this.accessBroker = accessBroker;
            this.loggingBroker = loggingBroker;
        }

        public ValueTask<ApprovalReview> AddApprovalReviewAsync(
            ApprovalReview approvalReview,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalReviewIsNotNull(approvalReview);

                EventEnvelope<ApprovalReview> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: approvalReview);

                return await DoAddApprovalReviewAsync(
                    approvalReview: approvalReview,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<ApprovalReview>> RetrieveAllApprovalReviewsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the envelope exists to capture the ambient security context the
                // visibility filter runs against — the request payload is empty
                EventEnvelope<ApprovalReview> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new ApprovalReview());

                IQueryable<ApprovalReview> allApprovalReviews =
                    await this.storageBroker.SelectAllApprovalReviewsAsync(cancellationToken);

                return await ApplyCollectionReadVisibilityFilterAsync(
                    approvalReviews: allApprovalReviews,
                    securityContext: envelope.SecurityContext);
            });

        public ValueTask<ApprovalReview> RetrieveApprovalReviewByIdAsync(
            Guid approvalReviewId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new ApprovalReview
                {
                    Id = approvalReviewId
                };

                EventEnvelope<ApprovalReview> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveApprovalReviewByIdAsync(
                    approvalReviewId: approvalReviewId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalReview> ModifyApprovalReviewAsync(
            ApprovalReview approvalReview,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalReviewIsNotNull(approvalReview);

                EventEnvelope<ApprovalReview> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: approvalReview);

                return await DoModifyApprovalReviewAsync(
                    approvalReview: approvalReview,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalReview> RemoveApprovalReviewByIdAsync(
            Guid approvalReviewId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new ApprovalReview
                {
                    Id = approvalReviewId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<ApprovalReview> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: removeRequest);

                return await DoRemoveApprovalReviewByIdAsync(
                    approvalReviewId: approvalReviewId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalReview> HardRemoveApprovalReviewByIdAsync(
            Guid approvalReviewId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new ApprovalReview
                {
                    Id = approvalReviewId
                };

                EventEnvelope<ApprovalReview> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveApprovalReviewByIdAsync(
                    approvalReviewId: approvalReviewId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        // the shared read posture of design §14.1/§14.5/§14.6: a verdict is never public —
        // it answers not-found — never unauthorized — to everyone but the reviewer who
        // wrote it and the review roles, with the true denial reason logged server-side only
        private async ValueTask<ApprovalReview> DoRetrieveApprovalReviewByIdAsync(
            Guid approvalReviewId,
            EventEnvelope<ApprovalReview> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRetrieveApprovalReviewById(approvalReviewId);

            ApprovalReview maybeApprovalReview =
                await this.storageBroker.SelectApprovalReviewByIdAsync(approvalReviewId, cancellationToken);

            ValidateStorageApprovalReview(maybeApprovalReview, approvalReviewId);

            if (maybeApprovalReview.IsDeleted)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Approval review read denied. Approval review {approvalReviewId} is " +
                        "soft-deleted; reported to the caller as not found.");

                throw new NotFoundApprovalReviewException(
                    message: $"Approval review not found with id: {approvalReviewId}.");
            }

            SecurityContext? securityContext = inboundEnvelope.SecurityContext;

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Approval review read denied. Approval review {approvalReviewId} is not " +
                        "publicly visible and the caller is not authenticated; reported to " +
                        "the caller as not found.");

                throw new NotFoundApprovalReviewException(
                    message: $"Approval review not found with id: {approvalReviewId}.");
            }

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && maybeApprovalReview.CreatedBy == actorUserId;

            if (isOwner is false && HasReviewRole(securityContext) is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Approval review read denied. Approval review {approvalReviewId} " +
                        $"is not publicly visible and user \"{actorUserId}\" is neither the " +
                        "owner nor in a review role; reported to the caller as not found.");

                throw new NotFoundApprovalReviewException(
                    message: $"Approval review not found with id: {approvalReviewId}.");
            }

            return maybeApprovalReview;
        }

        // the collection twin of the single-row posture: a row the caller may not see
        // drops out of the set instead of erroring, so a collection read never reveals
        // how many verdicts exist — an anonymous caller sees none at all
        private async ValueTask<IQueryable<ApprovalReview>> ApplyCollectionReadVisibilityFilterAsync(
            IQueryable<ApprovalReview> approvalReviews,
            SecurityContext? securityContext)
        {
            IQueryable<ApprovalReview> visibleApprovalReviews = approvalReviews.Where(approvalReview =>
                approvalReview.IsDeleted == false);

            bool isAuthenticated =
                securityContext is not null && securityContext.IsAuthenticated;

            if (isAuthenticated is false)
            {
                return Enumerable.Empty<ApprovalReview>().AsQueryable();
            }

            if (HasReviewRole(securityContext!))
            {
                return visibleApprovalReviews;
            }

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: securityContext!);

            bool includeOwnApprovalReviews = string.IsNullOrWhiteSpace(actorUserId) is false;

            return visibleApprovalReviews.Where(approvalReview =>
                includeOwnApprovalReviews && approvalReview.CreatedBy == actorUserId);
        }

        private async ValueTask<ApprovalReview> DoAddApprovalReviewAsync(
            ApprovalReview approvalReview,
            EventEnvelope<ApprovalReview> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToReviewApprovals(inboundEnvelope.SecurityContext);

            approvalReview = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(entity: approvalReview, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddApprovalReviewAsync(
                approvalReview: approvalReview,
                securityContext: inboundEnvelope.SecurityContext);

            await ValidateUserMayRecordApprovalReviewAsync(
                approvalId: approvalReview.ApprovalId,
                isAmendingOwnReview: false,
                securityContext: inboundEnvelope.SecurityContext,
                cancellationToken: cancellationToken);

            ApprovalReview addedApprovalReview =
                await this.storageBroker.InsertApprovalReviewAsync(approvalReview, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalReviewOnAddingApprovalReviewSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalReview> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedApprovalReview);

            await this.eventBroker.PublishApprovalReviewAsync(
                envelope: outboundEnvelope,
                operation: ApprovalReviewEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalReviewOnAddingApprovalReviewSubscriptionName,
                cancellationToken: cancellationToken);

            return addedApprovalReview;
        }

        private async ValueTask<ApprovalReview> DoModifyApprovalReviewAsync(
            ApprovalReview approvalReview,
            EventEnvelope<ApprovalReview> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

            approvalReview = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: approvalReview, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyApprovalReviewAsync(
                approvalReview: approvalReview,
                securityContext: inboundEnvelope.SecurityContext);

            ApprovalReview maybeApprovalReview = await this.storageBroker.SelectApprovalReviewByIdAsync(
                approvalReviewId: approvalReview.Id,
                cancellationToken: cancellationToken);

            ValidateStorageApprovalReview(maybeApprovalReview, approvalReviewId: approvalReview.Id);

            await ValidateUserCanModifyStorageApprovalReviewAsync(
                storageApprovalReview: maybeApprovalReview,
                securityContext: inboundEnvelope.SecurityContext);

            approvalReview = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: approvalReview,
                    storageEntity: maybeApprovalReview);

            ValidateAgainstStorageApprovalReviewOnModify(
                inputApprovalReview: approvalReview,
                storageApprovalReview: maybeApprovalReview);

            // From STORAGE, and after the pin above has refused any attempt to move it. An
            // amendment that could name its own ApprovalId would let a reviewer point a review
            // at an approval whose round is still open and change a verdict on one that closed.
            await ValidateUserMayRecordApprovalReviewAsync(
                approvalId: maybeApprovalReview.ApprovalId,
                isAmendingOwnReview: true,
                securityContext: inboundEnvelope.SecurityContext,
                cancellationToken: cancellationToken);

            ApprovalReview updatedApprovalReview =
                await this.storageBroker.UpdateApprovalReviewAsync(approvalReview, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalReviewOnModifyingApprovalReviewSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalReview> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedApprovalReview);

            await this.eventBroker.PublishApprovalReviewAsync(
                envelope: outboundEnvelope,
                operation: ApprovalReviewEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalReviewOnModifyingApprovalReviewSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedApprovalReview;
        }

        private async ValueTask<ApprovalReview> DoRemoveApprovalReviewByIdAsync(
            Guid approvalReviewId,
            string? deletionReason,
            EventEnvelope<ApprovalReview> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
            ValidateOnRemoveApprovalReviewById(approvalReviewId);

            ApprovalReview maybeApprovalReview =
                await this.storageBroker.SelectApprovalReviewByIdAsync(approvalReviewId, cancellationToken);

            ValidateStorageApprovalReview(maybeApprovalReview, approvalReviewId);

            // permission comes before the idempotent short-circuit, so an unauthorized
            // caller learns nothing about the row's deletion state
            await ValidateUserCanRemoveStorageApprovalReviewAsync(
                storageApprovalReview: maybeApprovalReview,
                securityContext: inboundEnvelope.SecurityContext);

            if (maybeApprovalReview.IsDeleted)
                return maybeApprovalReview;

            if (deletionReason is not null)
                maybeApprovalReview.DeletionReason = deletionReason;

            ApprovalReview auditedApprovalReview =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeApprovalReview,
                    securityContext: inboundEnvelope.SecurityContext);

            ApprovalReview removedApprovalReview = await this.storageBroker.UpdateApprovalReviewAsync(
                approvalReview: auditedApprovalReview,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalReviewOnRemovingApprovalReviewByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalReview> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedApprovalReview);

            await this.eventBroker.PublishApprovalReviewAsync(
                envelope: outboundEnvelope,
                operation: ApprovalReviewEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalReviewOnRemovingApprovalReviewByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedApprovalReview;
        }

        private async ValueTask<ApprovalReview> DoHardRemoveApprovalReviewByIdAsync(
            Guid approvalReviewId,
            EventEnvelope<ApprovalReview> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserCanHardRemoveApprovalReview(inboundEnvelope.SecurityContext);
            ValidateOnHardRemoveApprovalReviewById(approvalReviewId);

            ApprovalReview maybeApprovalReview =
                await this.storageBroker.SelectApprovalReviewByIdAsync(approvalReviewId, cancellationToken);

            ValidateStorageApprovalReview(maybeApprovalReview, approvalReviewId);

            ApprovalReview deletedApprovalReview =
                await this.storageBroker.DeleteApprovalReviewAsync(maybeApprovalReview, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalReviewOnHardRemovingApprovalReviewByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalReview> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedApprovalReview);

            await this.eventBroker.PublishApprovalReviewAsync(
                envelope: outboundEnvelope,
                operation: ApprovalReviewEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalReviewOnHardRemovingApprovalReviewByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedApprovalReview;
        }
    }
}
