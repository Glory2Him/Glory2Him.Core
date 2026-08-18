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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Orchestrations.Approvals;

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    /// <summary>
    /// The approval workflow (design §12.5.3, §16.7, §9.7, §10.17).
    ///
    /// <para>Public — unlike most orchestration interfaces — because an exposer binds to it: the
    /// verdict below is what a moderation UI reads to decide whether its approve button is
    /// enabled, and a public controller constructor cannot accept a less-accessible parameter
    /// type. Only the contract is public; the implementation and its exceptions stay internal
    /// and reach the host through <c>InternalsVisibleTo</c>.</para>
    ///
    /// <para>DELIBERATELY NOT A DEPENDENCY — the seven entity services. Approving writes the
    /// decision onto whichever entity the approval names, and taking all seven to do it is the
    /// shape §12.5 entry 1 is already on record as breaking. The decision instead goes out as a
    /// command event the owning service already listens for, which costs this service no
    /// dependencies and makes both halves separately testable (§16.7.1).</para>
    ///
    /// <para>DELIBERATELY NOT A DEPENDENCY — <c>IApprovalSettingService</c>. Resolving §8.4 here
    /// would put most-specific-wins in a second place beside the decision function, which
    /// §8.6.1 rule 4 exists to prevent. Every policy question is asked as a verdict.</para>
    /// </summary>
    public partial interface IApprovalOrchestrationService
    {
        /// <summary>
        /// What may happen to this approval now, and everything stopping it, answered for the
        /// CURRENT caller (§16.7.2). Writes nothing, publishes nothing, grants nothing.
        ///
        /// <para>A UI enables approve on <c>CanApprove</c>, offers approve-with-bypass on
        /// <c>IsBlocked &amp;&amp; IsBypassAllowedForCurrentUser</c>, and otherwise renders
        /// <c>BlockReasons</c> to say why the button is disabled.</para>
        ///
        /// <para>Restricted to the <c>Publisher</c> tier and <c>Admin</c>, and the exposer is
        /// gated to the same roles. §14.5's denial posture constrains what an ERROR may reveal
        /// to an unprivileged probe; it does not constrain what the party the policy is
        /// addressed to may be told deliberately. An approver can already read the entity, its
        /// reviews and its comments individually — this assembles them.</para>
        ///
        /// <para>Throws <c>NotFoundApprovalOrchestrationException</c> when no approval occupies
        /// the key. A <c>Draft</c> approval is NOT that case: it exists, and answers blocked
        /// with <c>BlockedDueToDraftStatus</c>, because "not submitted yet" is a state a
        /// moderator can clear by amending and submitting (§16.7.3).</para>
        /// </summary>
        ValueTask<ApprovalVerdict> RetrieveApprovalVerdictAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Records a human's approve or reject on the <c>Approval</c> row — the source of truth
        /// (§9.8) — and requests the matching entity write as a command event (§16.7.1).
        ///
        /// <para>This is the ONE authorisation in the flow. The bypass pair is written from the
        /// decision's verdict rather than the request, so a waiver asked for and not needed
        /// records none.</para>
        /// </summary>
        ValueTask<ApprovalOutcome> DecideApprovalAsync(
            EntityType entityType,
            Guid entityId,
            ApprovalDecision decision,
            bool isBypassRequested = false,
            string bypassReason = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The Added flow (design §9.7.3). Resolves the entity's approval, creating one at
        /// <c>Draft</c> if the key is unoccupied and reinstating a closed one in place
        /// (§9.7.2), then — only if the approval is <c>Submitted</c> — runs the shared
        /// evaluation (§9.7.7).
        ///
        /// <para>A <c>Draft</c> ends the flow: it has not entered a round, so no policy is
        /// resolved and nothing can be approved.</para>
        /// </summary>
        ValueTask<ApprovalOutcome> ProcessEntityAddedAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default);
    }
}
