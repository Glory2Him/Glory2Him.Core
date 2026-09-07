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

using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;

namespace Glory2Him.Core.Services.Foundations.AIReviewerAssignments
{
    /// <summary>
    /// The event path of the service: request handlers the event substrate dispatches to,
    /// one per request address (<c>AIReviewerAssignment-Adding</c>, <c>-Modifying</c>,
    /// <c>-RemovingById</c>, <c>-RetrievingById</c>). Handlers receive the full request
    /// envelope — including the original caller's <c>SecurityContext</c> — converge on the same
    /// private <c>DoXAsync</c> methods the non-event path uses (which publish the past-tense
    /// facts and record both the inbound and outbound event ids in the <c>ProcessedEvents</c>
    /// table), and return the outcome as the delivery's reply envelope. Mutating handlers check
    /// that table first so replayed or duplicated requests — including a published fact ever
    /// looping back into a request handler — are not applied twice; a deduplicated delivery
    /// replies <c>null</c>. Failures are categorized into the service's typed exceptions and
    /// rethrown so the substrate records the delivery as <c>Error</c> and drives retries; they
    /// are never swallowed.
    ///
    /// <para>Nothing wires these handlers to a live subscription yet — the future automated
    /// review process (design §8.6.2, GitHub issue #354) that would call through this path has
    /// not been built. They exist for structural consistency with every other foundation
    /// service in this codebase (the-standard-foundations skill rules ts-foundations-013/014),
    /// not because anything reaches them today.</para>
    /// </summary>
    internal partial class AIReviewerAssignmentService
    {
        public ValueTask<EventEnvelope<AIReviewerAssignment>?> OnAddingAIReviewerAssignmentAsync(
            EventEnvelope<AIReviewerAssignment> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateAIReviewerAssignmentEventEnvelopeAsync(
                    envelope, AIReviewerAssignmentEventOperation.Adding);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .AIReviewerAssignmentOnAddingAIReviewerAssignmentSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                AIReviewerAssignment addedAIReviewerAssignment = await DoAddAIReviewerAssignmentAsync(
                    aiReviewerAssignment: envelope.Content,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: addedAIReviewerAssignment);
            });

        public ValueTask<EventEnvelope<AIReviewerAssignment>?> OnModifyingAIReviewerAssignmentAsync(
            EventEnvelope<AIReviewerAssignment> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateAIReviewerAssignmentEventEnvelopeAsync(
                    envelope, AIReviewerAssignmentEventOperation.Modifying);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .AIReviewerAssignmentOnModifyingAIReviewerAssignmentSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                AIReviewerAssignment modifiedAIReviewerAssignment = await DoModifyAIReviewerAssignmentAsync(
                    aiReviewerAssignment: envelope.Content,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: modifiedAIReviewerAssignment);
            });

        public ValueTask<EventEnvelope<AIReviewerAssignment>?> OnRemovingAIReviewerAssignmentByIdAsync(
            EventEnvelope<AIReviewerAssignment> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateAIReviewerAssignmentEventEnvelopeAsync(
                    envelope, AIReviewerAssignmentEventOperation.RemovingById);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .AIReviewerAssignmentOnRemovingAIReviewerAssignmentByIdSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                AIReviewerAssignment removedAIReviewerAssignment =
                    await DoRemoveAIReviewerAssignmentByIdAsync(
                        aiReviewerAssignmentId: envelope.Content.Id,
                        deletionReason: envelope.Content.DeletionReason,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: removedAIReviewerAssignment);
            });

        public ValueTask<EventEnvelope<AIReviewerAssignment>?> OnRetrievingAIReviewerAssignmentByIdAsync(
            EventEnvelope<AIReviewerAssignment> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateAIReviewerAssignmentEventEnvelopeAsync(
                    envelope, AIReviewerAssignmentEventOperation.RetrievingById);

                // read-only: naturally idempotent, so no ProcessedEvents bookkeeping; the
                // shared do-work runs the visibility posture against the REQUEST envelope's
                // security context, not the ambient one
                AIReviewerAssignment retrievedAIReviewerAssignment =
                    await DoRetrieveAIReviewerAssignmentByIdAsync(
                        aiReviewerAssignmentId: envelope.Content.Id,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: retrievedAIReviewerAssignment);
            });

        private async ValueTask<bool> AlreadyProcessedAsync(
            EventEnvelope<AIReviewerAssignment> envelope,
            string receiverName,
            CancellationToken cancellationToken) =>
            await this.storageBroker.SelectProcessedEventExistsAsync(
                eventId: envelope.Metadata.EventId,
                receiverName: receiverName,
                cancellationToken: cancellationToken);

        private async ValueTask RecordEventProcessedAsync(
            EventEnvelope<AIReviewerAssignment> envelope,
            string receiverName,
            CancellationToken cancellationToken) =>
            await this.storageBroker.InsertProcessedEventAsync(
                processedEvent: new ProcessedEvent
                {
                    Id = await this.identifierBroker.GetIdentifierAsync(),
                    EventId = envelope.Metadata.EventId,
                    ReceiverName = receiverName,
                    ProcessedAt = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync()
                },
                cancellationToken: cancellationToken);
    }
}
