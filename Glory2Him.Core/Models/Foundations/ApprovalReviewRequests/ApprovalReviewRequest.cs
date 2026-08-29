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
using Glory2Him.Core.Models.Bases;
using Glory2Him.Core.Models.Foundations.Approvals;

namespace Glory2Him.Core.Models.Foundations.ApprovalReviewRequests
{
    /// <summary>
    /// An invitation to a specific eligible person to review an approval (design §7.9).
    ///
    /// <para><b>An invitation, not an assignment.</b> §8.4 removed reviewer assignment, and this
    /// does not reinstate it: a request grants no eligibility — that stays composed from roles
    /// (§8.3, §18.6) — gates nothing, and appears in <b>no</b> §8.5 condition. The verdict, the
    /// approval counts and the blocks never read it. It exists so a moderation surface can show
    /// who has been asked and has not yet answered.</para>
    ///
    /// <para><b>Why this is not an "empty" <c>ApprovalReview</c>.</b> A reviewer's identity on a
    /// review IS its <c>CreatedBy</c> — there is no separate reviewer field — so a placeholder
    /// review created "for" somebody else has only three shapes, and each breaks an invariant
    /// that holds everywhere else. Written under the requester's identity it occupies the
    /// requester's own one-review-per-approval slot
    /// (<c>UX_ApprovalReviews_ApprovalId_CreatedBy</c>) and the target could never amend it,
    /// reviews being owner-only (§8.6.1). Written under the target's identity it forges the audit
    /// trail, which the signed security context (§10.7) exists to prevent. And widening
    /// owner-only review writes so the row could later be handed over is refused by §14.7 posture
    /// D rule 4. A request is therefore its own row, truthfully created by the requester.</para>
    /// </summary>
    public class ApprovalReviewRequest : IKey, IAudit
    {
        /// <summary>
        /// Primary key identifier for the approval review request.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Identifier of the approval record this request belongs to.
        /// </summary>
        public Guid ApprovalId { get; set; }

        /// <summary>
        /// The invited user's account id — the identity an answering
        /// <c>ApprovalReview.CreatedBy</c> is matched against, and the half of
        /// <c>UX_ApprovalReviewRequests_ApprovalId_RequestedUserId</c> that makes one active
        /// invitation per person mean anything.
        ///
        /// <para>Never a display name: two accounts can share one, so matching on a name would
        /// let one person's review retire another person's invitation.</para>
        /// </summary>
        public string RequestedUserId { get; set; } = string.Empty;

        /// <summary>
        /// The invited user's display name, denormalised at request time for rendering only.
        ///
        /// <para>Carried on the row because the Core database cannot join the identity store's
        /// user table — the two live apart by design (§18.3). It is presentation, never identity:
        /// nothing compares it, and no decision reads it. A name that has since changed renders
        /// stale, which is the cost of not holding the round open on a cross-store join.</para>
        /// </summary>
        public string RequestedUserDisplayName { get; set; } = string.Empty;

        /// <summary>
        /// User identifier for who created the approval review request — the REQUESTER, not the
        /// invited user. Truthful by construction, which is the whole reason this entity exists
        /// rather than a placeholder review.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the approval review request was created.
        /// </summary>
        public DateTimeOffset CreatedWhen { get; set; }

        /// <summary>
        /// User identifier for who last updated the approval review request.
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the approval review request was last updated.
        /// </summary>
        public DateTimeOffset UpdatedWhen { get; set; }

        /// <summary>
        /// User identifier for who withdrew the approval review request, or the system identity
        /// when the request was retired by the invited user answering it (§7.9 rule 6).
        /// </summary>
        public string? DeletedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the approval review request was withdrawn.
        /// </summary>
        public DateTimeOffset? DeletedWhen { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the approval review request is withdrawn.
        /// A withdrawn or answered request renders nowhere and frees its uniqueness slot.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Reason for withdrawal, if applicable.
        /// </summary>
        public string? DeletionReason { get; set; }

        /// <summary>
        /// Navigation to the approval this request belongs to.
        /// </summary>
        public Approval Approval { get; set; } = null!;
    }
}
