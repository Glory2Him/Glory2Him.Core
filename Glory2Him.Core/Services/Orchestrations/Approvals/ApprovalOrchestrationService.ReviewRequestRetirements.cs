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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    /// <summary>
    /// What the ROUND does to invitations when its own events land (design §7.9 rules 6 and 8) —
    /// a reviewer answers, or the round closes on an outcome.
    ///
    /// <para>These are not the reviewer-coordination operations. Those moved to
    /// <c>IApprovalReviewerOrchestrationService</c> with the rest of §12.5.4; these two are
    /// private, on no contract, and run under the SYSTEM identity through the foundation's
    /// workflow seam because neither retirement is anybody's act.</para>
    ///
    /// <para><b>Both are knowingly temporary here.</b> Issue #522 turns them into subscriptions on
    /// the reviewer orchestration's own substrate, and takes
    /// <c>IApprovalReviewRequestWorkflowService</c> with them. Until then this service keeps that
    /// one seam and these two helpers — a split that removed them ahead of #522 would not
    /// compile.</para>
    /// </summary>
    internal partial class ApprovalOrchestrationService
    {
        /// <summary>
        /// Rule 6 - the invited person answered, so their invitation retires itself. Runs under
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

        // §7.9 rule 8 — the round closed, so what it never answered is retired. THREE callers,
        // and they are every route to an outcome there is: the manual decision (§16.7.1), the
        // §9.7.5 rejection branch, and the §9.7.7 automatic approval. One helper rather than
        // three copies, so the three cannot drift on what closing a round means.
        //
        // GATED ON THE ROUND'S STATUS, and it is worth being exact about what that buys, because
        // an earlier version of this comment was not. The gate is DEAD TODAY: all three callers
        // reach this line having just written Approved or Rejected, so it refuses nothing, and
        // deleting it would turn no test red. ResetApprovalAsync is not held off by it either —
        // that operation never calls this helper at all.
        //
        // It is kept as the guard for the caller that does not exist yet: a fourth route to an
        // outcome, or a retirement moved inside PublishEntityApprovalCommandAsync, which the
        // reset DOES reach and would then start retiring the invitations of a round being put
        // back for review. §14.6 rule 2 makes that kind of duplicate deliberate.
        //
        // What it reads is the approval the CALLER hands over, not a re-read row, which is why it
        // takes the approval rather than an id. Every caller passes what ModifyApprovalAsync
        // echoed back — the persisted row, and §9.8 makes Approval.ApprovalStatus the source of
        // truth written first — so today that IS stored state. A future caller that set the
        // status locally and had not yet written it would pass this gate; re-reading to close
        // that costs a round trip on every close, and the answer to a caller composing an outcome
        // it has not persisted is §9.8, not a second read here.
        //
        // The READ is the gathering seam, and unlike the AI reset this is not a theoretical
        // point: only ONE of the three callers runs under a moderator. The automatic approval
        // runs under whoever's edit or review tipped the round — ordinarily the AUTHOR revising
        // their own submission, who holds no review role — and the caller-facing request read
        // applies §14.7 posture D, so it would answer that caller with nothing and the panel
        // would keep its stale rows with no error anywhere.
        //
        // The WRITE is the workflow seam because the close is nobody's act. Attributing it to
        // whoever cast the deciding vote would have DeletedBy name a person who withdrew nothing,
        // and the public withdraw verb would refuse the system identity anyway — its gate asks
        // for a review tier the system identity does not hold.
        //
        // NO RE-ENTRANCY SUPPRESSION, unlike DismissStaleApprovalReviewsAsync. That one needs it
        // because this service SUBSCRIBES to ApprovalReview-Dismissed and would otherwise re-test
        // the round once per dismissal from inside its own loop. This service binds no handler to
        // ApprovalReviewRequest-Removed — nothing re-tests a round when an invitation goes, since
        // no §8.5 condition reads one — so the loop is observed by nobody.
        //
        // A FAILURE IS LOGGED AND NOT PROPAGATED, the same posture as
        // ResetStaleAIReviewerAssignmentAsync and for the same reason, which is sharper here
        // because one of the three callers is a person pressing Approve. By the time this runs
        // the outcome has COMMITTED and the entity command has been published; faulting would
        // report a decision that fully worked as a 424, and the retry that advises is then
        // refused because the round is no longer Submitted — a second, unrelated error on a round
        // that was never broken. On the two reactive callers it would fault a substrate delivery,
        // which is recorded as failed and REDELIVERED, re-running an evaluation over committed
        // work.
        //
        // What a failure costs instead is exactly the clutter this rule exists to remove: the
        // rows stay, a moderator can still withdraw them by hand, and the failure is in the error
        // log. Cancellation propagates untouched — a cancelled operation is not a failed one.
        //
        // ONE catch around the whole loop rather than one per row, matching both siblings. The
        // rows are independent and nothing here is atomic, so a per-row catch would salvage the
        // remainder — but the failure that actually reaches this loop is storage being unhappy,
        // which the remainder would meet too, and a row a concurrent withdraw took in the
        // meantime does not throw at all: the verb returns it unchanged. What a mid-loop failure
        // costs is that the rest stay outstanding until somebody withdraws them or the round is
        // reset and decided again.
        private async ValueTask RetireUnansweredApprovalReviewRequestsAsync(
            Approval closedApproval,
            CancellationToken cancellationToken)
        {
            bool isRoundClosed = closedApproval.ApprovalStatus
                is ApprovalStatus.Approved or ApprovalStatus.Rejected;

            if (isRoundClosed is false)
            {
                return;
            }

            try
            {
                List<Guid> unansweredRequestIds =
                    await this.accessBroker.FindRetirableApprovalReviewRequestIdsAsync(
                        approvalId: closedApproval.Id,
                        cancellationToken: cancellationToken);

                foreach (Guid unansweredRequestId in unansweredRequestIds)
                {
                    await this.approvalReviewRequestWorkflowService
                        .RetireClosedRoundApprovalReviewRequestAsync(
                            approvalReviewRequestId: unansweredRequestId,
                            cancellationToken: cancellationToken);
                }
            }
            catch (Exception retirementException)
                when (retirementException is not OperationCanceledException)
            {
                await this.loggingBroker.LogErrorAsync(retirementException);
            }
        }
    }
}
