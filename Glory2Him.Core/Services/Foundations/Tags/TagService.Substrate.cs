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
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Foundations.Tags;

namespace Glory2Him.Core.Services.Foundations.Tags
{
    /// <summary>
    /// The event path of the service: request handlers the event substrate dispatches to,
    /// one per request address (<c>Tag-Adding</c>, <c>-Modifying</c>,
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
    internal partial class TagService
    {
        public ValueTask<EventEnvelope<Tag>?> OnAddingTagAsync(
            EventEnvelope<Tag> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateTagEventEnvelopeAsync(
                    envelope, TagEventOperation.Adding);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Tag addedTag = await DoAddTagAsync(
                    tag: envelope.Content,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: addedTag);
            });

        public ValueTask<EventEnvelope<Tag>?> OnModifyingTagAsync(
            EventEnvelope<Tag> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateTagEventEnvelopeAsync(
                    envelope, TagEventOperation.Modifying);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.TagOnModifyingTagSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Tag modifiedTag = await DoModifyTagAsync(
                    tag: envelope.Content,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: modifiedTag);
            });

        public ValueTask<EventEnvelope<Tag>?> OnRemovingTagByIdAsync(
            EventEnvelope<Tag> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateTagEventEnvelopeAsync(
                    envelope, TagEventOperation.RemovingById);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.TagOnRemovingTagByIdSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Tag removedTag = await DoRemoveTagByIdAsync(
                    tagId: envelope.Content.Id,
                    deletionReason: envelope.Content.DeletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: removedTag);
            });

        public ValueTask<EventEnvelope<Tag>?> OnHardRemovingTagByIdAsync(
            EventEnvelope<Tag> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateTagEventEnvelopeAsync(
                    envelope, TagEventOperation.HardRemovingById);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.TagOnHardRemovingTagByIdSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Tag deletedTag = await DoHardRemoveTagByIdAsync(
                    tagId: envelope.Content.Id,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: deletedTag);
            });

        public ValueTask<EventEnvelope<Tag>?> OnRetrievingTagByIdAsync(
            EventEnvelope<Tag> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateTagEventEnvelopeAsync(
                    envelope, TagEventOperation.RetrievingById);

                // read-only: naturally idempotent, so no ProcessedEvents bookkeeping; the
                // shared do-work runs the visibility posture against the REQUEST envelope's
                // security context, not the ambient one
                Tag retrievedTag = await DoRetrieveTagByIdAsync(
                    tagId: envelope.Content.Id,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: retrievedTag);
            });

        public ValueTask<EventEnvelope<Tag>?> OnSubmittingTagAsync(
            EventEnvelope<Tag> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateTagEventEnvelopeAsync(
                    envelope, TagEventOperation.Submitting);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .TagOnSubmittingTagSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                // Submit owns only the status, so the id is the whole request; the envelope's
                // other fields are the caller's copy and never trusted by the do-work.
                Tag submittedTag =
                    await DoSubmitTagAsync(
                        tagId: envelope.Content.Id,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: submittedTag);
            });

        public ValueTask<EventEnvelope<Tag>?> OnApprovingTagAsync(
            EventEnvelope<Tag> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateTagEventEnvelopeAsync(
                    envelope, TagEventOperation.Approving);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .TagOnApprovingTagSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Tag approvedTag =
                    await DoTransitionTagApprovalAsync(
                        tag: envelope.Content,
                        inboundEnvelope: envelope,

                        // This envelope arrived over a PUBLIC event address and its security
                        // context was deserialized, not authenticated (§14.6 rule 4). A caller
                        // who could assert the system identity here would be granted the
                        // workflow's own authority — including the override out of a terminal
                        // state — simply by setting a JSON property. The claim is discarded and
                        // the caller is treated as the ordinary unprivileged one they are.
                        isSystemIdentityAdmissible: false,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: approvedTag);
            });

        private async ValueTask<bool> AlreadyProcessedAsync(
            EventEnvelope<Tag> envelope,
            string receiverName,
            CancellationToken cancellationToken) =>
            await this.storageBroker.SelectProcessedEventExistsAsync(
                eventId: envelope.Metadata.EventId,
                receiverName: receiverName,
                cancellationToken: cancellationToken);

        private async ValueTask RecordEventProcessedAsync(
            EventEnvelope<Tag> envelope,
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
