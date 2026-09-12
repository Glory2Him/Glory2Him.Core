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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviewRequests
{
    /// <summary>
    /// The approval workflow's own write seam on <c>ApprovalReviewRequest</c>, separate from the
    /// public <see cref="IApprovalReviewRequestService"/> the exposers bind to. Same
    /// implementation, a narrower door.
    ///
    /// <para><b>Why it exists.</b> §7.9 rule 6 retires an invitation once the invited person
    /// answers it, and says the retirement happens under the SYSTEM identity. That identity is
    /// minted by <c>IEventEnvelopeBroker.CreateSystemAsync</c>, which deliberately carries no
    /// roles — the system flag stands in for the tier by itself. So the public withdraw verb
    /// cannot serve rule 6: its gate asks for a review-tier role and the system identity holds
    /// none. Without this seam the orchestration's only options would be to forge a role-bearing
    /// context or to call the public verb as the answering user, and the second would record
    /// THEM as having withdrawn their own invitation — which is not what happened, and not what
    /// <c>ApprovalReviewRequest.DeletedBy</c> is documented to mean.</para>
    ///
    /// <para><b>Withdrawal and retirement are different acts</b>, which is why this is its own
    /// verb rather than a bypass bolted onto the existing one. A withdrawal says the invitation
    /// was a mistake and is performed by a person in the review tier (§7.9 rule 5); a retirement
    /// says the invitation was ANSWERED and is performed by nobody at all. They differ in who may
    /// act, in what <c>DeletionReason</c> records, and in what a reader should conclude from the
    /// row afterwards.</para>
    ///
    /// <para><b>§7.9 rule 8 joined rule 6 here</b> for exactly the same reasons: a round closing
    /// on an outcome retires every invitation it never answered, that close is nobody's act
    /// either, and the sentence it leaves on the row is a third one again. Two verbs below, one
    /// body behind them.</para>
    ///
    /// <para><c>internal</c> states the intent rather than enforcing it against every assembly —
    /// Core names <c>Glory2Him.WebApp</c> in <c>InternalsVisibleTo</c>. What it does enforce is
    /// the idiomatic route: a public controller cannot take an internal type through its
    /// constructor (CS0051), so reaching this from a portal would take a deliberate,
    /// conspicuous service-locator call rather than ordinary injection.</para>
    /// </summary>
    internal interface IApprovalReviewRequestWorkflowService
    {
        /// <summary>
        /// Retires the invitation the invited person has now answered (§7.9 rule 6) by
        /// soft-deleting it under the system identity. The caller does not hand the context in,
        /// and could not — it asks for the ACT and the service mints the identity, which is what
        /// makes the system flag unforgeable by construction rather than by validation.
        ///
        /// <para>A request that is already gone is returned unchanged and publishes nothing, so a
        /// retried retirement cannot emit a second removal fact.</para>
        /// </summary>
        ValueTask<ApprovalReviewRequest> RetireAnsweredApprovalReviewRequestAsync(
            Guid approvalReviewRequestId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retires an invitation the round has closed on top of (§7.9 rule 8) by soft-deleting it
        /// under the system identity. The invited person never answered, and once the approval
        /// reaches <c>Approved</c> or <c>Rejected</c> they no longer can — a review is refused on
        /// any round that is not <c>Submitted</c> — so the row would go on rendering an ask with
        /// no possible answer.
        ///
        /// <para><b>Its own verb rather than a reason on the one above</b>, for the reason rule 6
        /// makes retirement its own verb in the first place: what a reader concludes from the row
        /// differs, and a caller that could choose the sentence would be writing the audit trail.
        /// Three things can have happened to a deleted request — withdrawn as a mistake (and only
        /// that one names a person), answered, or overtaken by the close — and
        /// <c>DeletionReason</c> is where they are told apart. Everything else about the two
        /// retirements is identical and is implemented once.</para>
        ///
        /// <para>A request that is already gone is returned unchanged and publishes nothing, so
        /// the common case — the round closing on the very review that retired its own invitation
        /// — costs nothing and emits no second removal fact.</para>
        /// </summary>
        ValueTask<ApprovalReviewRequest> RetireClosedRoundApprovalReviewRequestAsync(
            Guid approvalReviewRequestId,
            CancellationToken cancellationToken = default);
    }
}
