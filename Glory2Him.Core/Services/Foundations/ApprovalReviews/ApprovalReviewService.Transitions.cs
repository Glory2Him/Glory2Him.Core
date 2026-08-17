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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviews
{
    /// <summary>
    /// The narrow state-transition operation (design §9.7.1, §8.8).
    ///
    /// <para>Add and modify are the reviewer's verdict path — they set <c>StatusId</c> to
    /// <c>Approved</c> or <c>Rejected</c> and refuse anything else (#134). <c>Dismissed</c> is
    /// not a verdict a reviewer declares; it is what happens TO a review when an entity-scoped
    /// change invalidates it (§9.5), so it gets its own operation, owning exactly
    /// <c>StatusId</c> and publishing its own fact. That separation is also the workflow's
    /// cycle-breaker: §8.8's dismissal is caused by a content change and must not itself look
    /// like the reviewer amending their verdict, so it never publishes <c>Modified</c>.</para>
    ///
    /// <para>Like every transition it loads the row FIRST and authorizes against what is
    /// STORED; the request carries only the id.</para>
    /// </summary>
    internal partial class ApprovalReviewService
    {
        public ValueTask<ApprovalReview> DismissApprovalReviewAsync(
            Guid approvalReviewId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Dismiss owns only StatusId and drives it to a fixed value, so the request
                // carries nothing but the id — the entity exists to anchor the security context
                // and the causation chain, exactly as the read path's does.
                var dismissRequest = new ApprovalReview { Id = approvalReviewId };

                EventEnvelope<ApprovalReview> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: dismissRequest);

                return await DoDismissApprovalReviewAsync(
                    approvalReviewId: approvalReviewId,
                    inboundEnvelope: envelope,

                    // This envelope's context was minted here, in process, from the ambient
                    // caller — so a system identity on it is one this process asserted about
                    // itself. The event path passes false; see OnDismissingApprovalReviewAsync.
                    isSystemIdentityAdmissible: true,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<ApprovalReview> DoDismissApprovalReviewAsync(
            Guid approvalReviewId,
            EventEnvelope<ApprovalReview> inboundEnvelope,
            bool isSystemIdentityAdmissible,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
            ValidateOnDismissApprovalReview(approvalReviewId);

            // The system identity is a claim about PROVENANCE, and provenance is not carried by
            // the payload. It is honoured only where this service minted the context itself; an
            // envelope that arrived over a public event address carries a deserialized,
            // unverified context (§14.6 rule 4), and a caller able to assert the flag there
            // would dismiss any review in the system by declaring themselves the workflow.
            bool isSystemIdentity =
                isSystemIdentityAdmissible
                    && inboundEnvelope.SecurityContext.IsSystemIdentity;

            ApprovalReview storageApprovalReview =
                await LoadDismissTargetAsync(
                    approvalReviewId: approvalReviewId,
                    cancellationToken: cancellationToken);

            // Dismissing stale reviews after the OWNER's edit is a write the workflow must make
            // and no human is permitted to: the owner holds no publisher tier, and the reviewers
            // whose reviews are being withdrawn are the last parties who should withdraw them.
            // The system identity is admitted in place of the publisher tier for exactly that,
            // and skips both tiers together — the second is the same question as the first,
            // narrowed to the entity under review.
            if (isSystemIdentity is false)
            {
                // the publisher tier, not the review role: dismissal is the workflow's act, and a
                // Reviewer moving a peer's (or their own) review to Dismissed by hand is exactly
                // what §8.8 reserves to the entity-change machinery
                ValidateUserCanDismissApprovalReview(inboundEnvelope.SecurityContext);

                // and that tier narrowed to the entity actually under review, which the row-local
                // check above cannot see — a Tag-Publisher clears it for any approval at all
                await ValidateUserMayDismissApprovalReviewAsync(
                    approvalId: storageApprovalReview.ApprovalId,
                    securityContext: inboundEnvelope.SecurityContext,
                    cancellationToken: cancellationToken);
            }

            // a dismissed review stays dismissed — refuse a second dismissal rather than
            // re-publishing the fact
            ValidateStorageApprovalReviewIsDismissable(storageApprovalReview);

            // the whole of the operation's remit is this one field. The target is fixed —
            // dismiss only ever means "-> Dismissed" — so there is nothing to read off a
            // caller's copy, and nothing it could set that this would trust.
            storageApprovalReview.StatusId = ApprovalStatus.Dismissed;

            return await SaveDismissTransitionAsync(
                approvalReview: storageApprovalReview,
                inboundEnvelope: inboundEnvelope,
                cancellationToken: cancellationToken);
        }

        // Loads the row the dismissal acts on. Authorization and the dismissable check are
        // decided against what is STORED, so the load happens first, and the NotFound guard
        // belongs with it.
        private async ValueTask<ApprovalReview> LoadDismissTargetAsync(
            Guid approvalReviewId,
            CancellationToken cancellationToken)
        {
            ApprovalReview maybeApprovalReview =
                await this.storageBroker.SelectApprovalReviewByIdAsync(
                    approvalReviewId: approvalReviewId,
                    cancellationToken: cancellationToken);

            ValidateStorageApprovalReview(maybeApprovalReview, approvalReviewId);

            // A soft-removed review is already out of the threshold. Dismissing one would
            // broadcast a Dismissed fact about a withdrawn row; reported as not-found, matching
            // the read posture, so a removed id is not distinguishable from one that never
            // existed.
            ValidateStorageApprovalReviewIsNotDeleted(maybeApprovalReview, approvalReviewId);

            return maybeApprovalReview;
        }

        // The transition tail: stamp the audit values, save, record the inbound delivery,
        // publish the operation's OWN fact (Dismissed, never Modified), record the outbound one.
        private async ValueTask<ApprovalReview> SaveDismissTransitionAsync(
            ApprovalReview approvalReview,
            EventEnvelope<ApprovalReview> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            approvalReview = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(
                    entity: approvalReview,
                    securityContext: inboundEnvelope.SecurityContext);

            ApprovalReview updatedApprovalReview =
                await this.storageBroker.UpdateApprovalReviewAsync(
                    approvalReview,
                    cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ApprovalReviewOnDismissingApprovalReviewSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalReview> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedApprovalReview);

            await this.eventBroker.PublishApprovalReviewAsync(
                envelope: outboundEnvelope,
                operation: ApprovalReviewEventOperation.Dismissed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ApprovalReviewOnDismissingApprovalReviewSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedApprovalReview;
        }
    }
}
