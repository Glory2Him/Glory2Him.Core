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
        // The workflow's own dismissal, and now the ONLY way a review reaches Dismissed
        // (§7.7 rule 7, #295). There is no public verb beside this one and no event address
        // carrying the request: a reviewer submits Approved or Rejected, and dismissal is what
        // happens TO a verdict when the content it judged has changed.
        //
        // The caller does not hand the context in, and could not — it asks for the ACT and this
        // service mints the identity. That is what makes the system-identity flag unforgeable
        // by construction rather than by validation: the flag has exactly one writer in the
        // solution, and no token, claim, role or header can produce it.
        public ValueTask<ApprovalReview> DismissStaleApprovalReviewAsync(
            Guid approvalReviewId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Dismiss owns only StatusId and drives it to a fixed value, so the request
                // carries nothing but the id — the entity exists to anchor the security context
                // and the causation chain, exactly as the read path's does.
                var dismissRequest = new ApprovalReview { Id = approvalReviewId };

                EventEnvelope<ApprovalReview> systemEnvelope =
                    await this.eventEnvelopeBroker.CreateSystemAsync(content: dismissRequest);

                return await DoDismissApprovalReviewAsync(
                    approvalReviewId: approvalReviewId,
                    inboundEnvelope: systemEnvelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<ApprovalReview> DoDismissApprovalReviewAsync(
            Guid approvalReviewId,
            EventEnvelope<ApprovalReview> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
            ValidateOnDismissApprovalReview(approvalReviewId);

            // Dismissal is the workflow's own act and no human's (#196 decision 9, #295). There
            // is no publisher-tier branch beside this one any more: the tiers used to be checked
            // when a person could reach this verb, and now nobody can.
            //
            // Defence in depth rather than a live gate. The one caller mints the context two
            // methods up, so this cannot fail today — it fails the day somebody adds a second
            // caller that does not, which is exactly when it should.
            ValidateDismissalIsTheWorkflowsOwnAct(inboundEnvelope.SecurityContext);

            ApprovalReview storageApprovalReview =
                await LoadDismissTargetAsync(
                    approvalReviewId: approvalReviewId,
                    cancellationToken: cancellationToken);

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

            // NO ProcessedEvents bookkeeping on this path, unlike every other transition (#295).
            //
            // The dual record exists so that a do-work shared between a public verb and an event
            // handler cannot process one delivery twice: the verb pre-records the id against the
            // handler's receiver name, and the handler then skips it. Both rows were keyed on
            // ApprovalReviewOnDismissingApprovalReviewSubscriptionName, and their only reader
            // was that handler's AlreadyProcessedAsync — deleted in this same commit. Nothing
            // reads them, so writing them is work done to be discarded.
            //
            // NOT because a downstream subscriber dedupes instead. ApprovalOrchestrationService
            // takes no IStorageBroker and performs no ProcessedEvents check of any kind, so a
            // redelivered ApprovalReview-Dismissed IS re-processed there. That is tolerable
            // because its handler re-evaluates the round rather than applying a delta — the
            // second pass reaches the same verdict — but it is idempotence by construction, not
            // deduplication, and the distinction matters to anyone reasoning about redelivery.
            EventEnvelope<ApprovalReview> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedApprovalReview);

            await this.eventBroker.PublishApprovalReviewAsync(
                envelope: outboundEnvelope,
                operation: ApprovalReviewEventOperation.Dismissed);

            return updatedApprovalReview;
        }
    }
}
