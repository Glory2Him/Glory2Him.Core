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
        /// The cross-entity half of the dismissal gate: is this actor in the publisher tier for
        /// the entity behind the approval. For an association that is either endpoint, which a
        /// single-entity service cannot see for itself.
        ///
        /// <para>Deliberately asks nothing about the review being dismissed. Whether it is
        /// already dismissed or soft-deleted is row-local and stays in the service (§7.7 rule
        /// 2b), and authorship is not consulted at all — dismissal happens <i>to</i> a verdict
        /// and is never its author's retraction (§7.7 rule 2).</para>
        /// </summary>
        ValueTask<AccessVerdict> MayDismissApprovalReviewAsync(
            Guid approvalId,
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
    }
}
