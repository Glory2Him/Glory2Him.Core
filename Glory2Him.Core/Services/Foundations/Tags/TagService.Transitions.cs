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
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
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

        public ValueTask<Tag> ApproveTagAsync(
            Tag tag,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateTagIsNotNull(tag);

                EventEnvelope<Tag> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: tag);

                return await DoApproveTagAsync(
                    tag: tag,
                    inboundEnvelope: envelope,
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

        private async ValueTask<Tag> DoApproveTagAsync(
            Tag tag,
            EventEnvelope<Tag> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
            ValidateOnApproveTag(tag);

            Tag storageTag =
                await LoadTransitionTargetAsync(
                    tagId: tag.Id,
                    cancellationToken: cancellationToken);

            // decided against the STORED row. Approving from the caller's copy would let a
            // contributor name someone else as author and approve their own row.
            AccessVerdict accessVerdict = await ValidateUserCanApproveStorageTagAsync(
                storageTag: storageTag,
                tag: tag,
                securityContext: inboundEnvelope.SecurityContext,
                cancellationToken: cancellationToken);

            ValidateStorageTagIsApprovable(storageTag);

            // the whole of IApproval, as one unit — approve and publish are one operation, so
            // there is no separate publish verb and PublishDate belongs here and nowhere else
            storageTag.ApprovalStatus = tag.ApprovalStatus;
            storageTag.IsPublished = tag.IsPublished;
            storageTag.PublishDate = tag.PublishDate;

            // The two exceptions, DERIVED from the decision rather than accepted from the
            // caller. Copying these the way the three above are copied would let a caller
            // performing a genuine bypass send IsApprovedByBypass = false and erase the record.
            //
            // This operation never requests a bypass, so the decision can only come back false;
            // a dedicated bypass verb is what would ever write true. Clearing the reason is
            // deliberate rather than incidental: an item bypass-approved, later amended and then
            // approved normally must stop claiming it was bypassed.
            storageTag.IsApprovedByBypass = accessVerdict.IsBypassUsed;
            storageTag.ApprovedByBypassReason = null;

            // The fact follows the DECISION, not the operation's name. A rejection broadcast on
            // the Approved address would tell every subscriber the opposite of what happened,
            // and the fact name is the contract they key on.
            TagEventOperation decision =
                storageTag.ApprovalStatus == ApprovalStatus.Approved
                    ? TagEventOperation.Approved
                    : TagEventOperation.Rejected;

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

            await this.eventBroker.PublishTagAsync(
                envelope: outboundEnvelope,
                operation: operation);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: receiverName,
                cancellationToken: cancellationToken);

            return updatedTag;
        }
    }
}
