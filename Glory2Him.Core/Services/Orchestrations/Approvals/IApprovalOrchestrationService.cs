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
using System.Threading;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
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
        /// <para>Restricted to the moderation tier — <c>Administrators</c>, the <c>Publishers</c>
        /// tier and the <c>Reviewers</c> tier, matched by suffix so the content-type-scoped
        /// roles of §18.6 qualify too. A reviewer cannot decide (HR-3), but the
        /// verdict is how they see whether their own review completed the round
        /// (§16.7.2). The exposer gates on the same tier; the duplication is deliberate
        /// (§14.6 rule 2).</para>
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

        /// <summary>
        /// The Modified flow (design §9.7.4). Resolves the approval, dismisses the reviews
        /// already recorded when <c>RequireReapprovalOnChange</c> asks for it, then evaluates.
        ///
        /// <para>The status is never moved here: a <c>Draft</c> stays <c>Draft</c>, because
        /// submitting is somebody's decision to offer the content rather than a side effect of
        /// editing it, and a <c>Submitted</c> row stays open.</para>
        /// </summary>
        ValueTask<ApprovalOutcome> ProcessEntityModifiedAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The Review flow (design §9.7.5). Evaluates the round a review was just recorded
        /// against, and ends it immediately when a standing rejection blocks under
        /// <c>BlockOnReject</c> — independent of the approval threshold, and even where
        /// approvals have already been recorded.
        /// </summary>
        ValueTask<ApprovalOutcome> ProcessApprovalInputsChangedAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Who is in scope to review this entity (§16.7.4) — the people holding a review-tier
        /// role for it, minus the entity's own author alone. Anyone who has already answered, and
        /// anyone already invited, stay IN: the read describes the round's population, and
        /// deciding what is left to do belongs to the surface, which needs the whole set to do
        /// it.
        ///
        /// <para>Writes nothing and grants nothing. It is a USER-ENUMERATION surface, so it is
        /// restricted to the requesting tier (§7.9 rule 2) and each candidate carries an account
        /// id and a display name and nothing else.</para>
        ///
        /// <para>Role membership comes from the identity store through the read-only
        /// <c>IdentityCoreStorageBroker</c> (§12.7.1); the tier NAMES are composed here from the
        /// approval's role subjects, so §18.6's convention keeps one home.</para>
        /// </summary>
        ValueTask<IReadOnlyList<ReviewerCandidate>> RetrieveReviewerCandidatesAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Invites somebody to review this entity (§7.9). Refuses unless the round is
        /// <c>Submitted</c> (rule 7), the invited person holds a review-tier role for the entity
        /// and does not own it (rule 3), and the caller is in the requesting tier (rule 2).
        ///
        /// <para><b>Idempotent, and never an error</b> (rule 4). An active invitation already
        /// standing comes back unchanged rather than colliding with the uniqueness index. And
        /// somebody who has already ANSWERED needs no asking, so nothing is created and nothing is
        /// returned: rule 6 retired their invitation the moment they answered, and a fresh one
        /// could never be retired — the vote that would have done it has already happened — nor
        /// withdrawn, since rule 5 refuses to withdraw an answered invitation. Both cases are a
        /// stale panel rather than a mistake, so neither is worth an error.</para>
        /// </summary>
        ValueTask<ApprovalReviewRequest> RequestApprovalReviewAsync(
            EntityType entityType,
            Guid entityId,
            string requestedUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Who has been asked to review this entity and has not yet answered (§7.9) — the read
        /// §7.9 was written around, and without which its opening promise that "a moderation
        /// surface can show who has been asked" could not be kept.
        ///
        /// <para><b>Pending only, and that falls out rather than being filtered for.</b> A
        /// withdrawn invitation is soft-deleted by rule 5 and an answered one is retired by rule 6,
        /// so the foundation's visibility filter — which drops deleted rows — leaves exactly the
        /// outstanding set.</para>
        ///
        /// <para>Same tier as the candidates read, and for the same reason: these rows name people,
        /// and §16.7.4 places them under §14.7 posture D. The foundation applies its own posture
        /// underneath, and §14.6 rule 2 makes that duplicate deliberate.</para>
        /// </summary>
        ValueTask<IReadOnlyList<ApprovalReviewRequest>> RetrieveApprovalReviewRequestsAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Withdraws a pending invitation (§7.9 rule 5) — the undo for one sent to the wrong
        /// person. Open to the whole requesting tier rather than to the requester alone, because
        /// a request carries no verdict to protect and the person who sent it may not be around.
        ///
        /// <para><b>Keyed on the PERSON, not the row.</b> The pair is already unique
        /// (<c>UX_ApprovalReviewRequests_ApprovalId_RequestedUserId</c>), and it is how the
        /// surface thinks — <c>onReviewRequestWithdrawn</c> hands its consumer somebody's account
        /// id, never a request id. Keying on the row id required a round trip that no longer
        /// exists: #352 correctly made the create return <c>204</c>, and the id had appeared
        /// nowhere else, so withdrawal became unreachable from a browser.</para>
        ///
        /// <para><b>Idempotent.</b> Nothing outstanding for that person is a no-op, not a
        /// not-found — withdrawing twice is a stale panel, not a mistake. An invitation that has
        /// been ANSWERED is still refused (rule 5), which is reachable only where retirement has
        /// not run: rule 6 ordinarily removes the row the moment its target answers.</para>
        ///
        /// <para>Distinct from the RETIREMENT of rule 6, which happens when the invited person
        /// answers and runs under the system identity; that has no caller-facing verb.</para>
        /// </summary>
        ValueTask<ApprovalReviewRequest> WithdrawApprovalReviewRequestAsync(
            EntityType entityType,
            Guid entityId,
            string requestedUserId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);
    }
}
