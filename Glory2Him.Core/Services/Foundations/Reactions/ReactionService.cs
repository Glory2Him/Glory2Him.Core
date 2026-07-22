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
using Glory2Him.Core.Models.Foundations.Reactions;

namespace Glory2Him.Core.Services.Foundations.Reactions
{
    /// <summary>
    /// Foundation service for reactions. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain.
    /// </summary>
    public partial class ReactionService : IReactionService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeFactory eventEnvelopeFactory;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public ReactionService(
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

        public ValueTask<Reaction> AddReactionAsync(
            Reaction reaction,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateReactionIsNotNull(reaction);

                EventEnvelope<Reaction> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: reaction);

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

                return await this.storageBroker.SelectAllReactionsAsync(cancellationToken);
            });

        public ValueTask<Reaction> RetrieveReactionByIdAsync(
            Guid reactionId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveReactionById(reactionId);

                Reaction maybeReaction =
                    await this.storageBroker.SelectReactionByIdAsync(reactionId, cancellationToken);

                ValidateStorageReaction(maybeReaction, reactionId);

                return maybeReaction;
            });

        public ValueTask<Reaction> ModifyReactionAsync(
            Reaction reaction,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateReactionIsNotNull(reaction);

                EventEnvelope<Reaction> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: reaction);

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
                    await this.eventEnvelopeFactory.CreateAsync(content: removeRequest);

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
                    await this.eventEnvelopeFactory.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveReactionByIdAsync(
                    reactionId: reactionId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<Reaction> DoAddReactionAsync(
            Reaction reaction,
            EventEnvelope<Reaction> inboundEnvelope,
            CancellationToken cancellationToken)
        {
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
                await this.eventEnvelopeFactory.CreateNextAsync(
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
            reaction = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: reaction, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyReactionAsync(
                reaction: reaction,
                securityContext: inboundEnvelope.SecurityContext);

            Reaction maybeReaction = await this.storageBroker.SelectReactionByIdAsync(
                reactionId: reaction.Id,
                cancellationToken: cancellationToken);

            ValidateStorageReaction(maybeReaction, reactionId: reaction.Id);

            reaction = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: reaction,
                    storageEntity: maybeReaction);

            ValidateAgainstStorageReactionOnModify(
                inputReaction: reaction,
                storageReaction: maybeReaction);

            Reaction updatedReaction =
                await this.storageBroker.UpdateReactionAsync(reaction, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ReactionOnModifyingReactionSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Reaction> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
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
            ValidateOnRemoveReactionById(reactionId);

            Reaction maybeReaction =
                await this.storageBroker.SelectReactionByIdAsync(reactionId, cancellationToken);

            ValidateStorageReaction(maybeReaction, reactionId);

            if (maybeReaction.IsDeleted)
                return maybeReaction;

            if (deletionReason is not null)
                maybeReaction.DeletionReason = deletionReason;

            Reaction auditedReaction =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeReaction,
                    securityContext: inboundEnvelope.SecurityContext);

            Reaction removedReaction = await this.storageBroker.UpdateReactionAsync(
                reaction: auditedReaction,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ReactionOnRemovingReactionByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Reaction> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
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
                await this.eventEnvelopeFactory.CreateNextAsync(
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
