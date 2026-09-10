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
    /// One candidate approval-policy row, as this client sees it (design §8.2). The consuming
    /// application maps its own stored settings onto this shape; only the policy fields cross,
    /// never the row's identity or audit trail.
    ///
    /// <para>Candidates are supplied <b>unresolved</b>. Picking which one applies is part of the
    /// decision (§8.4) and therefore lives here — if the caller resolved first, the tie-break
    /// rules would be re-implemented per caller and drift.</para>
    /// </summary>
    public class ApprovalPolicy
    {
        /// <summary>
        /// The entity type this row governs, or null for the global default tier — "every
        /// entity type" (§8.4).
        /// </summary>
        public required string? EntityType { get; init; }

        /// <summary>
        /// Whether this row is narrowed to personal associations (true) or editorial ones
        /// (false), or null for "every association". Meaningful only on the Association entity
        /// type (§8.4).
        /// </summary>
        public required bool? IsPersonal { get; init; }

        /// <summary>
        /// The content type this row is narrowed to, or null for the entity-type default tier —
        /// "every content type of this entity type".
        /// </summary>
        public required string? ContentType { get; init; }

        /// <summary>
        /// Whether approvals are required at all. When false the approval conditions are
        /// trivially met (§8.5 rule 1). This is <b>not</b> the same as automatic approval and the
        /// two must never be collapsed (§9.7.7).
        /// </summary>
        public required bool RequireApprovals { get; init; }

        /// <summary>
        /// How many active approving reviews are needed, when <see cref="RequireApprovals"/> is
        /// true.
        /// </summary>
        public required int RequiredNumberOfApprovals { get; init; }

        /// <summary>
        /// Whether the system applies <c>Approved</c> without a human click once the conditions
        /// are <i>already</i> met. It never bypasses the conditions and never substitutes for
        /// them (§9.7.7).
        /// </summary>
        public required bool AutoApproveIfAllApprovalRequirementsMet { get; init; }

        /// <summary>
        /// Whether the author may approve their own item. This is the single setting HR-2
        /// governs, and it relaxes <i>approving</i> only — self-<i>review</i> is refused
        /// unconditionally by HR-1 and no setting reaches it.
        /// </summary>
        public required bool AllowSelfApproval { get; init; }

        /// <summary>
        /// Whether one active rejection blocks the approval (§8.7).
        /// </summary>
        public required bool BlockOnReject { get; init; }

        /// <summary>
        /// Whether an entity whose confidence score is exactly zero is blocked. A <b>null</b>
        /// score never blocks — it means the confidence process has not run, not that the entity
        /// was judged worthless (§8.5 rule 8).
        /// </summary>
        public required bool BlockOnZeroApprovalScore { get; init; }

        /// <summary>
        /// Whether edits dismiss the reviews recorded against the previous text (§8.8). This
        /// client does not act on it — dismissal is a write, and this client only decides — but
        /// it travels with the resolved policy because the caller performing that write resolves
        /// the policy through the same call.
        /// </summary>
        public required bool RequireReapprovalOnChange { get; init; }

        /// <summary>
        /// Whether every approval comment must be settled before the conditions are met. It
        /// gates the approval as a whole, never an individual reviewer's verdict — a reviewer may
        /// record <c>Approved</c> while a comment is still outstanding (§8.5 rule 7).
        ///
        /// <para>Only comments that ask for something ever hold this shut. An informational
        /// comment is created settled and never counts against it (§7.8).</para>
        /// </summary>
        public required bool RequireReviewCommentResolutionBeforeApprovals { get; init; }

        /// <summary>
        /// When true, bypass is unavailable to <i>everyone</i>, administrators included, and the
        /// conditions cannot be waived by any route (§8.6 HR-4).
        /// </summary>
        public required bool DoNotAllowBypassingSettings { get; init; }

        /// <summary>
        /// The AI reviewer ("Berean") feature switch (§8.6.2). True offers Berean in the
        /// reviewer-request UI and has it comment under its system identity once asked; false
        /// offers nothing and performs no AI action of any kind.
        /// </summary>
        public required bool IsAIReviewerOffered { get; init; }

        /// <summary>
        /// Whether Berean may additionally cast a vote, decided from
        /// <c>IConfidence.ConfidenceScore</c> against the two thresholds below. Requires
        /// <see cref="IsAIReviewerOffered"/> to be true — this client does not re-derive that,
        /// it travels with whatever the resolved row carries (§8.6.2).
        /// </summary>
        public required bool IsAIAllowedToVote { get; init; }

        /// <summary>
        /// <c>ConfidenceScore</c> value below which Berean would file a <c>Rejected</c> review.
        /// Same 0.00–10.00 <c>decimal(4,2)</c> scale as the score itself (§13.5). Meaningful only
        /// where <see cref="IsAIAllowedToVote"/> is true (§8.6.2).
        /// </summary>
        public required decimal AIApprovalConfidenceRejectionThreshold { get; init; }

        /// <summary>
        /// <c>ConfidenceScore</c> value above which Berean would file an <c>Approved</c> review.
        /// Same scale as <see cref="AIApprovalConfidenceRejectionThreshold"/>, and the higher of
        /// the two (§8.6.2).
        /// </summary>
        public required decimal AIApprovalConfidenceApprovalThreshold { get; init; }
    }
}
