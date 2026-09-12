// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Orchestrations.ApprovalReviewers
{
    /// <summary>
    /// The two §7.9 retirements, as subscriptions (§12.5.4 business rule 4). Neither is anybody's
    /// act: both run under the SYSTEM identity the foundation's workflow seam mints for itself,
    /// so <c>DeletedBy</c> names the system rather than whoever's vote happened to trigger the
    /// delivery.
    ///
    /// <para><b>Nothing here is asked for anything and nothing here publishes.</b> A fact handler
    /// records no inbound <c>ProcessedEvent</c> — the dedup pair is a foundation request-handler
    /// device, both halves are storage-broker calls an orchestration may not hold (§12.5), and
    /// there is no transaction here to commit them in because this service writes no row of its
    /// own. What makes REDELIVERY safe instead is the signed status gate below plus the
    /// idempotent gather: a second delivery on a round whose requests are gone finds nothing to
    /// do (<c>Documentation/Design/Events.md</c> §EVN19 rule 1).</para>
    ///
    /// <para><b>A failure is NOT swallowed here</b>, which is the one posture that changed with
    /// the move. It propagates into this service's <c>TryCatch</c> and out of the handler, where
    /// the substrate records the delivery as failed on the publish result rather than throwing it
    /// at whoever pressed Approve (§EVN23). The hand-written <c>try</c>/<c>catch</c>/log that
    /// used to hold that guarantee went with the call sites it protected.</para>
    /// </summary>
    internal partial class ApprovalReviewerOrchestrationService
    {
        // §7.9 rule 6. VERIFIED FIRST, and that ordering is the security property rather than a
        // style: retiring on the strength of an envelope whose signature has not been checked
        // would let anyone reaching the address clear the panel.
        //
        // The token is checked ahead of the verification for the reason the round's handlers do
        // it: a caller who has already gone should not pay for an HMAC verification, and without
        // it a refused branch would check the token nowhere.
        public ValueTask<EventEnvelope<ApprovalReview>?> OnApprovalReviewAddedAsync(
            EventEnvelope<ApprovalReview> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatch<EventEnvelope<ApprovalReview>?>(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateEntityFactEnvelopeAsync(envelope, "ApprovalReviewAdded");

                await RetireAnsweredReviewRequestAsync(
                    approvalId: envelope.Content.ApprovalId,
                    reviewerUserId: envelope.Content.CreatedBy,
                    cancellationToken: cancellationToken);

                return null;
            });

        /// <summary>
        /// Rule 6 — the invited person answered, so their invitation retires itself. Runs under
        /// the SYSTEM identity through the foundation's workflow seam, because the retirement is
        /// nobody's act: DeletedBy must say "answered", not name whoever happened to trigger it.
        ///
        /// <para>Silent when there is no invitation, which is the common case: most reviews are
        /// recorded by people who were never formally asked.</para>
        /// </summary>
        private async ValueTask RetireAnsweredReviewRequestAsync(
            Guid approvalId,
            string reviewerUserId,
            CancellationToken cancellationToken)
        {
            if (approvalId == Guid.Empty || string.IsNullOrWhiteSpace(reviewerUserId))
            {
                return;
            }

            ApprovalReviewerScope maybeScope =
                await this.accessBroker.RetrieveApprovalReviewerScopeByIdAsync(
                    approvalId,
                    cancellationToken);

            ActiveReviewRequest answeredRequest = maybeScope?.ActiveRequests
                .FirstOrDefault(request => request.RequestedUserId == reviewerUserId);

            if (answeredRequest is null)
            {
                return;
            }

            await this.approvalReviewRequestWorkflowService
                .RetireAnsweredApprovalReviewRequestAsync(
                    approvalReviewRequestId: answeredRequest.Id,
                    cancellationToken: cancellationToken);
        }
    }
}
