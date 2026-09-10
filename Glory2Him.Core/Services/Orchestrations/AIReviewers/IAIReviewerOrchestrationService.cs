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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Orchestrations.AIReviewers;

namespace Glory2Him.Core.Services.Orchestrations.AIReviewers
{
    /// <summary>
    /// Berean's half of the invitation flow (design §8.6.2, issue #354 Track A) — asking for it,
    /// asking again once it has finished, withdrawing it, and reporting where it stands.
    ///
    /// <para>ITS OWN CONTRACT, and that is the point of this type existing. These three
    /// operations began life on <c>IApprovalOrchestrationService</c>, which is the contract for
    /// the approval ROUND — its verdict, its decisions, its reset, its human invitations. Berean
    /// is a different subject with a different lifecycle, so bolting it on gave that interface a
    /// second contract and its exposer a second resource. The AI reviewer workflow gets its own
    /// orchestration and its own exposer instead.</para>
    ///
    /// <para>Public — unlike most orchestration interfaces — for the same reason the approval
    /// round's contract is: an exposer binds to it, and a public controller constructor cannot
    /// accept a less-accessible parameter type. Only the contract is public; the implementation
    /// and its exceptions stay internal and reach the host through <c>InternalsVisibleTo</c>.</para>
    ///
    /// <para>DELIBERATELY NOT A DEPENDENCY — the approval orchestration. An orchestration calling
    /// an orchestration is what the Standard has coordination services for, and there is no
    /// coordination need here: this service resolves the round it needs from the Approval
    /// foundation itself, over the two fields these operations actually read.</para>
    ///
    /// <para>DELIBERATELY NOT A DEPENDENCY — <c>IApprovalSettingService</c>. Resolving §8.4 here
    /// would put most-specific-wins in a second place beside the decision function, which
    /// §8.6.1 rule 4 exists to prevent. The §8.6.2 offer is asked as a verdict.</para>
    /// </summary>
    public partial interface IAIReviewerOrchestrationService
    {
        /// <summary>
        /// Berean's status on this round (design §8.6.2) — whether it is offered, whether it has
        /// been assigned, and how far its (not-yet-built) automated pass has gotten. Visible to
        /// the whole requesting tier (§7.9 rule 2), not narrowed to Publishers/Administrators the
        /// way the approval round's verdict read is.
        /// </summary>
        ValueTask<AIReviewerStatus> RetrieveAIReviewerStatusAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Assigns Berean to this round, or asks it again once a prior assignment has completed.
        /// An UPSERT: no live row creates one pending, a completed one resets to pending (the
        /// re-request action), a still-pending one is a no-op returning the standing row.
        ///
        /// <para>Refuses unless the round is <c>Submitted</c> and the resolved
        /// <c>IsAIReviewerOffered</c> is true — fail-closed, asked fresh on every write.</para>
        /// </summary>
        ValueTask<AIReviewerAssignment> RequestAIReviewerAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Withdraws Berean's assignment. Unconditional — re-requesting covers "ask again after
        /// completion", so there is no answered-invitation state left to refuse the way the human
        /// withdrawal does. Idempotent: nothing standing is a no-op, not a not-found.
        /// </summary>
        ValueTask<AIReviewerAssignment> WithdrawAIReviewerAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default);
    }
}
