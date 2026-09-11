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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Exceptions;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Tags;

namespace Glory2Him.Core.Services.Foundations.Tags
{
    /// <summary>
    /// The narrow state-transition operations (design §9.7.1, §9.2).
    ///
    /// <para>The general modify is content-only. Every <c>IApproval</c> field belongs to a
    /// transition here, owning exactly its own fields and publishing its own fact. That
    /// separation is the approval workflow's cycle-breaker: the workflow subscribes to
    /// <c>Modified</c> and causes <c>Approved</c>, so a transition that published
    /// <c>Modified</c> would re-enter the handler that caused it. <c>ProcessedEvents</c> cannot
    /// help — it is keyed on the event id, and a write-back mints a fresh one — and under
    /// inline dispatch the repetition is synchronous re-entry inside the originating
    /// request.</para>
    ///
    /// <para>Every operation here follows the same order, which differs from
    /// <c>DoModifyTagAsync</c> in one important way: the row is loaded FIRST and the caller's
    /// entity is never the thing saved. Authorization is decided against the STORED row,
    /// because the author is an authorization input and a caller-supplied one would be
    /// self-certification. Only the operation's own fields are then copied onto the stored
    /// row.</para>
    /// </summary>
    internal partial class TagService
    {
        public ValueTask<Tag> SubmitTagByIdAsync(
            Guid tagId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Submit owns only ApprovalStatus and drives it to a fixed value, so the
                // request carries nothing but the id — the entity exists to anchor the
                // security context and the causation chain, exactly as the read path's does.
                var submitRequest = new Tag { Id = tagId };

                EventEnvelope<Tag> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: submitRequest);

                return await DoSubmitTagAsync(
                    tagId: tagId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Tag> TransitionTagApprovalAsync(
            Tag tag,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateTagIsNotNull(tag);

                EventEnvelope<Tag> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: tag);

                return await DoTransitionTagApprovalAsync(
                    tag: tag,
                    inboundEnvelope: envelope,

                    // This envelope's context was minted here, in process, from the ambient
                    // caller — so a system identity on it is one this process asserted about
                    // itself, so a system identity on it is one this process asserted about
                    // itself. The event path admits the claim too, because only this
                    // system holds the signing key — so a verified envelope is one this
                    // system minted, whichever path it arrived by (§16.7.1).
                    isSystemIdentityAdmissible: true,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<Tag> DoSubmitTagAsync(
            Guid tagId,
            EventEnvelope<Tag> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
            ValidateOnSubmitTag(tagId);

            Tag storageTag =
                await LoadTransitionTargetAsync(
                    tagId: tagId,
                    cancellationToken: cancellationToken);

            // decided against the STORED row: submitting is the owner-or-publisher act of §9.2,
            // and the author it is measured against must be the one on record rather than one
            // the caller supplied
            await ValidateUserCanSubmitStorageTagAsync(
                storageTag: storageTag,
                securityContext: inboundEnvelope.SecurityContext);

            ValidateStorageTagIsSubmittable(storageTag);

            // the whole of Submit's remit is this one field. The target is fixed — Submit only
            // ever means Draft → Submitted — so unlike approve there is nothing to read off the
            // caller's copy, and nothing it could set that this would trust.
            storageTag.ApprovalStatus = ApprovalStatus.Submitted;

            return await SaveTransitionAsync(
                tag: storageTag,
                inboundEnvelope: inboundEnvelope,
                operation: TagEventOperation.Submitted,
                receiverName: EventBrokerIdentifiers
                    .TagOnSubmittingTagSubscriptionName,
                cancellationToken: cancellationToken);
        }

        private async ValueTask<Tag> DoTransitionTagApprovalAsync(
            Tag tag,
            EventEnvelope<Tag> inboundEnvelope,
            bool isSystemIdentityAdmissible,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

            // Shape first, and the bypass reason with it, so an unexplained bypass is refused
            // before any policy is read — under every policy, including one that would have
            // permitted the waiver.
            ValidateOnTransitionTagApproval(tag);

            // The system identity is a claim about PROVENANCE, and the SIGNATURE carries it.
            // The flag sits inside the signed payload and only this system holds the key, so it
            // cannot be added to a genuine envelope without breaking the HMAC, nor asserted on
            // a forged one — a verified envelope is one this system minted, whichever path it
            // arrived by (§16.7.1). That is what lets the approval workflow sync its decision
            // onto the entity over an event at all — a call-site rule could not.
            //
            // Provenance is still an ARGUMENT each entry point supplies rather than a property
            // read off the data, so an entry point carrying no workflow command has a place to
            // refuse the claim (§9.7.1 rule 3). Every entry point verifies its envelope and
            // then passes true, so this conjunction narrows nothing today: it is the seam, not
            // the guard. The guard is the signature check the receiver already ran.
            bool isSystemIdentity =
                isSystemIdentityAdmissible
                    && inboundEnvelope.SecurityContext.IsSystemIdentity;

            Tag storageTag =
                await LoadTransitionTargetAsync(
                    tagId: tag.Id,
                    cancellationToken: cancellationToken);

            // decided against the STORED row. Transitioning from the caller's copy would let a
            // contributor name someone else as author and approve their own row — and would let
            // anyone present a terminal row as Submitted to slip past the override gate.
            bool isBypassUsed = await ValidateUserCanTransitionStorageTagApprovalAsync(
                storageTag: storageTag,
                tag: tag,
                securityContext: inboundEnvelope.SecurityContext,
                isSystemIdentity: isSystemIdentity,
                cancellationToken: cancellationToken);

            ValidateStorageTagIsTransitionable(storageTag);

            // the whole of IApproval, as one unit — approve and publish are one operation, so
            // there is no separate publish verb and PublishDate belongs here and nowhere else
            storageTag.ApprovalStatus = tag.ApprovalStatus;

            // Publication is DERIVED, not copied. Any target but Approved unpublishes the row,
            // so an override out of Approved cannot leave a re-opened item publicly visible
            // while it waits for a second verdict. The validation above already refuses the
            // inverse pairing, which makes this a backstop rather than the only guard — but it
            // is what makes the rule true by construction instead of true by validator.
            //
            // Nothing republishes whatever this may have demoted: the group simply has no
            // public row until something is approved again (epic decision 7).
            bool isApproved = tag.ApprovalStatus == ApprovalStatus.Approved;
            storageTag.IsPublished = isApproved && tag.IsPublished;
            storageTag.PublishDate = storageTag.IsPublished ? tag.PublishDate : null;

            // The bypass pair, DERIVED from the decision rather than accepted from the caller.
            // Copying these the way ApprovalStatus is copied would let a caller performing a
            // genuine bypass send IsApprovedByBypass = false and erase the record.
            //
            // The reason's VALUE is necessarily the caller's own words — no decision can say why
            // a human chose to override — but its RETENTION is the decision's call. A bypass
            // that turned out to be unnecessary records no bypass at all, and an item
            // bypass-approved, later amended and then approved normally stops claiming it was
            // bypassed (§9.7.1 rule 3, §9.7.5).
            storageTag.IsApprovedByBypass = isBypassUsed;

            storageTag.ApprovedByBypassReason = isBypassUsed
                ? tag.ApprovedByBypassReason
                : null;

            // The fact follows the DECISION, not the operation's name. A rejection broadcast on
            // the Approved address would tell every subscriber the opposite of what happened,
            // and the fact name is the contract they key on. An override back to Submitted
            // re-opens the round, which is exactly what the Submitted address already means.
            TagEventOperation decision = tag.ApprovalStatus switch
            {
                ApprovalStatus.Approved => TagEventOperation.Approved,
                ApprovalStatus.Rejected => TagEventOperation.Rejected,
                _ => TagEventOperation.Submitted
            };

            return await SaveTransitionAsync(
                tag: storageTag,
                inboundEnvelope: inboundEnvelope,
                operation: decision,
                receiverName: EventBrokerIdentifiers
                    .TagOnApprovingTagSubscriptionName,
                cancellationToken: cancellationToken);
        }

        // Loads the row a transition acts on. Every transition authorizes against what is
        // STORED, so the load has to happen before the authorization decision rather than
        // after it, and the NotFound guard belongs with the load.
        private async ValueTask<Tag> LoadTransitionTargetAsync(
            Guid tagId,
            CancellationToken cancellationToken)
        {
            Tag maybeTag =
                await this.storageBroker.SelectTagByIdAsync(
                    tagId: tagId,
                    cancellationToken: cancellationToken);

            ValidateStorageTag(maybeTag, tagId);

            // A soft-removed row is a takedown. Transitioning one would submit, approve or
            // publish something already withdrawn, and would broadcast a fact about it —
            // approving a tombstone would set IsPublished on a row the reads deliberately hide.
            // Reported as not-found, matching the read posture, so a removed id is not
            // distinguishable from one that never existed.
            ValidateStorageTagIsNotDeleted(maybeTag, tagId);

            return maybeTag;
        }

        // The tail every transition shares: stamp the audit values, save, record the inbound
        // delivery, publish the operation's OWN fact, record the outbound one. Shared so that
        // no transition can quietly publish Modified — there is exactly one publish call for
        // all transitions and the operation is a parameter.
        private async ValueTask<Tag> SaveTransitionAsync(
            Tag tag,
            EventEnvelope<Tag> inboundEnvelope,
            TagEventOperation operation,
            string receiverName,
            CancellationToken cancellationToken)
        {
            tag = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(
                    entity: tag,
                    securityContext: inboundEnvelope.SecurityContext);

            Tag updatedTag =
                await this.storageBroker.UpdateTagAsync(
                    tag,
                    cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: receiverName,
                cancellationToken: cancellationToken);

            EventEnvelope<Tag> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedTag);

            EventPublishResult<Tag> publishResult =
                await this.eventBroker.PublishTagAsync(
                    envelope: outboundEnvelope,
                    operation: operation);

            // §EVN23. Delivery is contained, so a subscriber that failed says so HERE and
            // nowhere else, and nothing redelivers it. Tag-Submitted reaches the approval
            // round; dropping it diverges the round from the row permanently, because the
            // read-triggered repair only opens a MISSING round (§16.7.2). Unconditional
            // because an unsubscribed address reports no deliveries at all — the subscription
            // list answers this, and a copy of it does not belong in a service.
            //
            // Logged, never thrown: the row is already committed above, and failing the caller
            // now would report a completed write as a failed one. What guarantees that is the
            // CONTAINMENT below, not where this sits — an earlier version of this comment
            // claimed the position did it.
            //
            // BEFORE the outbound dedup write, and that ordering is the rule's own requirement.
            // That write can fail, and a report placed after it would be skipped by the very
            // failure it has to survive, leaving a dropped required delivery unreported while an
            // unrelated bookkeeping fault took the blame. The two are independent: a dedup row
            // that would not save says nothing about whether the fact arrived, and the operator
            // needs both. It sat after this write until the report was contained, so that a
            // faulting sink could not cost the event its dedup row; containment answers that now.
            if (publishResult.HasFailedDeliveries)
            {
                // CONTAINED, because the report is bookkeeping on somebody else's path and does
                // not get to decide that path's outcome — the same shape, and the same argument,
                // as ResetStaleAIReviewerAssignmentAsync and Substrate's onVerified hook.
                // LogCriticalAsync has no try/catch of its own, so a faulting sink (back-
                // pressure, a disposed provider at shutdown) would otherwise propagate: it would
                // report a COMMITTED write as a failed one, which is exactly what rule 2 forbids
                // and what being last in this method does NOT prevent. Being last only stops the
                // throw skipping work that follows it HERE — and where this report sits in a
                // shared helper, work still follows it in the CALLER.
                //
                // Swallowed rather than logged, and that is the one place this differs from
                // those two: the sink that would have to carry a second message is the one that
                // just threw. The delivery failure is already recorded on the event store's own
                // row, which the event id and subscription id locate.
                //
                // EVERY exception, cancellation included, and that is deliberate.
                // ILoggingBroker.LogCriticalAsync(Exception) takes no CancellationToken, so the
                // caller's token cannot reach it: an OperationCanceledException raised in there
                // is never the caller cancelling, it is the SINK failing — a disposed provider
                // at shutdown, or a batching provider's own flush deadline — which is the case
                // this block exists for. An earlier version carved it out by exception filter,
                // copied from ResetStaleAIReviewerAssignmentAsync and Substrate's onVerified
                // hook; both of those guard calls that DO take a token, so the carve-out means
                // something there and nothing here.
                //
                // What the carve-out actually did: the escape landed in this service's TryCatch,
                // whose cancellation arm matches an OperationCanceledException whose token is
                // NOT cancelled — the exact shape of a sink-originated one — and turned a
                // committed write into a timeout reported to the caller. It also skipped the
                // outbound dedup write below, letting a redelivery re-apply the transition.
                try
                {
                    await this.loggingBroker.LogCriticalAsync(
                        FailedEventDeliveryException.ForFailedDeliveries(publishResult, operation));
                }
                catch (Exception)
                {
                }
            }

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: receiverName,
                cancellationToken: cancellationToken);

            return updatedTag;
        }
    }
}
