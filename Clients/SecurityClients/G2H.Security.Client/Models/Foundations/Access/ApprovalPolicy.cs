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
        /// The entity type this row governs.
        /// </summary>
        public required string EntityType { get; init; }

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
        /// Whether every approval comment must be resolved before the conditions are met. It
        /// gates the approval as a whole, never an individual reviewer's verdict — a reviewer may
        /// record <c>Approved</c> while a question is still open (§8.5 rule 7).
        /// </summary>
        public required bool RequireReviewCommentResolutionBeforeApprovals { get; init; }

        /// <summary>
        /// When true, bypass is unavailable to <i>everyone</i>, administrators included, and the
        /// conditions cannot be waived by any route (§8.6 HR-4).
        /// </summary>
        public required bool DoNotAllowBypassingSettings { get; init; }
    }
}
