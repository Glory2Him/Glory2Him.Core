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
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;

namespace Glory2Him.Core.Services.Foundations.ApprovalSettingPublisherRoles
{
    /// <summary>
    /// The event path of the service: request handlers the event substrate dispatches to,
    /// one per request address (<c>ApprovalSettingPublisherRole-Adding</c>, <c>-Modifying</c>,
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
    internal partial class ApprovalSettingPublisherRoleService
    {
        public ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?> OnAddingApprovalSettingPublisherRoleAsync(
            EventEnvelope<ApprovalSettingPublisherRole> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalSettingPublisherRoleEventEnvelope(envelope);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnAddingApprovalSettingPublisherRoleSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                ApprovalSettingPublisherRole addedApprovalSettingPublisherRole = await DoAddApprovalSettingPublisherRoleAsync(
                    approvalSettingPublisherRole: envelope.Content,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: addedApprovalSettingPublisherRole);
            });

        public ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?> OnModifyingApprovalSettingPublisherRoleAsync(
            EventEnvelope<ApprovalSettingPublisherRole> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalSettingPublisherRoleEventEnvelope(envelope);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                ApprovalSettingPublisherRole modifiedApprovalSettingPublisherRole = await DoModifyApprovalSettingPublisherRoleAsync(
                    approvalSettingPublisherRole: envelope.Content,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: modifiedApprovalSettingPublisherRole);
            });

        public ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?> OnRemovingApprovalSettingPublisherRoleByIdAsync(
            EventEnvelope<ApprovalSettingPublisherRole> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalSettingPublisherRoleEventEnvelope(envelope);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnRemovingApprovalSettingPublisherRoleByIdSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                ApprovalSettingPublisherRole removedApprovalSettingPublisherRole = await DoRemoveApprovalSettingPublisherRoleByIdAsync(
                    approvalSettingPublisherRoleId: envelope.Content.Id,
                    deletionReason: envelope.Content.DeletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: removedApprovalSettingPublisherRole);
            });

        public ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?> OnHardRemovingApprovalSettingPublisherRoleByIdAsync(
            EventEnvelope<ApprovalSettingPublisherRole> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalSettingPublisherRoleEventEnvelope(envelope);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnHardRemovingApprovalSettingPublisherRoleByIdSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                ApprovalSettingPublisherRole deletedApprovalSettingPublisherRole = await DoHardRemoveApprovalSettingPublisherRoleByIdAsync(
                    approvalSettingPublisherRoleId: envelope.Content.Id,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: deletedApprovalSettingPublisherRole);
            });

        public ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?> OnRetrievingApprovalSettingPublisherRoleByIdAsync(
            EventEnvelope<ApprovalSettingPublisherRole> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalSettingPublisherRoleEventEnvelope(envelope);

                // read-only: naturally idempotent, so no ProcessedEvents bookkeeping
                ApprovalSettingPublisherRole retrievedApprovalSettingPublisherRole = await RetrieveApprovalSettingPublisherRoleByIdAsync(
                    approvalSettingPublisherRoleId: envelope.Content.Id,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: retrievedApprovalSettingPublisherRole);
            });

        private async ValueTask<bool> AlreadyProcessedAsync(
            EventEnvelope<ApprovalSettingPublisherRole> envelope,
            string receiverName,
            CancellationToken cancellationToken) =>
            await this.storageBroker.SelectProcessedEventExistsAsync(
                eventId: envelope.Metadata.EventId,
                receiverName: receiverName,
                cancellationToken: cancellationToken);

        private async ValueTask RecordEventProcessedAsync(
            EventEnvelope<ApprovalSettingPublisherRole> envelope,
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
