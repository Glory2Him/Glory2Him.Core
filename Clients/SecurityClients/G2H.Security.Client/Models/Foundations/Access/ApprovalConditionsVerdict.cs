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
    /// The answer to "are the approval conditions met?" — the §8.5 formula, evaluated once.
    /// </summary>
    public class ApprovalConditionsVerdict
    {
        /// <summary>
        /// Whether every condition in §8.5 is satisfied. While this is false the approval stays
        /// <c>Submitted</c>; a blocked entity is <b>not</b> <c>Rejected</c> (§8.5 rule 9).
        /// </summary>
        public required bool AreConditionsMet { get; init; }

        /// <summary>
        /// Whether the system should apply <c>Approved</c> with no human click — true only when
        /// the conditions are already met <i>and</i> the policy asks for it.
        ///
        /// <para>Kept as its own flag rather than left to the caller to derive, because the two
        /// settings behind it are the pair §9.7.7 forbids collapsing: <c>RequireApprovals =
        /// false</c> means no reviews are needed, while
        /// <c>AutoApproveIfAllApprovalRequirementsMet = true</c> means nobody has to click once
        /// they are. A caller deriving this itself is exactly where those two get confused.</para>
        /// </summary>
        public required bool ShouldAutoApprove { get; init; }

        /// <summary>
        /// The first condition that failed, or <see cref="AccessDenialReason.None"/> when the
        /// conditions are met.
        /// </summary>
        public required AccessDenialReason BlockReason { get; init; }

        /// <summary>
        /// How many active approving reviews were counted — dismissed and soft-deleted rows
        /// excluded (§8.5 rule 3).
        /// </summary>
        public required int ApprovalCount { get; init; }

        /// <summary>
        /// How many the resolved policy required, so a caller can log the shortfall without
        /// re-resolving the policy. Zero when <c>RequireApprovals</c> is false.
        /// </summary>
        public required int RequiredNumberOfApprovals { get; init; }

        /// <summary>
        /// A human-readable account of the evaluation, for the server-side log only. The same
        /// warning applies as on <see cref="AccessVerdict.Explanation"/>: this is composed from
        /// resolved policy values and must never reach an exception message or its
        /// <c>Data</c> (§14.5 rule 2).
        /// </summary>
        public required string Explanation { get; init; }
    }
}
