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
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;

namespace Glory2Him.Core.Services.Foundations.Reactions
{
    /// <summary>
    /// Foundation service for reactions. Every operation is both callable directly (the
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
    internal partial class ReactionService : IReactionService
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

        public ReactionService(
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

        public ValueTask<Reaction> AddReactionAsync(
            Reaction reaction,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateReactionIsNotNull(reaction);

                EventEnvelope<Reaction> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: reaction);

                return await DoAddReactionAsync(
                    reaction: reaction,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<Reaction>> RetrieveAllReactionsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the envelope exists to capture the ambient security context the
                // visibility filter runs against — the request payload is empty
                EventEnvelope<Reaction> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new Reaction());

                IQueryable<Reaction> allReactions =
                    await this.storageBroker.SelectAllReactionsAsync(cancellationToken);

                return await ApplyCollectionReadVisibilityFilterAsync(
                    reactions: allReactions,
                    securityContext: envelope.SecurityContext);
            });

        public ValueTask<Reaction> RetrieveReactionByIdAsync(
            Guid reactionId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new Reaction
                {
                    Id = reactionId
                };

                EventEnvelope<Reaction> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveReactionByIdAsync(
                    reactionId: reactionId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Reaction> ModifyReactionAsync(
            Reaction reaction,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateReactionIsNotNull(reaction);

                EventEnvelope<Reaction> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: reaction);

                return await DoModifyReactionAsync(
                    reaction: reaction,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Reaction> RemoveReactionByIdAsync(
            Guid reactionId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new Reaction
                {
                    Id = reactionId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<Reaction> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: removeRequest);

                return await DoRemoveReactionByIdAsync(
                    reactionId: reactionId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Reaction> HardRemoveReactionByIdAsync(
            Guid reactionId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new Reaction
                {
                    Id = reactionId
                };

                EventEnvelope<Reaction> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveReactionByIdAsync(
                    reactionId: reactionId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        // the shared read posture of design §14.1/§14.5/§14.6: a publicly visible version
        // is readable by anyone; a non-public version answers not-found — never
        // unauthorized — to everyone but the owner and the review roles, with the true
        // denial reason logged server-side only
        private async ValueTask<Reaction> DoRetrieveReactionByIdAsync(
            Guid reactionId,
            EventEnvelope<Reaction> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRetrieveReactionById(reactionId);

            Reaction maybeReaction =
                await this.storageBroker.SelectReactionByIdAsync(reactionId, cancellationToken);

            ValidateStorageReaction(maybeReaction, reactionId);

            if (maybeReaction.IsDeleted)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Reaction read denied. Reaction {reactionId} is " +
                        "soft-deleted; reported to the caller as not found.");

                throw new NotFoundReactionException(
                    message: $"Reaction not found with id: {reactionId}.");
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            bool isPubliclyVisible =
                maybeReaction.ApprovalStatus == ApprovalStatus.Approved
                    && maybeReaction.IsPublished
                    && (maybeReaction.PublishDate is null
                        || maybeReaction.PublishDate <= currentDateTime);

            if (isPubliclyVisible)
            {
                return maybeReaction;
            }

            SecurityContext? securityContext = inboundEnvelope.SecurityContext;

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Reaction read denied. Reaction {reactionId} is not " +
                        "publicly visible and the caller is not authenticated; reported to " +
                        "the caller as not found.");

                throw new NotFoundReactionException(
                    message: $"Reaction not found with id: {reactionId}.");
            }

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && maybeReaction.CreatedBy == actorUserId;

            if (isOwner is false && HasReviewRole(securityContext) is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Reaction read denied. Reaction {reactionId} " +
                        $"is not publicly visible and user \"{actorUserId}\" is neither the " +
                        "owner nor in a review role; reported to the caller as not found.");

                throw new NotFoundReactionException(
                    message: $"Reaction not found with id: {reactionId}.");
            }

            return maybeReaction;
        }

        // the collection twin of the single-row posture: a row the caller may not see
        // drops out of the set instead of erroring, so a collection read never reveals
        // how many non-public rows exist
        private async ValueTask<IQueryable<Reaction>> ApplyCollectionReadVisibilityFilterAsync(
            IQueryable<Reaction> reactions,
            SecurityContext? securityContext)
        {
            IQueryable<Reaction> visibleReactions = reactions.Where(reaction =>
                reaction.IsDeleted == false);

            bool isAuthenticated =
                securityContext is not null && securityContext.IsAuthenticated;

            if (isAuthenticated && HasReviewRole(securityContext!))
            {
                return visibleReactions;
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            string? actorUserId = isAuthenticated
                ? await this.securityAuditBroker.GetUserIdAsync(securityContext: securityContext!)
                : null;

            bool includeOwnReactions = string.IsNullOrWhiteSpace(actorUserId) is false;

            return visibleReactions.Where(reaction =>
                (reaction.ApprovalStatus == ApprovalStatus.Approved
                    && reaction.IsPublished
                    && (reaction.PublishDate == null
                        || reaction.PublishDate <= currentDateTime))
                || (includeOwnReactions && reaction.CreatedBy == actorUserId));
        }

        private async ValueTask<Reaction> DoAddReactionAsync(
            Reaction reaction,
            EventEnvelope<Reaction> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

            reaction = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(entity: reaction, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddReactionAsync(
                reaction: reaction,
                securityContext: inboundEnvelope.SecurityContext);

            Reaction addedReaction =
                await this.storageBroker.InsertReactionAsync(reaction, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ReactionOnAddingReactionSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Reaction> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedReaction);

            await this.eventBroker.PublishReactionAsync(
                envelope: outboundEnvelope,
                operation: ReactionEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ReactionOnAddingReactionSubscriptionName,
                cancellationToken: cancellationToken);

            return addedReaction;
        }

        private async ValueTask<Reaction> DoModifyReactionAsync(
            Reaction reaction,
            EventEnvelope<Reaction> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

            reaction = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: reaction, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyReactionAsync(
                reaction: reaction,
                securityContext: inboundEnvelope.SecurityContext);

            Reaction maybeReaction = await this.storageBroker.SelectReactionByIdAsync(
                reactionId: reaction.Id,
                cancellationToken: cancellationToken);

            ValidateStorageReaction(maybeReaction, reactionId: reaction.Id);

            bool mayTransitionApprovalStatus =
                await ValidateUserCanModifyStorageReactionAsync(
                    storageReaction: maybeReaction,
                    securityContext: inboundEnvelope.SecurityContext);

            reaction = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: reaction,
                    storageEntity: maybeReaction);

            ValidateAgainstStorageReactionOnModify(
                inputReaction: reaction,
                storageReaction: maybeReaction,
                mayTransitionApprovalStatus: mayTransitionApprovalStatus);

            Reaction updatedReaction =
                await this.storageBroker.UpdateReactionAsync(reaction, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ReactionOnModifyingReactionSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Reaction> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedReaction);

            await this.eventBroker.PublishReactionAsync(
                envelope: outboundEnvelope,
                operation: ReactionEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ReactionOnModifyingReactionSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedReaction;
        }

        private async ValueTask<Reaction> DoRemoveReactionByIdAsync(
            Guid reactionId,
            string? deletionReason,
            EventEnvelope<Reaction> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
            ValidateOnRemoveReactionById(reactionId, deletionReason);

            Reaction maybeReaction =
                await this.storageBroker.SelectReactionByIdAsync(reactionId, cancellationToken);

            ValidateStorageReaction(maybeReaction, reactionId);

            // permission comes before the idempotent short-circuit, so an unauthorized
            // caller learns nothing about the row's deletion state
            await ValidateUserCanRemoveStorageReactionAsync(
                storageReaction: maybeReaction,
                securityContext: inboundEnvelope.SecurityContext);

            if (maybeReaction.IsDeleted)
                return maybeReaction;

            Reaction auditedReaction =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeReaction,
                    securityContext: inboundEnvelope.SecurityContext,
                    deletionReason: deletionReason);

            Reaction removedReaction = await this.storageBroker.UpdateReactionAsync(
                reaction: auditedReaction,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ReactionOnRemovingReactionByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Reaction> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedReaction);

            await this.eventBroker.PublishReactionAsync(
                envelope: outboundEnvelope,
                operation: ReactionEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ReactionOnRemovingReactionByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedReaction;
        }

        private async ValueTask<Reaction> DoHardRemoveReactionByIdAsync(
            Guid reactionId,
            EventEnvelope<Reaction> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserCanHardRemoveReaction(inboundEnvelope.SecurityContext);
            ValidateOnHardRemoveReactionById(reactionId);

            Reaction maybeReaction =
                await this.storageBroker.SelectReactionByIdAsync(reactionId, cancellationToken);

            ValidateStorageReaction(maybeReaction, reactionId);

            Reaction deletedReaction =
                await this.storageBroker.DeleteReactionAsync(maybeReaction, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ReactionOnHardRemovingReactionByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Reaction> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedReaction);

            await this.eventBroker.PublishReactionAsync(
                envelope: outboundEnvelope,
                operation: ReactionEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ReactionOnHardRemovingReactionByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedReaction;
        }
    }
}
