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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.BibleReferences.Exceptions;

namespace Glory2Him.Core.Services.Foundations.BibleReferences
{
    /// <summary>
    /// Foundation service for bible references. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain. Per design §14.6 the foundation enforces security itself — the
    /// contribution gate on writes, owner-or-moderation-role write permission (removal by
    /// owner or Admin, hard removal by Admin only), and the §14.1/§14.5 read visibility
    /// posture — never assuming an upstream orchestration already gated the caller.
    /// </summary>
    internal partial class BibleReferenceService : IBibleReferenceService
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

        public BibleReferenceService(
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

        public ValueTask<BibleReference> AddBibleReferenceAsync(
            BibleReference bibleReference,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateBibleReferenceIsNotNull(bibleReference);

                EventEnvelope<BibleReference> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: bibleReference);

                return await DoAddBibleReferenceAsync(
                    bibleReference: bibleReference,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<BibleReference>> RetrieveAllBibleReferencesAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the envelope exists to capture the ambient security context the
                // visibility filter runs against — the request payload is empty
                EventEnvelope<BibleReference> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new BibleReference());

                IQueryable<BibleReference> allBibleReferences =
                    await this.storageBroker.SelectAllBibleReferencesAsync(cancellationToken);

                return await ApplyCollectionReadVisibilityFilterAsync(
                    bibleReferences: allBibleReferences,
                    securityContext: envelope.SecurityContext);
            });

        public ValueTask<BibleReference> RetrieveBibleReferenceByIdAsync(
            Guid bibleReferenceId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new BibleReference
                {
                    Id = bibleReferenceId
                };

                EventEnvelope<BibleReference> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveBibleReferenceByIdAsync(
                    bibleReferenceId: bibleReferenceId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<BibleReference> ModifyBibleReferenceAsync(
            BibleReference bibleReference,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateBibleReferenceIsNotNull(bibleReference);

                EventEnvelope<BibleReference> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: bibleReference);

                return await DoModifyBibleReferenceAsync(
                    bibleReference: bibleReference,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<BibleReference> RemoveBibleReferenceByIdAsync(
            Guid bibleReferenceId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new BibleReference
                {
                    Id = bibleReferenceId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<BibleReference> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: removeRequest);

                return await DoRemoveBibleReferenceByIdAsync(
                    bibleReferenceId: bibleReferenceId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<BibleReference> HardRemoveBibleReferenceByIdAsync(
            Guid bibleReferenceId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new BibleReference
                {
                    Id = bibleReferenceId
                };

                EventEnvelope<BibleReference> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveBibleReferenceByIdAsync(
                    bibleReferenceId: bibleReferenceId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        // the shared read posture of design §14.1/§14.5/§14.6: a publicly visible version
        // is readable by anyone; a non-public version answers not-found — never
        // unauthorized — to everyone but the owner and the review roles, with the true
        // denial reason logged server-side only
        private async ValueTask<BibleReference> DoRetrieveBibleReferenceByIdAsync(
            Guid bibleReferenceId,
            EventEnvelope<BibleReference> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRetrieveBibleReferenceById(bibleReferenceId);

            BibleReference maybeBibleReference = await this.storageBroker.SelectBibleReferenceByIdAsync(
                bibleReferenceId: bibleReferenceId,
                cancellationToken: cancellationToken);

            ValidateStorageBibleReference(maybeBibleReference, bibleReferenceId);

            if (maybeBibleReference.IsDeleted)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Bible reference read denied. Bible reference {bibleReferenceId} is " +
                        "soft-deleted; reported to the caller as not found.");

                throw new NotFoundBibleReferenceException(
                    message: $"Bible reference not found with id: {bibleReferenceId}.");
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            bool isPubliclyVisible =
                maybeBibleReference.ApprovalStatus == ApprovalStatus.Approved
                    && maybeBibleReference.IsPublished
                    && (maybeBibleReference.PublishDate is null
                        || maybeBibleReference.PublishDate <= currentDateTime);

            if (isPubliclyVisible)
            {
                return maybeBibleReference;
            }

            SecurityContext? securityContext = inboundEnvelope.SecurityContext;

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Bible reference read denied. Bible reference {bibleReferenceId} is not " +
                        "publicly visible and the caller is not authenticated; reported to " +
                        "the caller as not found.");

                throw new NotFoundBibleReferenceException(
                    message: $"Bible reference not found with id: {bibleReferenceId}.");
            }

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && maybeBibleReference.CreatedBy == actorUserId;

            if (isOwner is false && HasReviewRole(securityContext) is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Bible reference read denied. Bible reference {bibleReferenceId} " +
                        $"is not publicly visible and user \"{actorUserId}\" is neither the " +
                        "owner nor in a review role; reported to the caller as not found.");

                throw new NotFoundBibleReferenceException(
                    message: $"Bible reference not found with id: {bibleReferenceId}.");
            }

            return maybeBibleReference;
        }

        // the collection twin of the single-row posture: a row the caller may not see
        // drops out of the set instead of erroring, so a collection read never reveals
        // how many non-public rows exist
        private async ValueTask<IQueryable<BibleReference>> ApplyCollectionReadVisibilityFilterAsync(
            IQueryable<BibleReference> bibleReferences,
            SecurityContext? securityContext)
        {
            IQueryable<BibleReference> visibleBibleReferences = bibleReferences.Where(bibleReference =>
                bibleReference.IsDeleted == false);

            bool isAuthenticated =
                securityContext is not null && securityContext.IsAuthenticated;

            if (isAuthenticated && HasReviewRole(securityContext!))
            {
                return visibleBibleReferences;
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            string? actorUserId = isAuthenticated
                ? await this.securityAuditBroker.GetUserIdAsync(securityContext: securityContext!)
                : null;

            bool includeOwnBibleReferences = string.IsNullOrWhiteSpace(actorUserId) is false;

            return visibleBibleReferences.Where(bibleReference =>
                (bibleReference.ApprovalStatus == ApprovalStatus.Approved
                    && bibleReference.IsPublished
                    && (bibleReference.PublishDate == null
                        || bibleReference.PublishDate <= currentDateTime))
                || (includeOwnBibleReferences && bibleReference.CreatedBy == actorUserId));
        }

        private async ValueTask<BibleReference> DoAddBibleReferenceAsync(
            BibleReference bibleReference,
            EventEnvelope<BibleReference> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

            bibleReference = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(entity: bibleReference, securityContext: inboundEnvelope.SecurityContext);

            bibleReference.ScriptureHtml = SanitizeScriptureHtml(bibleReference.ScriptureHtml);

            await ValidateOnAddBibleReferenceAsync(
                bibleReference: bibleReference,
                securityContext: inboundEnvelope.SecurityContext);

            BibleReference addedBibleReference =
                await this.storageBroker.InsertBibleReferenceAsync(bibleReference, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.BibleReferenceOnAddingBibleReferenceSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<BibleReference> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedBibleReference);

            await this.eventBroker.PublishBibleReferenceAsync(
                envelope: outboundEnvelope,
                operation: BibleReferenceEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.BibleReferenceOnAddingBibleReferenceSubscriptionName,
                cancellationToken: cancellationToken);

            return addedBibleReference;
        }

        private async ValueTask<BibleReference> DoModifyBibleReferenceAsync(
            BibleReference bibleReference,
            EventEnvelope<BibleReference> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

            bibleReference = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: bibleReference, securityContext: inboundEnvelope.SecurityContext);

            bibleReference.ScriptureHtml = SanitizeScriptureHtml(bibleReference.ScriptureHtml);

            await ValidateOnModifyBibleReferenceAsync(
                bibleReference: bibleReference,
                securityContext: inboundEnvelope.SecurityContext);

            BibleReference maybeBibleReference = await this.storageBroker.SelectBibleReferenceByIdAsync(
                bibleReferenceId: bibleReference.Id,
                cancellationToken: cancellationToken);

            ValidateStorageBibleReference(maybeBibleReference, bibleReferenceId: bibleReference.Id);

            bool mayTransitionApprovalStatus =
                await ValidateUserCanModifyStorageBibleReferenceAsync(
                    storageBibleReference: maybeBibleReference,
                    securityContext: inboundEnvelope.SecurityContext);

            // Checked AFTER write permission so the refusal cannot be used to read a row's
            // approval state without the standing to see it.
            ValidateStorageBibleReferenceIsNotTerminal(maybeBibleReference);

            bibleReference = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: bibleReference,
                    storageEntity: maybeBibleReference);

            ValidateAgainstStorageBibleReferenceOnModify(
                inputBibleReference: bibleReference,
                storageBibleReference: maybeBibleReference,
                mayTransitionApprovalStatus: mayTransitionApprovalStatus);

            BibleReference updatedBibleReference =
                await this.storageBroker.UpdateBibleReferenceAsync(bibleReference, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.BibleReferenceOnModifyingBibleReferenceSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<BibleReference> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedBibleReference);

            await this.eventBroker.PublishBibleReferenceAsync(
                envelope: outboundEnvelope,
                operation: BibleReferenceEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.BibleReferenceOnModifyingBibleReferenceSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedBibleReference;
        }

        private async ValueTask<BibleReference> DoRemoveBibleReferenceByIdAsync(
            Guid bibleReferenceId,
            string? deletionReason,
            EventEnvelope<BibleReference> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
            ValidateOnRemoveBibleReferenceById(bibleReferenceId, deletionReason);

            BibleReference maybeBibleReference =
                await this.storageBroker.SelectBibleReferenceByIdAsync(bibleReferenceId, cancellationToken);

            ValidateStorageBibleReference(maybeBibleReference, bibleReferenceId);

            // permission comes before the idempotent short-circuit, so an unauthorized
            // caller learns nothing about the row's deletion state
            await ValidateUserCanRemoveStorageBibleReferenceAsync(
                storageBibleReference: maybeBibleReference,
                securityContext: inboundEnvelope.SecurityContext);

            if (maybeBibleReference.IsDeleted)
                return maybeBibleReference;

            BibleReference auditedBibleReference =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeBibleReference,
                    securityContext: inboundEnvelope.SecurityContext,
                    deletionReason: deletionReason);

            BibleReference removedBibleReference = await this.storageBroker.UpdateBibleReferenceAsync(
                bibleReference: auditedBibleReference,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.BibleReferenceOnRemovingBibleReferenceByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<BibleReference> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedBibleReference);

            await this.eventBroker.PublishBibleReferenceAsync(
                envelope: outboundEnvelope,
                operation: BibleReferenceEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.BibleReferenceOnRemovingBibleReferenceByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedBibleReference;
        }

        private async ValueTask<BibleReference> DoHardRemoveBibleReferenceByIdAsync(
            Guid bibleReferenceId,
            EventEnvelope<BibleReference> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserCanHardRemoveBibleReference(inboundEnvelope.SecurityContext);
            ValidateOnHardRemoveBibleReferenceById(bibleReferenceId);

            BibleReference maybeBibleReference =
                await this.storageBroker.SelectBibleReferenceByIdAsync(bibleReferenceId, cancellationToken);

            ValidateStorageBibleReference(maybeBibleReference, bibleReferenceId);

            BibleReference deletedBibleReference =
                await this.storageBroker.DeleteBibleReferenceAsync(maybeBibleReference, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.BibleReferenceOnHardRemovingBibleReferenceByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<BibleReference> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedBibleReference);

            await this.eventBroker.PublishBibleReferenceAsync(
                envelope: outboundEnvelope,
                operation: BibleReferenceEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.BibleReferenceOnHardRemovingBibleReferenceByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedBibleReference;
        }
    }
}
