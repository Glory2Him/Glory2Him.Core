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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
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

        // §7.9 rule 8, and the ONE trigger it has. A round closes three ways — the manual
        // decision (§16.7.1), the §9.7.5 rejection branch and the §9.7.7 automatic approval — and
        // all three write the outcome through ModifyApprovalAsync, so one subscription hears all
        // three and no enumeration of the sites has to be kept in step with anything. Nor would
        // an enumeration be complete: the public modify and the live Approval-Modifying command
        // address reach the same write and publish the same fact.
        //
        // THE GATE IS HERE, BEFORE THE GATHER, and it reads the envelope (§12.5.4 business rule
        // 4(i)). Only three of the workflow seam's six call sites close a round; the rest publish
        // -Modified on an OPEN one — the entity-status sync, the reinstatement of a soft-deleted
        // approval, and §8.6 HR-4's reset to Submitted. Ungated, the handler would retire every
        // pending invitation on an open round, and HR-4 is the sharpest case: reset, the
        // moderator re-invites, and the next -Modified sweeps the invitations they had just
        // re-issued — the opposite of what rule 8's "a moderator asks again" promises.
        //
        // IAccessBroker.FindRetirableApprovalReviewRequestIdsAsync does NOT answer this half: it
        // filters ApprovalId and IsDeleted and never reads the status.
        //
        // What it reads is SIGNED SYSTEM DATA rather than a caller's claim. The value is the
        // updated row the foundation published through CreateNextAsync(sourceEnvelope,
        // updatedApproval) and it sits inside the HMAC, which is §16.7.1's provenance rule rather
        // than an exception to it — and it is why the verification above has to come first. No
        // second read is needed and none is added: a -Modified that closed nothing costs one
        // comparison and no broker call at all, where the three direct call sites it replaces
        // always reached the broker first.
        public ValueTask<EventEnvelope<Approval>?> OnApprovalModifiedAsync(
            EventEnvelope<Approval> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatch<EventEnvelope<Approval>?>(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateEntityFactEnvelopeAsync(envelope, "ApprovalModified");

                bool isRoundClosed = envelope.Content.ApprovalStatus
                    is ApprovalStatus.Approved or ApprovalStatus.Rejected;

                if (isRoundClosed is false)
                {
                    return null;
                }

                await RetireUnansweredApprovalReviewRequestsAsync(
                    approvalId: envelope.Content.Id,
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

        // The gather and the loop, with the status gate lifted into the handler above where the
        // envelope is.
        //
        // The READ is the gathering seam rather than the caller-facing one, and unlike most
        // duplicated postures this is not theoretical: only one of the three routes to a close
        // runs under a moderator. The automatic approval runs under whoever's edit or review
        // tipped the round — ordinarily the AUTHOR revising their own submission, who holds no
        // review role — and the caller-facing request read applies §14.7 posture D, so it would
        // answer that caller with nothing and the panel would keep its stale rows with no error
        // anywhere.
        //
        // It also EXCLUDES anyone holding a review that still stands on the round (§12.5.4
        // business rule 4(ii)), which is what makes this subscription and rule 6's independent of
        // each other. Two subscriptions have no stated order, and the losing order is the common
        // case: the last vote closes the round, so without the exclusion this sweep would take
        // the answerer's own invitation and stamp it "the approval round closed before this
        // review was cast" — false about the one person who did cast one.
        //
        // The WRITE is the workflow seam because the close is nobody's act. Attributing it to
        // whoever cast the deciding vote would have DeletedBy name a person who withdrew nothing,
        // and the public withdraw verb would refuse the system identity anyway — its gate asks
        // for a review tier the system identity does not hold.
        //
        // NO try/catch/log, and its absence is the change rather than an omission. The hand-
        // written one that used to sit here protected three direct call sites on a caller's
        // thread; on a delivery a failure is recorded on EventPublishResult.FailedDeliveries and
        // the publisher decides (§EVN23), so the guarantee that this bookkeeping never faults the
        // close is structural instead of depending on every future caller remembering the catch.
        // Swallowing it here as well would take the failure off the delivery report too, which is
        // the only place it is now visible.
        //
        // ONE loop, no batching and no atomicity. The rows are independent and nothing here is
        // transactional; a row a concurrent withdraw took in the meantime does not throw at all —
        // the verb returns it unchanged.
        private async ValueTask RetireUnansweredApprovalReviewRequestsAsync(
            Guid approvalId,
            CancellationToken cancellationToken)
        {
            List<Guid> unansweredRequestIds =
                await this.accessBroker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId: approvalId,
                    cancellationToken: cancellationToken);

            foreach (Guid unansweredRequestId in unansweredRequestIds)
            {
                await this.approvalReviewRequestWorkflowService
                    .RetireClosedRoundApprovalReviewRequestAsync(
                        approvalReviewRequestId: unansweredRequestId,
                        cancellationToken: cancellationToken);
            }
        }
    }
}
