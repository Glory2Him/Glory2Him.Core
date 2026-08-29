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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviewRequests
{
    /// <summary>
    /// The event path of the service: request handlers the event substrate dispatches to,
    /// one per request address (<c>ApprovalReviewRequest-Adding</c>, <c>-RemovingById</c>,
    /// <c>-HardRemovingById</c>, <c>-RetrievingById</c>). Handlers receive the full request
    /// envelope — including the original caller's <c>SecurityContext</c> — converge on the same
    /// private <c>DoXAsync</c> methods the non-event path uses (which publish the past-tense
    /// facts and record both the inbound and outbound event ids in the <c>ProcessedEvents</c>
    /// table), and return the outcome as the delivery's reply envelope. Mutating handlers check
    /// that table first so replayed or duplicated requests — including a published fact ever
    /// looping back into a request handler — are not applied twice; a deduplicated delivery
    /// replies <c>null</c>. Failures are categorized into the service's typed exceptions and
    /// rethrown so the substrate records the delivery as <c>Error</c> and drives retries; they
    /// are never swallowed.
    /// </summary>
    internal partial class ApprovalReviewRequestService
    {
        public ValueTask<EventEnvelope<ApprovalReviewRequest>?> OnAddingApprovalReviewRequestAsync(
            EventEnvelope<ApprovalReviewRequest> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateApprovalReviewRequestEventEnvelopeAsync(
                    envelope, ApprovalReviewRequestEventOperation.Adding);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .ApprovalReviewRequestOnAddingApprovalReviewRequestSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                ApprovalReviewRequest addedApprovalReviewRequest = await DoAddApprovalReviewRequestAsync(
                    approvalReviewRequest: envelope.Content,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: addedApprovalReviewRequest);
            });

        public ValueTask<EventEnvelope<ApprovalReviewRequest>?> OnRemovingApprovalReviewRequestByIdAsync(
            EventEnvelope<ApprovalReviewRequest> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateApprovalReviewRequestEventEnvelopeAsync(
                    envelope, ApprovalReviewRequestEventOperation.RemovingById);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .ApprovalReviewRequestOnRemovingApprovalReviewRequestByIdSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                ApprovalReviewRequest removedApprovalReviewRequest =
                    await DoRemoveApprovalReviewRequestByIdAsync(
                        approvalReviewRequestId: envelope.Content.Id,
                        deletionReason: envelope.Content.DeletionReason,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: removedApprovalReviewRequest);
            });

        public ValueTask<EventEnvelope<ApprovalReviewRequest>?> OnHardRemovingApprovalReviewRequestByIdAsync(
            EventEnvelope<ApprovalReviewRequest> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateApprovalReviewRequestEventEnvelopeAsync(
                    envelope, ApprovalReviewRequestEventOperation.HardRemovingById);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .ApprovalReviewRequestOnHardRemovingApprovalReviewRequestByIdSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                ApprovalReviewRequest deletedApprovalReviewRequest =
                    await DoHardRemoveApprovalReviewRequestByIdAsync(
                        approvalReviewRequestId: envelope.Content.Id,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: deletedApprovalReviewRequest);
            });

        public ValueTask<EventEnvelope<ApprovalReviewRequest>?> OnRetrievingApprovalReviewRequestByIdAsync(
            EventEnvelope<ApprovalReviewRequest> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateApprovalReviewRequestEventEnvelopeAsync(
                    envelope, ApprovalReviewRequestEventOperation.RetrievingById);

                // read-only: naturally idempotent, so no ProcessedEvents bookkeeping; the
                // shared do-work runs the visibility posture against the REQUEST envelope's
                // security context, not the ambient one
                ApprovalReviewRequest retrievedApprovalReviewRequest =
                    await DoRetrieveApprovalReviewRequestByIdAsync(
                        approvalReviewRequestId: envelope.Content.Id,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: retrievedApprovalReviewRequest);
            });

        private async ValueTask<bool> AlreadyProcessedAsync(
            EventEnvelope<ApprovalReviewRequest> envelope,
            string receiverName,
            CancellationToken cancellationToken) =>
            await this.storageBroker.SelectProcessedEventExistsAsync(
                eventId: envelope.Metadata.EventId,
                receiverName: receiverName,
                cancellationToken: cancellationToken);

        private async ValueTask RecordEventProcessedAsync(
            EventEnvelope<ApprovalReviewRequest> envelope,
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
