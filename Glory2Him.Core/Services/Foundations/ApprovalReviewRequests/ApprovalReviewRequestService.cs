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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviewRequests
{
    /// <summary>
    /// Foundation service for approval review requests — the invitations of design §7.9. Every
    /// operation is both callable directly (the non-event path: object in → request envelope →
    /// shared do-work) and reachable through the event substrate (the event path in the
    /// <c>.Substrate</c> partial: request envelope in → shared do-work). The private
    /// <c>DoXAsync</c> methods own auditing, validation, storage, and publishing the past-tense
    /// fact, so the two paths cannot diverge; the inbound envelope carries the original caller's
    /// <c>SecurityContext</c> and anchors the causation chain.
    ///
    /// <para><b>An invitation grants nothing.</b> A request row confers no eligibility — that
    /// stays composed from roles (§8.3, §18.6) — and appears in no §8.5 condition, so nothing
    /// here can move an approval, change a count or block a round. That is why this service takes
    /// no <c>IAccessBroker</c>: it has no cross-entity invariant to defend.</para>
    ///
    /// <para><b>Which half of the rules lives here.</b> Per §14.6 the foundation enforces
    /// security itself and never assumes an upstream layer gated the caller, so the ROW-LOCAL
    /// half is enforced below: a requester must be authenticated, unblocked and hold a
    /// review-tier role (§7.9 rule 2); withdrawal is open to that same tier rather than to the
    /// requester alone (rule 5); hard removal is <c>Administrators</c>-only; and reads are never public.
    /// The CROSS-ENTITY half — that the invited person satisfies the review tier for the entity
    /// and does not own it (rule 3), that the round is still <c>Submitted</c> (rule 7), and the
    /// idempotent dismiss of a duplicate (rule 4) — needs the parent <c>Approval</c> and the
    /// entity behind it, neither of which a single-entity service may read. Those belong to
    /// <c>ApprovalOrchestrationService.RequestApprovalReviewAsync</c> (§16.7.4), which is also
    /// what the exposers bind to (§10.17 rule 3).</para>
    ///
    /// <para><b>Withdrawal is deliberately NOT owner-only</b>, which is the one place this
    /// service diverges from its <c>ApprovalReview</c> sibling. A review carries a verdict, so
    /// only its author may touch it; a request carries no judgement at all, and the case rule 5
    /// exists to serve — undoing an invitation sent to the wrong person — is one the requester
    /// may not be around to fix. <c>DeletedBy</c> records who withdrew it.</para>
    /// </summary>
    internal partial class ApprovalReviewRequestService : IApprovalReviewRequestService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly IEnvelopeIntegrityBroker envelopeIntegrityBroker;
        private readonly ILoggingBroker loggingBroker;

        public ApprovalReviewRequestService(
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

        public ValueTask<ApprovalReviewRequest> AddApprovalReviewRequestAsync(
            ApprovalReviewRequest approvalReviewRequest,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalReviewRequestIsNotNull(approvalReviewRequest);

                EventEnvelope<ApprovalReviewRequest> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: approvalReviewRequest);

                return await DoAddApprovalReviewRequestAsync(
                    approvalReviewRequest: approvalReviewRequest,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<ApprovalReviewRequest>> RetrieveAllApprovalReviewRequestsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the envelope exists to capture the ambient security context the
                // visibility filter runs against — the request payload is empty
                EventEnvelope<ApprovalReviewRequest> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new ApprovalReviewRequest());

                IQueryable<ApprovalReviewRequest> allApprovalReviewRequests =
                    await this.storageBroker.SelectAllApprovalReviewRequestsAsync(cancellationToken);

                return await ApplyCollectionReadVisibilityFilterAsync(
                    approvalReviewRequests: allApprovalReviewRequests,
                    securityContext: envelope.SecurityContext);
            });

        public ValueTask<ApprovalReviewRequest> RetrieveApprovalReviewRequestByIdAsync(
            Guid approvalReviewRequestId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new ApprovalReviewRequest
                {
                    Id = approvalReviewRequestId
                };

                EventEnvelope<ApprovalReviewRequest> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveApprovalReviewRequestByIdAsync(
                    approvalReviewRequestId: approvalReviewRequestId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalReviewRequest> RemoveApprovalReviewRequestByIdAsync(
            Guid approvalReviewRequestId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new ApprovalReviewRequest
                {
                    Id = approvalReviewRequestId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<ApprovalReviewRequest> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: removeRequest);

                return await DoRemoveApprovalReviewRequestByIdAsync(
                    approvalReviewRequestId: approvalReviewRequestId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalReviewRequest> HardRemoveApprovalReviewRequestByIdAsync(
            Guid approvalReviewRequestId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new ApprovalReviewRequest
                {
                    Id = approvalReviewRequestId
                };

                EventEnvelope<ApprovalReviewRequest> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveApprovalReviewRequestByIdAsync(
                    approvalReviewRequestId: approvalReviewRequestId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        // The read posture of §14.1/§14.5/§14.6, and it is not public: who has been asked to
        // review is moderation coordination (§16.7.4), so it answers not-found — never
        // unauthorized — to everyone outside the round, with the true denial reason logged
        // server-side only.
        //
        // Three parties are inside it: the review roles, the requester, and the invited person
        // themselves. The last two are named explicitly rather than left to the tier, because a
        // party to an invitation should still be able to read the row that names them if their
        // role membership shifts underneath it.
        private async ValueTask<ApprovalReviewRequest> DoRetrieveApprovalReviewRequestByIdAsync(
            Guid approvalReviewRequestId,
            EventEnvelope<ApprovalReviewRequest> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRetrieveApprovalReviewRequestById(approvalReviewRequestId);

            ApprovalReviewRequest maybeApprovalReviewRequest =
                await this.storageBroker.SelectApprovalReviewRequestByIdAsync(
                    approvalReviewRequestId, cancellationToken);

            ValidateStorageApprovalReviewRequest(maybeApprovalReviewRequest, approvalReviewRequestId);

            if (maybeApprovalReviewRequest.IsDeleted)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Approval review request read denied. Approval review request " +
                        $"{approvalReviewRequestId} is withdrawn; reported to the caller as not found.");

                throw new NotFoundApprovalReviewRequestException(
                    message: $"Approval review request not found with id: {approvalReviewRequestId}.");
            }

            SecurityContext? securityContext = inboundEnvelope.SecurityContext;

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Approval review request read denied. Approval review request " +
                        $"{approvalReviewRequestId} is not publicly visible and the caller is not " +
                        "authenticated; reported to the caller as not found.");

                throw new NotFoundApprovalReviewRequestException(
                    message: $"Approval review request not found with id: {approvalReviewRequestId}.");
            }

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: securityContext);

            bool isParty = IsPartyToRequest(
                approvalReviewRequest: maybeApprovalReviewRequest,
                actorUserId: actorUserId);

            if (isParty is false && HasReviewRole(securityContext) is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Approval review request read denied. Approval review request " +
                        $"{approvalReviewRequestId} is not publicly visible and user " +
                        $"\"{actorUserId}\" is neither a party to it nor in a review role; " +
                        "reported to the caller as not found.");

                throw new NotFoundApprovalReviewRequestException(
                    message: $"Approval review request not found with id: {approvalReviewRequestId}.");
            }

            return maybeApprovalReviewRequest;
        }

        // The collection twin of the single-row posture: a row the caller may not see drops out
        // of the set instead of erroring, so a collection read never reveals how many
        // invitations exist — an anonymous caller sees none at all.
        private async ValueTask<IQueryable<ApprovalReviewRequest>> ApplyCollectionReadVisibilityFilterAsync(
            IQueryable<ApprovalReviewRequest> approvalReviewRequests,
            SecurityContext? securityContext)
        {
            IQueryable<ApprovalReviewRequest> visibleApprovalReviewRequests =
                approvalReviewRequests.Where(approvalReviewRequest =>
                    approvalReviewRequest.IsDeleted == false);

            bool isAuthenticated =
                securityContext is not null && securityContext.IsAuthenticated;

            if (isAuthenticated is false)
            {
                return Enumerable.Empty<ApprovalReviewRequest>().AsQueryable();
            }

            if (HasReviewRole(securityContext!))
            {
                return visibleApprovalReviewRequests;
            }

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: securityContext!);

            bool includeOwnApprovalReviewRequests = string.IsNullOrWhiteSpace(actorUserId) is false;

            return visibleApprovalReviewRequests.Where(approvalReviewRequest =>
                includeOwnApprovalReviewRequests
                    && (approvalReviewRequest.CreatedBy == actorUserId
                        || approvalReviewRequest.RequestedUserId == actorUserId));
        }

        private async ValueTask<ApprovalReviewRequest> DoAddApprovalReviewRequestAsync(
            ApprovalReviewRequest approvalReviewRequest,
            EventEnvelope<ApprovalReviewRequest> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToRequestApprovalReviews(inboundEnvelope.SecurityContext);

            approvalReviewRequest = await this.securityAuditBroker.ApplyAddAuditValuesAsync(
                entity: approvalReviewRequest,
                securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddApprovalReviewRequestAsync(
                approvalReviewRequest: approvalReviewRequest,
                securityContext: inboundEnvelope.SecurityContext);

            ApprovalReviewRequest addedApprovalReviewRequest =
                await this.storageBroker.InsertApprovalReviewRequestAsync(
                    approvalReviewRequest, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ApprovalReviewRequestOnAddingApprovalReviewRequestSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalReviewRequest> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedApprovalReviewRequest);

            await this.eventBroker.PublishApprovalReviewRequestAsync(
                envelope: outboundEnvelope,
                operation: ApprovalReviewRequestEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ApprovalReviewRequestOnAddingApprovalReviewRequestSubscriptionName,
                cancellationToken: cancellationToken);

            return addedApprovalReviewRequest;
        }

        private async ValueTask<ApprovalReviewRequest> DoRemoveApprovalReviewRequestByIdAsync(
            Guid approvalReviewRequestId,
            string? deletionReason,
            EventEnvelope<ApprovalReviewRequest> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            // The requesting tier, not the requester (§7.9 rule 5). Withdrawal destroys no
            // judgement — there is none on the row — so the gate that protects a review's author
            // has nothing to protect here, and insisting on it would strand an invitation sent in
            // error whenever the person who sent it is unavailable.
            ValidateUserIsAllowedToRequestApprovalReviews(inboundEnvelope.SecurityContext);
            ValidateOnRemoveApprovalReviewRequestById(approvalReviewRequestId, deletionReason);

            ApprovalReviewRequest maybeApprovalReviewRequest =
                await this.storageBroker.SelectApprovalReviewRequestByIdAsync(
                    approvalReviewRequestId, cancellationToken);

            ValidateStorageApprovalReviewRequest(maybeApprovalReviewRequest, approvalReviewRequestId);

            if (maybeApprovalReviewRequest.IsDeleted)
                return maybeApprovalReviewRequest;

            ApprovalReviewRequest auditedApprovalReviewRequest =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeApprovalReviewRequest,
                    securityContext: inboundEnvelope.SecurityContext,
                    deletionReason: deletionReason);

            ApprovalReviewRequest removedApprovalReviewRequest =
                await this.storageBroker.UpdateApprovalReviewRequestAsync(
                    approvalReviewRequest: auditedApprovalReviewRequest,
                    cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ApprovalReviewRequestOnRemovingApprovalReviewRequestByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalReviewRequest> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedApprovalReviewRequest);

            await this.eventBroker.PublishApprovalReviewRequestAsync(
                envelope: outboundEnvelope,
                operation: ApprovalReviewRequestEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ApprovalReviewRequestOnRemovingApprovalReviewRequestByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedApprovalReviewRequest;
        }

        private async ValueTask<ApprovalReviewRequest> DoHardRemoveApprovalReviewRequestByIdAsync(
            Guid approvalReviewRequestId,
            EventEnvelope<ApprovalReviewRequest> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserCanHardRemoveApprovalReviewRequest(inboundEnvelope.SecurityContext);
            ValidateOnHardRemoveApprovalReviewRequestById(approvalReviewRequestId);

            ApprovalReviewRequest maybeApprovalReviewRequest =
                await this.storageBroker.SelectApprovalReviewRequestByIdAsync(
                    approvalReviewRequestId, cancellationToken);

            ValidateStorageApprovalReviewRequest(maybeApprovalReviewRequest, approvalReviewRequestId);

            ApprovalReviewRequest deletedApprovalReviewRequest =
                await this.storageBroker.DeleteApprovalReviewRequestAsync(
                    maybeApprovalReviewRequest, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ApprovalReviewRequestOnHardRemovingApprovalReviewRequestByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalReviewRequest> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedApprovalReviewRequest);

            await this.eventBroker.PublishApprovalReviewRequestAsync(
                envelope: outboundEnvelope,
                operation: ApprovalReviewRequestEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ApprovalReviewRequestOnHardRemovingApprovalReviewRequestByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedApprovalReviewRequest;
        }
    }
}
