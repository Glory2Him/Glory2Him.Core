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

using System.Collections.Generic;

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
        /// <summary>
        /// Whether an edit to the approved subject should dismiss the reviews already
        /// recorded against it — <c>ApprovalSetting.RequireReapprovalOnChange</c>, resolved
        /// through the same most-specific-wins pass as everything else here (§8.4).
        ///
        /// <para>It rides on this verdict rather than being read from a settings service by
        /// the caller, because resolving §8.4 in a second place would put most-specific-wins
        /// beside the decision function that owns it (§8.6.1 rule 4). One read answers both
        /// "should the reviews be reset" and "do the conditions now hold".</para>
        ///
        /// <para>Note that the conditions reported ALONGSIDE this flag are the ones that held
        /// BEFORE any dismissal. A caller that acts on the flag must re-evaluate afterwards,
        /// or it will decide against a review set it has just discarded.</para>
        /// </summary>
        public required bool ShouldResetStaleReviewsOnChange { get; init; }

        public required bool ShouldAutoApprove { get; init; }

        /// <summary>
        /// The first condition that failed, or <see cref="AccessDenialReason.None"/> when the
        /// conditions are met.
        /// </summary>
        public required AccessDenialReason BlockReason { get; init; }

        /// <summary>
        /// EVERY condition currently failing, in the same precedence order
        /// <see cref="BlockReason"/> picks its first from — empty when the conditions are met.
        ///
        /// <para>The singular <see cref="BlockReason"/> above cannot answer the question an
        /// approver actually asks. Told only "approval threshold not met", they add a reviewer,
        /// retry, and are then told about an unresolved comment they could have settled in the
        /// same visit. The evaluation knows both at once; short-circuiting threw the rest away.
        /// So the conditions are each evaluated independently and all failures collected
        /// (§16.7.2).</para>
        ///
        /// <para><see cref="BlockReason"/> is retained rather than replaced, and stays the FIRST
        /// of these: <c>AccessVerdict.DenialReason</c> and <c>BypassedBlockReason</c> are
        /// single-valued by design — "there is exactly one value meaning permitted, and it stays
        /// that way" — so a refusal still names one reason. This set is for the caller who is
        /// entitled to the whole picture, which §16.7.2 limits to the publisher tier.</para>
        /// </summary>
        public required IReadOnlyList<AccessDenialReason> BlockReasons { get; init; }

        /// <summary>
        /// How many approval comments are outstanding — not deleted and not resolved. Carried so
        /// a caller can say "two unresolved comments" without re-reading the thread, the same way
        /// <see cref="ApprovalCount"/> and <see cref="RequiredNumberOfApprovals"/> let it report
        /// the shortfall. Zero when the policy does not require comment resolution.
        /// </summary>
        public required int UnresolvedApprovalCommentCount { get; init; }

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
