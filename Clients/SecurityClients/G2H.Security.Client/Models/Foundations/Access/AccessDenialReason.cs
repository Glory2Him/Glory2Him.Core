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

namespace G2H.Security.Client.Models.Foundations.Access
{
    /// <summary>
    /// Why a decision came back refused, or why the approval conditions are not met.
    ///
    /// <para>This exists so the caller can author its own outward-facing message without ever
    /// echoing this client's explanation text. The explanation is composed from resolved policy
    /// values, and exception messages surface to callers (§14.5) — passing it outward would leak
    /// the approval configuration through a public event address. A caller switches on this enum
    /// and writes its own wording; it logs the explanation server-side instead.</para>
    ///
    /// <para>The members are ordered by when they are evaluated, and the <b>first</b> failing
    /// reason is the one reported. Identity and role failures are decided before any policy is
    /// consulted, so a caller who is not permitted at all never learns anything about the
    /// configuration.</para>
    /// </summary>
    public enum AccessDenialReason
    {
        /// <summary>Not refused. The only value present on a permitted verdict.</summary>
        None = 0,

        /// <summary>The actor is not authenticated, or carries no resolvable user id.</summary>
        NotAuthenticated = 1,

        /// <summary>The actor holds no review-tier role for any named subject (§8.9 rule 1).</summary>
        NotInReviewTier = 2,

        /// <summary>The actor holds no publisher-tier role for any named subject (§8.9 rule 2).</summary>
        NotInPublisherTier = 3,

        /// <summary>
        /// The actor holds a review-tier role but no publisher-tier role. HR-3: a reviewer's whole
        /// instrument is the review record, and the tiers are different jobs rather than
        /// different strengths of the same one.
        /// </summary>
        ReviewerMayNotDecide = 4,

        /// <summary>
        /// The actor is the author of the content under review. HR-1, and no setting relaxes it.
        /// </summary>
        SelfReviewNeverPermitted = 5,

        /// <summary>
        /// The actor is the author and the resolved policy does not allow self-approval (HR-2).
        /// </summary>
        SelfApprovalNotPermitted = 6,

        /// <summary>
        /// The actor holds an active review on this entity, so they have already spent their vote
        /// on this round and another decider must apply the outcome (§8.6 regardless-rule 1).
        /// </summary>
        ReviewerOnThisRoundMayNotDecide = 7,

        /// <summary>
        /// The approval is not <c>Submitted</c>, so the round is not open (§7.7 rule 2b).
        /// </summary>
        ApprovalNotOpenForReview = 8,

        /// <summary>
        /// The actor already has an active review on this approval. They amend that one or file
        /// again after it is dismissed; decisions are never superseded (§7.7 rules 1 and 7).
        /// </summary>
        ActiveReviewAlreadyRecorded = 9,

        /// <summary>The §8.5 conditions are not met and no bypass was requested.</summary>
        ApprovalConditionsNotMet = 10,

        /// <summary>
        /// A bypass was requested but the policy closes that route entirely — nobody, publishers
        /// and administrators included (HR-4 route 3).
        /// </summary>
        BypassNotPermitted = 11,

        /// <summary>
        /// A bypass was requested without a reason. Bypass is only tolerable because it is
        /// recorded, and an unexplained one records nothing worth reading.
        /// </summary>
        BypassReasonRequired = 12,

        /// <summary>
        /// Not enough active approving reviews (§8.5 rule 2). Reported by the conditions
        /// evaluation.
        /// </summary>
        ApprovalThresholdNotMet = 13,

        /// <summary>An active rejection blocks the approval under the policy (§8.7).</summary>
        BlockedByRejection = 14,

        /// <summary>An unresolved approval comment is outstanding (§8.5 rule 7).</summary>
        BlockedByUnresolvedApprovalComment = 15,

        /// <summary>The entity's confidence score is exactly zero (§8.5 rule 8).</summary>
        BlockedByZeroConfidenceScore = 16,

        /// <summary>
        /// The parent approval has closed, so its comment thread is closed with it. Separate from
        /// <see cref="ApprovalNotOpenForReview"/> because the two windows are asked about by
        /// different operations and a shared reason would report a review problem for a comment.
        /// </summary>
        ApprovalNotOpenForComment = 17,

        /// <summary>
        /// The parent approval cannot be acted against: either it is soft-deleted, or the
        /// gatherer could not find it at all.
        ///
        /// <para>Soft deletion is the half of "existing, non-deleted parent" that a foreign key
        /// cannot express — the key still resolves, because deletion is a flag rather than a row
        /// removal (§10.4). The missing case is reported through the same reason deliberately: a
        /// caller learns only that the parent is unusable, never which of the two it was.</para>
        /// </summary>
        ParentApprovalUnavailable = 18,

        /// <summary>
        /// The actor did not write the comment. Comments are owned by whoever submitted them: no
        /// role amends another person's words, and an <c>Admin</c> who needs past an unresolved
        /// one bypasses rather than edits it.
        /// </summary>
        NotApprovalCommentAuthor = 19,
    }
}
