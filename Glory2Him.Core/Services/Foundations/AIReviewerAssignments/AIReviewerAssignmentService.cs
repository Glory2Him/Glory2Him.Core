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
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;

namespace Glory2Him.Core.Services.Foundations.AIReviewerAssignments
{
    /// <summary>
    /// Foundation service for AI reviewer assignments — Berean's assignment record (design
    /// §8.6.2, GitHub issue #354). Every operation is both callable directly (the non-event
    /// path: object in → request envelope → shared do-work) and reachable through the event
    /// substrate (the event path in the <c>.Substrate</c> partial: request envelope in → shared
    /// do-work). The private <c>DoXAsync</c> methods own auditing, validation, storage, and
    /// publishing the past-tense fact, so the two paths cannot diverge; the inbound envelope
    /// carries the original caller's <c>SecurityContext</c> and anchors the causation chain.
    ///
    /// <para><b>Simpler than its <c>ApprovalReviewRequest</c> sibling, and deliberately so.</b>
    /// Berean is not a role-bearing human identity, so there is no per-person dimension: at most
    /// one LIVE row exists per approval, enforced by <c>UX_AIReviewerAssignments_ApprovalId</c>
    /// alone rather than a composite index. There is also no "party to it" read nuance — nobody
    /// is invited by name — so the read posture below asks one question, the review tier,
    /// instead of two.</para>
    ///
    /// <para><b>Security posture (design §14.6): the foundation enforces its own rules and never
    /// assumes an upstream layer gated the caller.</b> Assigning, amending or removing Berean's
    /// assignment is open to the same review tier that may request a human reviewer — the whole
    /// <c>Reviewers</c>/<c>Publishers</c>/<c>Administrators</c> tier plus the entity-scoped
    /// variants (§16.6) — because deciding to bring Berean onto a round is the same kind of
    /// round-coordination act as inviting a person to it. Reads are never public: who Berean has
    /// been assigned to is moderation coordination, so a denied read answers not-found, never
    /// unauthorized, with the true denial reason logged server-side only.</para>
    /// </summary>
    internal partial class AIReviewerAssignmentService : IAIReviewerAssignmentService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly IEnvelopeIntegrityBroker envelopeIntegrityBroker;
        private readonly ILoggingBroker loggingBroker;

        public AIReviewerAssignmentService(
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

        public ValueTask<AIReviewerAssignment> AddAIReviewerAssignmentAsync(
            AIReviewerAssignment aiReviewerAssignment,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateAIReviewerAssignmentIsNotNull(aiReviewerAssignment);

                EventEnvelope<AIReviewerAssignment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: aiReviewerAssignment);

                return await DoAddAIReviewerAssignmentAsync(
                    aiReviewerAssignment: aiReviewerAssignment,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<AIReviewerAssignment> RetrieveAIReviewerAssignmentByIdAsync(
            Guid aiReviewerAssignmentId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new AIReviewerAssignment
                {
                    Id = aiReviewerAssignmentId
                };

                EventEnvelope<AIReviewerAssignment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveAIReviewerAssignmentByIdAsync(
                    aiReviewerAssignmentId: aiReviewerAssignmentId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<AIReviewerAssignment> ModifyAIReviewerAssignmentAsync(
            AIReviewerAssignment aiReviewerAssignment,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateAIReviewerAssignmentIsNotNull(aiReviewerAssignment);

                EventEnvelope<AIReviewerAssignment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: aiReviewerAssignment);

                return await DoModifyAIReviewerAssignmentAsync(
                    aiReviewerAssignment: aiReviewerAssignment,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<AIReviewerAssignment> RemoveAIReviewerAssignmentByIdAsync(
            Guid aiReviewerAssignmentId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new AIReviewerAssignment
                {
                    Id = aiReviewerAssignmentId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<AIReviewerAssignment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: removeRequest);

                return await DoRemoveAIReviewerAssignmentByIdAsync(
                    aiReviewerAssignmentId: aiReviewerAssignmentId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        // The read posture of design §14.1/§14.5/§14.6, and it is not public: which round Berean
        // has been assigned to is moderation coordination, so it answers not-found — never
        // unauthorized — to everyone outside the review tier, with the true denial reason logged
        // server-side only. Unlike ApprovalReviewRequest's read gate, there is no "party to it"
        // exception: nobody is invited by name here, so the tier is the whole question.
        private async ValueTask<AIReviewerAssignment> DoRetrieveAIReviewerAssignmentByIdAsync(
            Guid aiReviewerAssignmentId,
            EventEnvelope<AIReviewerAssignment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRetrieveAIReviewerAssignmentById(aiReviewerAssignmentId);

            AIReviewerAssignment maybeAIReviewerAssignment =
                await this.storageBroker.SelectAIReviewerAssignmentByIdAsync(
                    aiReviewerAssignmentId, cancellationToken);

            ValidateStorageAIReviewerAssignment(maybeAIReviewerAssignment, aiReviewerAssignmentId);

            if (maybeAIReviewerAssignment.IsDeleted)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"AI reviewer assignment read denied. AI reviewer assignment " +
                        $"{aiReviewerAssignmentId} is removed; reported to the caller as not found.");

                throw new NotFoundAIReviewerAssignmentException(
                    message: $"AI reviewer assignment not found with id: {aiReviewerAssignmentId}.");
            }

            SecurityContext? securityContext = inboundEnvelope.SecurityContext;

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"AI reviewer assignment read denied. AI reviewer assignment " +
                        $"{aiReviewerAssignmentId} is not publicly visible and the caller is not " +
                        "authenticated; reported to the caller as not found.");

                throw new NotFoundAIReviewerAssignmentException(
                    message: $"AI reviewer assignment not found with id: {aiReviewerAssignmentId}.");
            }

            if (HasReviewRole(securityContext) is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"AI reviewer assignment read denied. AI reviewer assignment " +
                        $"{aiReviewerAssignmentId} is not publicly visible and the caller is not " +
                        "in a review role; reported to the caller as not found.");

                throw new NotFoundAIReviewerAssignmentException(
                    message: $"AI reviewer assignment not found with id: {aiReviewerAssignmentId}.");
            }

            return maybeAIReviewerAssignment;
        }

        private async ValueTask<AIReviewerAssignment> DoAddAIReviewerAssignmentAsync(
            AIReviewerAssignment aiReviewerAssignment,
            EventEnvelope<AIReviewerAssignment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToManageAIReviewerAssignments(inboundEnvelope.SecurityContext);

            aiReviewerAssignment = await this.securityAuditBroker.ApplyAddAuditValuesAsync(
                entity: aiReviewerAssignment,
                securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddAIReviewerAssignmentAsync(
                aiReviewerAssignment: aiReviewerAssignment,
                securityContext: inboundEnvelope.SecurityContext);

            AIReviewerAssignment addedAIReviewerAssignment =
                await this.storageBroker.InsertAIReviewerAssignmentAsync(
                    aiReviewerAssignment, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .AIReviewerAssignmentOnAddingAIReviewerAssignmentSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<AIReviewerAssignment> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedAIReviewerAssignment);

            await this.eventBroker.PublishAIReviewerAssignmentAsync(
                envelope: outboundEnvelope,
                operation: AIReviewerAssignmentEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .AIReviewerAssignmentOnAddingAIReviewerAssignmentSubscriptionName,
                cancellationToken: cancellationToken);

            return addedAIReviewerAssignment;
        }

        private async ValueTask<AIReviewerAssignment> DoModifyAIReviewerAssignmentAsync(
            AIReviewerAssignment aiReviewerAssignment,
            EventEnvelope<AIReviewerAssignment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToManageAIReviewerAssignments(inboundEnvelope.SecurityContext);

            aiReviewerAssignment = await this.securityAuditBroker.ApplyModifyAuditValuesAsync(
                entity: aiReviewerAssignment,
                securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyAIReviewerAssignmentAsync(
                aiReviewerAssignment: aiReviewerAssignment,
                securityContext: inboundEnvelope.SecurityContext);

            AIReviewerAssignment maybeAIReviewerAssignment =
                await this.storageBroker.SelectAIReviewerAssignmentByIdAsync(
                    aiReviewerAssignment.Id, cancellationToken);

            ValidateStorageAIReviewerAssignment(maybeAIReviewerAssignment, aiReviewerAssignment.Id);

            // A WITHDRAWN ASSIGNMENT IS CLOSED TO WRITES, and this is the only path that can still
            // reach one. The orchestration's upsert reads by approval id, which returns live rows
            // only — but the future review process of §8.6.2 retrieves by id, and can arrive for a
            // pass that started before a moderator withdrew Berean. Without this it flips
            // IsAIReviewCompleted on a removed row, resurrecting an assignment nobody re-made.
            //
            // REFUSED rather than treated as a no-op, unlike the remove path below: removing an
            // already-removed row is the same request answered twice, while writing to one is a
            // different request with nothing left to write to — answering it quietly would tell the
            // caller its write landed. Reported as not found, matching the read posture above
            // (§14.5) and the modify guard ApprovalService takes for the same case.
            //
            // After the permission gate at the top of this method, not before, following the remove
            // path: a caller who may not touch this row learns nothing about its deletion state
            // either way.
            ValidateStorageAIReviewerAssignmentIsNotDeleted(
                storageAIReviewerAssignment: maybeAIReviewerAssignment,
                aiReviewerAssignmentId: aiReviewerAssignment.Id);

            aiReviewerAssignment = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: aiReviewerAssignment,
                    storageEntity: maybeAIReviewerAssignment);

            ValidateAgainstStorageAIReviewerAssignmentOnModify(
                inputAIReviewerAssignment: aiReviewerAssignment,
                storageAIReviewerAssignment: maybeAIReviewerAssignment);

            AIReviewerAssignment updatedAIReviewerAssignment =
                await this.storageBroker.UpdateAIReviewerAssignmentAsync(
                    aiReviewerAssignment, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .AIReviewerAssignmentOnModifyingAIReviewerAssignmentSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<AIReviewerAssignment> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedAIReviewerAssignment);

            await this.eventBroker.PublishAIReviewerAssignmentAsync(
                envelope: outboundEnvelope,
                operation: AIReviewerAssignmentEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .AIReviewerAssignmentOnModifyingAIReviewerAssignmentSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedAIReviewerAssignment;
        }

        private async ValueTask<AIReviewerAssignment> DoRemoveAIReviewerAssignmentByIdAsync(
            Guid aiReviewerAssignmentId,
            string? deletionReason,
            EventEnvelope<AIReviewerAssignment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToManageAIReviewerAssignments(inboundEnvelope.SecurityContext);
            ValidateOnRemoveAIReviewerAssignmentById(aiReviewerAssignmentId, deletionReason);

            AIReviewerAssignment maybeAIReviewerAssignment =
                await this.storageBroker.SelectAIReviewerAssignmentByIdAsync(
                    aiReviewerAssignmentId, cancellationToken);

            ValidateStorageAIReviewerAssignment(maybeAIReviewerAssignment, aiReviewerAssignmentId);

            if (maybeAIReviewerAssignment.IsDeleted)
                return maybeAIReviewerAssignment;

            AIReviewerAssignment auditedAIReviewerAssignment =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeAIReviewerAssignment,
                    securityContext: inboundEnvelope.SecurityContext,
                    deletionReason: deletionReason);

            AIReviewerAssignment removedAIReviewerAssignment =
                await this.storageBroker.UpdateAIReviewerAssignmentAsync(
                    aiReviewerAssignment: auditedAIReviewerAssignment,
                    cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .AIReviewerAssignmentOnRemovingAIReviewerAssignmentByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<AIReviewerAssignment> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedAIReviewerAssignment);

            await this.eventBroker.PublishAIReviewerAssignmentAsync(
                envelope: outboundEnvelope,
                operation: AIReviewerAssignmentEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .AIReviewerAssignmentOnRemovingAIReviewerAssignmentByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedAIReviewerAssignment;
        }
    }
}
