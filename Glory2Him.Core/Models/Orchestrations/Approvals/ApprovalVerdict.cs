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
using Glory2Him.Core.Models.Enums;

namespace Glory2Him.Core.Models.Orchestrations.Approvals
{
    /// <summary>
    /// What may happen to an approval right now, and everything stopping it, answered for the
    /// CURRENT caller (design §16.7.2).
    ///
    /// <para>Producing one grants nothing and decides nothing, and writes only in one case: a
    /// missing round is opened first so there is something to report on (§16.7.2). It
    /// is the same evaluation the approve path runs, asked without acting on the answer, so a
    /// caller can be shown why the button is disabled instead of discovering it by pressing
    /// it.</para>
    ///
    /// <para>Exposed to the <c>Publishers</c> tier and <c>Administrators</c> only. §14.5's denial posture
    /// constrains what an ERROR may reveal, so an unprivileged probe cannot distinguish a
    /// non-public entity from a missing one; it does not constrain what the party the policy is
    /// addressed to may be told deliberately. An approver can already read the entity, its
    /// reviews and its comments individually — this assembles them rather than disclosing
    /// anything new.</para>
    /// </summary>
    public class ApprovalVerdict
    {
        /// <summary>The approval this verdict is about.</summary>
        public required Guid ApprovalId { get; init; }

        /// <summary>The entity under approval.</summary>
        public required EntityType EntityType { get; init; }

        /// <summary>The entity's id.</summary>
        public required Guid EntityId { get; init; }

        /// <summary>The approval's current status, so a caller can distinguish a round that has
        /// not opened from one already decided without inferring it from the reasons.</summary>
        public required ApprovalStatus ApprovalStatus { get; init; }

        /// <summary>
        /// Everything currently blocking approval — empty when nothing is.
        ///
        /// <para>The full set rather than the first failure, because an approver told only about
        /// the threshold adds a reviewer, retries, and only then learns about the comments they
        /// could have settled in the same visit.</para>
        /// </summary>
        public required IReadOnlyList<ApprovalBlockReason> BlockReasons { get; init; }

        /// <summary>
        /// Whether anything blocks approval. DERIVED from <see cref="BlockReasons"/> rather than
        /// set independently, so the two can never disagree — a caller checking this and a
        /// caller checking the set must reach the same conclusion, and a settable flag is how
        /// they end up not doing.
        /// </summary>
        public bool IsBlocked => BlockReasons.Any();

        /// <summary>
        /// Whether THIS caller may approve over the blocks — their role and
        /// <c>DoNotAllowBypassingSettings</c> folded into the one question a UI needs.
        ///
        /// <para>One bool rather than a flag per reason, because a bypass waives the §8.5
        /// conditions WHOLESALE (§9.7.5). There is no half-bypass to express: a block is either
        /// resolved or waived.</para>
        ///
        /// <para>Meaningful only while <see cref="IsBlocked"/> — with nothing to waive, an
        /// ordinary approve is available and no bypass is requested or recorded.</para>
        /// </summary>
        public required bool IsBypassAllowedForCurrentUser { get; init; }

        /// <summary>
        /// Whether this caller may approve WITHOUT a bypass. The whole of the button rule:
        /// enable approve on this, and offer approve-with-bypass on
        /// <see cref="IsBlocked"/> AND <see cref="IsBypassAllowedForCurrentUser"/>.
        ///
        /// <para>Not merely <c>!IsBlocked</c>: the §8.5 conditions being met says nothing about
        /// whether this caller may act on them. A contributor looking at their own fully-approved
        /// submission is unblocked and still may not approve it (HR-2), and neither may the
        /// reviewer whose own review carried it over the line (§8.6 regardless-rule 1).</para>
        /// </summary>
        public required bool CanApprove { get; init; }

        /// <summary>
        /// How many active approving reviews are recorded, and how many the resolved policy
        /// requires. Carried so a caller can render progress — "1 of 3" — rather than only the
        /// blocked/not-blocked bit. <see cref="RequiredNumberOfApprovals"/> is zero when the
        /// policy does not require approvals at all.
        /// </summary>
        public required int ApprovalCount { get; init; }

        /// <inheritdoc cref="ApprovalCount"/>
        public required int RequiredNumberOfApprovals { get; init; }

        /// <summary>
        /// How many approval comments are outstanding — not deleted and not resolved. Reported
        /// even when the policy does not gate on comment resolution, because the count is
        /// evidence a caller may want to show beside an approvable item, not a verdict.
        /// </summary>
        public required int UnresolvedApprovalCommentCount { get; init; }
    }
}
