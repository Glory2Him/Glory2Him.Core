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
using System.Threading;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Brokers.Securities
{
    /// <summary>
    /// The policy broker. It gathers the rows an approval decision depends on — the settings, the
    /// approval, its reviews and its comments — and hands them to
    /// <c>ISecurityClient.Access</c>, which decides.
    ///
    /// <para>This is what keeps a foundation service single-entity while still enforcing rules
    /// that span four tables: the service calls a <i>broker</i>, exactly as it calls a storage or
    /// clock broker, and never a second service.</para>
    ///
    /// <para><b>It returns a verdict, never settings.</b> Handing back an <c>ApprovalSetting</c>
    /// would put the decision logic back inside each of the seven approvable services, which is
    /// seven places for it to drift.</para>
    /// </summary>
    internal interface IAccessBroker
    {
        /// <summary>
        /// Whether the caller may apply an approval decision to this entity.
        /// </summary>
        ValueTask<AccessVerdict> MayDecideApprovalAsync(
            ApprovalDecisionQuery approvalDecisionQuery,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether the caller may record or amend a review on this approval.
        ///
        /// <para>Takes only the approval's id because everything else is reachable from it: the
        /// approval carries the entity's type and row, and the entity carries the author the
        /// self-review bar compares against and the content type the narrow review role is scoped
        /// to. A caller holding only a review row could not have supplied those.</para>
        /// </summary>
        /// <param name="isAmendingOwnReview">
        /// True when the caller is changing a review they already hold rather than filing a new
        /// one — an amendment must not be refused for finding its own review.
        /// </param>
        ValueTask<AccessVerdict> MayRecordApprovalReviewAsync(
            Guid approvalId,
            bool isAmendingOwnReview,
            Models.Events.SecurityContext securityContext,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The approval-modify gate: is this actor the approval's submitter, or in the REVIEW
        /// tier for the entity behind it. For an association that is either endpoint, which a
        /// single-entity service cannot see for itself.
        ///
        /// <para>Asks nothing about the round — §14.7 posture D rule 3 has reviewers move the
        /// status through this very path. It does ask about authorship, because rule 3 admits
        /// the submitter as well and the service composes this with its row-local check as an
        /// AND: a decision answering only the tier half would delete the owner branch.</para>
        /// </summary>
        ValueTask<AccessVerdict> MayAmendApprovalAsync(
            Guid approvalId,
            Models.Events.SecurityContext securityContext,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The §8.6.1 approval decision, asked from the APPROVAL side. The entity services ask
        /// <see cref="MayDecideApprovalAsync"/> with their own row in hand; ApprovalService
        /// serves the workflow record and cannot supply the entity's author, content type or
        /// confidence score, so this overload resolves all three from storage — off the STORED
        /// approval's target, never a payload's — before asking the same decision function.
        ///
        /// <para>The bypass inputs are the caller's REQUEST, not what will land: the decision
        /// refuses a bypass the policy closes or one with no reason, and its verdict's
        /// <c>IsBypassUsed</c> — false when the conditions were already met, because nothing was
        /// waived — is what the service derives the stored pair from (§9.7.5).</para>
        /// </summary>
        ValueTask<AccessVerdict> MayDecideApprovalByIdAsync(
            Guid approvalId,
            ApprovalDecision decision,
            bool isBypassRequested,
            string? bypassReason,
            Models.Events.SecurityContext securityContext,
            CancellationToken cancellationToken = default);

        ValueTask<AccessVerdict> MayRecordApprovalCommentAsync(
            Guid approvalId,
            Models.Events.SecurityContext securityContext,
            CancellationToken cancellationToken = default);

        /// <param name="commentCreatedBy">
        /// The stored comment's <c>CreatedBy</c>. The caller must read it from storage — this
        /// broker passes it through without verifying it, so a payload-supplied value would
        /// defeat the ownership gate it feeds.
        /// </param>
        ValueTask<AccessVerdict> MayAmendApprovalCommentAsync(
            Guid approvalId,
            string commentCreatedBy,
            Models.Events.SecurityContext securityContext,
            CancellationToken cancellationToken = default);

        ValueTask<AccessVerdict> MayResolveApprovalCommentAsync(
            Guid approvalId,
            string commentCreatedBy,
            Models.Events.SecurityContext securityContext,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Evaluates the §8.5 approval conditions for a stored approval and returns the FULL
        /// verdict — every failing condition, the approval count against the requirement, and
        /// the outstanding comment count (§16.7.2).
        ///
        /// <para>Distinct from <see cref="MayDecideApprovalByIdAsync"/>, which answers "may this
        /// actor decide?" and collapses to one denial reason because
        /// <c>AccessVerdict.DenialReason</c> is single-valued by design. This answers "what is
        /// stopping this approval?", which has no single answer worth giving: an approver told
        /// only about the threshold adds a reviewer, retries, and only then learns about the
        /// comments they could have settled in the same visit.</para>
        ///
        /// <para>Actor-independent — the conditions are a property of the approval, not of who
        /// is asking. Whether the caller may act on them is the separate question
        /// <see cref="MayDecideApprovalByIdAsync"/> answers.</para>
        ///
        /// <para>Returns <c>null</c> when no approval carries the id, so a caller can report
        /// not-found rather than inferring it from an empty verdict.</para>
        /// </summary>
        ValueTask<ApprovalConditionsVerdict?> EvaluateApprovalConditionsByIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The ids of the reviews on an approval that still count toward it — not deleted, not
        /// already dismissed — read from storage without regard to who is asking.
        /// </summary>
        /// <remarks>
        /// <para>Actor-independent, for the same reason
        /// <see cref="EvaluateApprovalConditionsByIdAsync"/> is: what a round's reviews ARE is a
        /// property of the approval, not of the caller.</para>
        ///
        /// <para>It exists because the caller-facing read is not. An actor holding no review
        /// role sees only reviews they wrote themselves, and HR-1 forbids reviewing your own
        /// content — so an author revising their own submission sees NONE of the round's real
        /// approvals. Deciding what to dismiss from that view dismisses nothing, throws nothing,
        /// and then lets the unfiltered evaluation approve the edit on the strength of a review
        /// of the text it just replaced. Both halves of one decision have to read one view.</para>
        ///
        /// <para>This answers WHAT to dismiss, and nothing answers "who may" any more: dismissal
        /// is the approval workflow's own act and no human's (#295), so there is no caller whose
        /// authority to weigh.</para>
        /// </remarks>
        ValueTask<List<Guid>> FindDismissableApprovalReviewIdsAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default);
    }
}
