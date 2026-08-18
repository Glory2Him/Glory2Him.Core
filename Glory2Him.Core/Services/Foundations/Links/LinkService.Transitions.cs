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
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Links;

namespace Glory2Him.Core.Services.Foundations.Links
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
    /// <c>DoModifyLinkAsync</c> in one important way: the row is loaded FIRST and the caller's
    /// entity is never the thing saved. Authorization is decided against the STORED row,
    /// because the author is an authorization input and a caller-supplied one would be
    /// self-certification. Only the operation's own fields are then copied onto the stored
    /// row.</para>
    /// </summary>
    internal partial class LinkService
    {
        public ValueTask<Link> SubmitLinkByIdAsync(
            Guid linkId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Submit owns only ApprovalStatus and drives it to a fixed value, so the
                // request carries nothing but the id — the entity exists to anchor the
                // security context and the causation chain, exactly as the read path's does.
                var submitRequest = new Link { Id = linkId };

                EventEnvelope<Link> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: submitRequest);

                return await DoSubmitLinkAsync(
                    linkId: linkId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Link> TransitionLinkApprovalAsync(
            Link link,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateLinkIsNotNull(link);

                EventEnvelope<Link> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: link);

                return await DoTransitionLinkApprovalAsync(
                    link: link,
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

        private async ValueTask<Link> DoSubmitLinkAsync(
            Guid linkId,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
            ValidateOnSubmitLink(linkId);

            Link storageLink =
                await LoadTransitionTargetAsync(
                    linkId: linkId,
                    cancellationToken: cancellationToken);

            // decided against the STORED row: submitting is the owner-or-publisher act of §9.2,
            // and the author it is measured against must be the one on record rather than one
            // the caller supplied
            await ValidateUserCanSubmitStorageLinkAsync(
                storageLink: storageLink,
                securityContext: inboundEnvelope.SecurityContext);

            ValidateStorageLinkIsSubmittable(storageLink);

            // the whole of Submit's remit is this one field. The target is fixed — Submit only
            // ever means Draft → Submitted — so unlike approve there is nothing to read off the
            // caller's copy, and nothing it could set that this would trust.
            storageLink.ApprovalStatus = ApprovalStatus.Submitted;

            return await SaveTransitionAsync(
                link: storageLink,
                inboundEnvelope: inboundEnvelope,
                operation: LinkEventOperation.Submitted,
                receiverName: EventBrokerIdentifiers
                    .LinkOnSubmittingLinkSubscriptionName,
                cancellationToken: cancellationToken);
        }

        private async ValueTask<Link> DoTransitionLinkApprovalAsync(
            Link link,
            EventEnvelope<Link> inboundEnvelope,
            bool isSystemIdentityAdmissible,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

            // Shape first, and the bypass reason with it, so an unexplained bypass is refused
            // before any policy is read — under every policy, including one that would have
            // permitted the waiver.
            ValidateOnTransitionLinkApproval(link);

            // The system identity is a claim about PROVENANCE, and provenance is not carried by
            // the payload. It is honoured only where this service minted the context itself; an
            // envelope that arrived over a public event address carries a deserialized,
            // unverified context (§14.6 rule 4), and a caller able to assert the flag there
            // would walk past every rule below by declaring themselves the workflow.
            bool isSystemIdentity =
                isSystemIdentityAdmissible
                    && inboundEnvelope.SecurityContext.IsSystemIdentity;

            Link storageLink =
                await LoadTransitionTargetAsync(
                    linkId: link.Id,
                    cancellationToken: cancellationToken);

            // decided against the STORED row. Transitioning from the caller's copy would let a
            // contributor name someone else as author and approve their own row — and would let
            // anyone present a terminal row as Submitted to slip past the override gate.
            bool isBypassUsed = await ValidateUserCanTransitionStorageLinkApprovalAsync(
                storageLink: storageLink,
                link: link,
                securityContext: inboundEnvelope.SecurityContext,
                isSystemIdentity: isSystemIdentity,
                cancellationToken: cancellationToken);

            ValidateStorageLinkIsTransitionable(storageLink);

            // the whole of IApproval, as one unit — approve and publish are one operation, so
            // there is no separate publish verb and PublishDate belongs here and nowhere else
            storageLink.ApprovalStatus = link.ApprovalStatus;

            // Publication is DERIVED, not copied. Any target but Approved unpublishes the row,
            // so an override out of Approved cannot leave a re-opened item publicly visible
            // while it waits for a second verdict. The validation above already refuses the
            // inverse pairing, which makes this a backstop rather than the only guard — but it
            // is what makes the rule true by construction instead of true by validator.
            //
            // Nothing republishes whatever this may have demoted: the group simply has no
            // public row until something is approved again (epic decision 7).
            bool isApproved = link.ApprovalStatus == ApprovalStatus.Approved;
            storageLink.IsPublished = isApproved && link.IsPublished;
            storageLink.PublishDate = storageLink.IsPublished ? link.PublishDate : null;

            // The bypass pair, DERIVED from the decision rather than accepted from the caller.
            // Copying these the way ApprovalStatus is copied would let a caller performing a
            // genuine bypass send IsApprovedByBypass = false and erase the record.
            //
            // The reason's VALUE is necessarily the caller's own words — no decision can say why
            // a human chose to override — but its RETENTION is the decision's call. A bypass
            // that turned out to be unnecessary records no bypass at all, and an item
            // bypass-approved, later amended and then approved normally stops claiming it was
            // bypassed (§9.7.1 rule 3, §9.7.5).
            storageLink.IsApprovedByBypass = isBypassUsed;

            storageLink.ApprovedByBypassReason = isBypassUsed
                ? link.ApprovedByBypassReason
                : null;

            // The fact follows the DECISION, not the operation's name. A rejection broadcast on
            // the Approved address would tell every subscriber the opposite of what happened,
            // and the fact name is the contract they key on. An override back to Submitted
            // re-opens the round, which is exactly what the Submitted address already means.
            LinkEventOperation decision = link.ApprovalStatus switch
            {
                ApprovalStatus.Approved => LinkEventOperation.Approved,
                ApprovalStatus.Rejected => LinkEventOperation.Rejected,
                _ => LinkEventOperation.Submitted
            };

            return await SaveTransitionAsync(
                link: storageLink,
                inboundEnvelope: inboundEnvelope,
                operation: decision,
                receiverName: EventBrokerIdentifiers
                    .LinkOnApprovingLinkSubscriptionName,
                cancellationToken: cancellationToken);
        }

        public ValueTask<Link> UnpublishLinkByIdAsync(
            Guid linkId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Owns two fields and drives both to fixed values, so the request
                // carries nothing but the id — the same shape demote has, for the
                // same reason: there is nothing to read off a caller's copy.
                var unpublishRequest = new Link { Id = linkId };

                EventEnvelope<Link> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: unpublishRequest);

                return await DoUnpublishLinkAsync(
                    linkId: linkId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Link> UnpublishLinkByIdAsync(
            Guid linkId,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Chained off the swap's envelope, so IsSystemIdentity travels with it
                // and causation stays linked. CreateNextAsync copies the security
                // context forward; it does not mint one.
                EventEnvelope<Link> unpublishEnvelope =
                    await this.eventEnvelopeBroker.CreateNextAsync(
                        sourceEnvelope: inboundEnvelope,
                        content: new Link { Id = linkId });

                return await DoUnpublishLinkAsync(
                    linkId: linkId,
                    inboundEnvelope: unpublishEnvelope,
                    cancellationToken: cancellationToken);
            });

        // The publication swap's first write (§9.7.7 rule 7). It exists as its own
        // verb because the general modify refuses IApproval members and the approve
        // transition only ever writes publication as a CONSEQUENCE of a decision —
        // and no decision is being made here. The incumbent is not being un-approved,
        // it is being superseded.
        private async ValueTask<Link> DoUnpublishLinkAsync(
            Guid linkId,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnUnpublishLink(linkId);
            ValidateUserCanUnpublishLink(inboundEnvelope.SecurityContext);

            // NOT LoadTransitionTargetAsync: that refuses a soft-deleted row, and a
            // soft delete never clears IsPublished. The index filter names that column
            // alone, so a tombstone still holds the slot, and refusing to clear it
            // would leave the group permanently unpublishable (§9.7.7 rule 7).
            Link maybeLink =
                await this.storageBroker.SelectLinkByIdAsync(
                    linkId: linkId,
                    cancellationToken: cancellationToken);

            ValidateStorageLink(maybeLink, linkId);

            // Idempotent. The swap probes for an incumbent and may race another that
            // already cleared it; refusing here would fail an approval for work that
            // is already done.
            if (maybeLink.IsPublished is false)
            {
                return maybeLink;
            }

            maybeLink.IsPublished = false;

            // A publish date without publication is a date nothing reads, which is
            // what IsPublishDateWithoutPublication already refuses on the way in.
            maybeLink.PublishDate = null;

            return await SaveTransitionAsync(
                link: maybeLink,
                inboundEnvelope: inboundEnvelope,
                operation: LinkEventOperation.Unpublished,
                receiverName: EventBrokerIdentifiers
                    .LinkOnLinkUnpublishedSubscriptionName,
                cancellationToken: cancellationToken);
        }

        // Loads the row a transition acts on. Every transition authorizes against what is
        // STORED, so the load has to happen before the authorization decision rather than
        // after it, and the NotFound guard belongs with the load.
        private async ValueTask<Link> LoadTransitionTargetAsync(
            Guid linkId,
            CancellationToken cancellationToken)
        {
            Link maybeLink =
                await this.storageBroker.SelectLinkByIdAsync(
                    linkId: linkId,
                    cancellationToken: cancellationToken);

            ValidateStorageLink(maybeLink, linkId);

            // A soft-removed row is a takedown. Transitioning one would submit, approve or
            // publish something already withdrawn, and would broadcast a fact about it —
            // approving a tombstone would set IsPublished on a row the reads deliberately hide.
            // Reported as not-found, matching the read posture, so a removed id is not
            // distinguishable from one that never existed.
            ValidateStorageLinkIsNotDeleted(maybeLink, linkId);

            return maybeLink;
        }

        // The tail every transition shares: stamp the audit values, save, record the inbound
        // delivery, publish the operation's OWN fact, record the outbound one. Shared so that
        // no transition can quietly publish Modified — there is exactly one publish call for
        // all transitions and the operation is a parameter.
        private async ValueTask<Link> SaveTransitionAsync(
            Link link,
            EventEnvelope<Link> inboundEnvelope,
            LinkEventOperation operation,
            string receiverName,
            CancellationToken cancellationToken)
        {
            link = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(
                    entity: link,
                    securityContext: inboundEnvelope.SecurityContext);

            Link updatedLink =
                await this.storageBroker.UpdateLinkAsync(
                    link,
                    cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: receiverName,
                cancellationToken: cancellationToken);

            EventEnvelope<Link> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedLink);

            await this.eventBroker.PublishLinkAsync(
                envelope: outboundEnvelope,
                operation: operation);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: receiverName,
                cancellationToken: cancellationToken);

            return updatedLink;
        }
    }
}
