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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;

namespace Glory2Him.Core.Services.Foundations.Approvals
{
    /// <summary>
    /// The event path of the service: request handlers the event substrate dispatches to,
    /// one per request address (<c>Approval-Adding</c>, <c>-Modifying</c>,
    /// <c>-RemovingById</c>, <c>-HardRemovingById</c>, <c>-RetrievingById</c>). Handlers
    /// receive the full request envelope — including the original caller's
    /// <c>SecurityContext</c> — converge on the same private <c>DoXAsync</c> methods the
    /// non-event path uses (which publish the past-tense facts and record both the inbound
    /// and outbound event ids in the <c>ProcessedEvents</c> table), and return the outcome
    /// as the delivery's reply envelope. Mutating handlers check that table first so replayed
    /// or duplicated requests — including a published fact ever looping back into a request
    /// handler — are not applied twice; a deduplicated delivery replies <c>null</c>. Failures
    /// are categorized into the service's typed exceptions and rethrown so the substrate
    /// records the delivery as <c>Error</c> and drives retries; they are never swallowed.
    /// </summary>
    public partial class ApprovalService
    {
        public ValueTask<EventEnvelope<Approval>?> OnAddingApprovalAsync(
            EventEnvelope<Approval> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalEventEnvelope(envelope);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.ApprovalOnAddingApprovalSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Approval addedApproval = await DoAddApprovalAsync(
                    approval: envelope.Content,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: addedApproval);
            });

        public ValueTask<EventEnvelope<Approval>?> OnModifyingApprovalAsync(
            EventEnvelope<Approval> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalEventEnvelope(envelope);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.ApprovalOnModifyingApprovalSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Approval modifiedApproval = await DoModifyApprovalAsync(
                    approval: envelope.Content,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: modifiedApproval);
            });

        public ValueTask<EventEnvelope<Approval>?> OnRemovingApprovalByIdAsync(
            EventEnvelope<Approval> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalEventEnvelope(envelope);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.ApprovalOnRemovingApprovalByIdSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Approval removedApproval = await DoRemoveApprovalByIdAsync(
                    approvalId: envelope.Content.Id,
                    deletionReason: envelope.Content.DeletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: removedApproval);
            });

        public ValueTask<EventEnvelope<Approval>?> OnHardRemovingApprovalByIdAsync(
            EventEnvelope<Approval> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalEventEnvelope(envelope);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.ApprovalOnHardRemovingApprovalByIdSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Approval deletedApproval = await DoHardRemoveApprovalByIdAsync(
                    approvalId: envelope.Content.Id,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: deletedApproval);
            });

        public ValueTask<EventEnvelope<Approval>?> OnRetrievingApprovalByIdAsync(
            EventEnvelope<Approval> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalEventEnvelope(envelope);

                // read-only: naturally idempotent, so no ProcessedEvents bookkeeping
                Approval retrievedApproval = await RetrieveApprovalByIdAsync(
                    approvalId: envelope.Content.Id,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: retrievedApproval);
            });

        private async ValueTask<bool> AlreadyProcessedAsync(
            EventEnvelope<Approval> envelope,
            string receiverName,
            CancellationToken cancellationToken) =>
            await this.storageBroker.SelectProcessedEventExistsAsync(
                eventId: envelope.Metadata.EventId,
                receiverName: receiverName,
                cancellationToken: cancellationToken);

        private async ValueTask RecordEventProcessedAsync(
            EventEnvelope<Approval> envelope,
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
