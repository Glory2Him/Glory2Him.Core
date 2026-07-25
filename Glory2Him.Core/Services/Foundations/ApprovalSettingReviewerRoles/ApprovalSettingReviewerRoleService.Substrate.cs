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
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;

namespace Glory2Him.Core.Services.Foundations.ApprovalSettingReviewerRoles
{
    /// <summary>
    /// The event path of the service: request handlers the event substrate dispatches to,
    /// one per request address (<c>ApprovalSettingReviewerRole-Adding</c>, <c>-Modifying</c>,
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
    public partial class ApprovalSettingReviewerRoleService
    {
        public ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?> OnAddingApprovalSettingReviewerRoleAsync(
            EventEnvelope<ApprovalSettingReviewerRole> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalSettingReviewerRoleEventEnvelope(envelope);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnAddingApprovalSettingReviewerRoleSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                ApprovalSettingReviewerRole addedApprovalSettingReviewerRole = await DoAddApprovalSettingReviewerRoleAsync(
                    approvalSettingReviewerRole: envelope.Content,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: addedApprovalSettingReviewerRole);
            });

        public ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?> OnModifyingApprovalSettingReviewerRoleAsync(
            EventEnvelope<ApprovalSettingReviewerRole> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalSettingReviewerRoleEventEnvelope(envelope);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnModifyingApprovalSettingReviewerRoleSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                ApprovalSettingReviewerRole modifiedApprovalSettingReviewerRole = await DoModifyApprovalSettingReviewerRoleAsync(
                    approvalSettingReviewerRole: envelope.Content,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: modifiedApprovalSettingReviewerRole);
            });

        public ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?> OnRemovingApprovalSettingReviewerRoleByIdAsync(
            EventEnvelope<ApprovalSettingReviewerRole> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalSettingReviewerRoleEventEnvelope(envelope);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                ApprovalSettingReviewerRole removedApprovalSettingReviewerRole = await DoRemoveApprovalSettingReviewerRoleByIdAsync(
                    approvalSettingReviewerRoleId: envelope.Content.Id,
                    deletionReason: envelope.Content.DeletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: removedApprovalSettingReviewerRole);
            });

        public ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?> OnHardRemovingApprovalSettingReviewerRoleByIdAsync(
            EventEnvelope<ApprovalSettingReviewerRole> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalSettingReviewerRoleEventEnvelope(envelope);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnHardRemovingApprovalSettingReviewerRoleByIdSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                ApprovalSettingReviewerRole deletedApprovalSettingReviewerRole = await DoHardRemoveApprovalSettingReviewerRoleByIdAsync(
                    approvalSettingReviewerRoleId: envelope.Content.Id,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: deletedApprovalSettingReviewerRole);
            });

        public ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?> OnRetrievingApprovalSettingReviewerRoleByIdAsync(
            EventEnvelope<ApprovalSettingReviewerRole> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalSettingReviewerRoleEventEnvelope(envelope);

                // read-only: naturally idempotent, so no ProcessedEvents bookkeeping
                ApprovalSettingReviewerRole retrievedApprovalSettingReviewerRole = await RetrieveApprovalSettingReviewerRoleByIdAsync(
                    approvalSettingReviewerRoleId: envelope.Content.Id,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: retrievedApprovalSettingReviewerRole);
            });

        private async ValueTask<bool> AlreadyProcessedAsync(
            EventEnvelope<ApprovalSettingReviewerRole> envelope,
            string receiverName,
            CancellationToken cancellationToken) =>
            await this.storageBroker.SelectProcessedEventExistsAsync(
                eventId: envelope.Metadata.EventId,
                receiverName: receiverName,
                cancellationToken: cancellationToken);

        private async ValueTask RecordEventProcessedAsync(
            EventEnvelope<ApprovalSettingReviewerRole> envelope,
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
