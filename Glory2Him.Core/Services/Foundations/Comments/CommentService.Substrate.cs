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
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;

namespace Glory2Him.Core.Services.Foundations.Comments
{
    /// <summary>
    /// The event path of the service: request handlers the event substrate dispatches to,
    /// one per request address (<c>Comment-Adding</c>, <c>-Modifying</c>,
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
    internal partial class CommentService
    {
        public ValueTask<EventEnvelope<Comment>?> OnAddingCommentAsync(
            EventEnvelope<Comment> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateCommentEventEnvelopeAsync(
                    envelope, CommentEventOperation.Adding);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.CommentOnAddingCommentSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Comment addedComment = await DoAddCommentAsync(
                    comment: envelope.Content,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: addedComment);
            });

        public ValueTask<EventEnvelope<Comment>?> OnModifyingCommentAsync(
            EventEnvelope<Comment> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateCommentEventEnvelopeAsync(
                    envelope, CommentEventOperation.Modifying);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.CommentOnModifyingCommentSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Comment modifiedComment = await DoModifyCommentAsync(
                    comment: envelope.Content,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: modifiedComment);
            });

        public ValueTask<EventEnvelope<Comment>?> OnRemovingCommentByIdAsync(
            EventEnvelope<Comment> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateCommentEventEnvelopeAsync(
                    envelope, CommentEventOperation.RemovingById);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.CommentOnRemovingCommentByIdSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Comment removedComment = await DoRemoveCommentByIdAsync(
                    commentId: envelope.Content.Id,
                    deletionReason: envelope.Content.DeletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: removedComment);
            });

        public ValueTask<EventEnvelope<Comment>?> OnHardRemovingCommentByIdAsync(
            EventEnvelope<Comment> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateCommentEventEnvelopeAsync(
                    envelope, CommentEventOperation.HardRemovingById);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers.CommentOnHardRemovingCommentByIdSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Comment deletedComment = await DoHardRemoveCommentByIdAsync(
                    commentId: envelope.Content.Id,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: deletedComment);
            });

        public ValueTask<EventEnvelope<Comment>?> OnRetrievingCommentByIdAsync(
            EventEnvelope<Comment> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateCommentEventEnvelopeAsync(
                    envelope, CommentEventOperation.RetrievingById);

                // read-only: naturally idempotent, so no ProcessedEvents bookkeeping; the
                // shared do-work runs the visibility posture against the REQUEST envelope's
                // security context, not the ambient one
                Comment retrievedComment = await DoRetrieveCommentByIdAsync(
                    commentId: envelope.Content.Id,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: retrievedComment);
            });

        public ValueTask<EventEnvelope<Comment>?> OnSubmittingCommentAsync(
            EventEnvelope<Comment> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateCommentEventEnvelopeAsync(
                    envelope, CommentEventOperation.Submitting);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .CommentOnSubmittingCommentSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                // Submit owns only the status, so the id is the whole request; the envelope's
                // other fields are the caller's copy and never trusted by the do-work.
                Comment submittedComment =
                    await DoSubmitCommentAsync(
                        commentId: envelope.Content.Id,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: submittedComment);
            });

        public ValueTask<EventEnvelope<Comment>?> OnApprovingCommentAsync(
            EventEnvelope<Comment> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateCommentEventEnvelopeAsync(
                    envelope, CommentEventOperation.Approving);

                bool alreadyProcessed = await AlreadyProcessedAsync(
                    envelope: envelope,
                    receiverName: EventBrokerIdentifiers
                        .CommentOnApprovingCommentSubscriptionName,
                    cancellationToken: cancellationToken);

                if (alreadyProcessed)
                    return null;

                Comment approvedComment =
                    await DoTransitionCommentApprovalAsync(
                        comment: envelope.Content,
                        inboundEnvelope: envelope,

                        // Admissible because the envelope was VERIFIED above. Only this system
                        // holds the signing key, so a valid signature is what establishes that
                        // the envelope was minted here rather than by whoever could reach the
                        // address — and the security context is inside the signed payload, so a
                        // caller cannot set the flag on a genuine envelope without breaking the
                        // HMAC, nor mint a fresh one carrying it.
                        //
                        // That is what lets the approval workflow sync its decision onto the
                        // entity over an event at all (§16.7.1). It rests on the signing key
                        // never leaving this system; a future host that publishes with a key of
                        // its own would be asserting this identity too, and would need its own
                        // answer here.
                        isSystemIdentityAdmissible: true,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: approvedComment);
            });

        private async ValueTask<bool> AlreadyProcessedAsync(
            EventEnvelope<Comment> envelope,
            string receiverName,
            CancellationToken cancellationToken) =>
            await this.storageBroker.SelectProcessedEventExistsAsync(
                eventId: envelope.Metadata.EventId,
                receiverName: receiverName,
                cancellationToken: cancellationToken);

        private async ValueTask RecordEventProcessedAsync(
            EventEnvelope<Comment> envelope,
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
