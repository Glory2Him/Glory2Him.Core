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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Orchestrations.Approvals;

namespace Glory2Him.Core.Services.Orchestrations.ApprovalReviewers
{
    /// <summary>
    /// The reviewer-coordination half of the approval workflow (design §12.5.4, §16.7.4) — who
    /// could be asked to review an entity, asking them, withdrawing the ask, who is still
    /// outstanding, and what everybody the round names is CALLED.
    ///
    /// <para>ITS OWN CONTRACT, and that is the point of this type existing. These operations began
    /// life on <c>IApprovalOrchestrationService</c>, which is the contract for the approval ROUND
    /// — its verdict, its decisions, its reset. Coordinating REVIEWERS is a different subject with
    /// a different population, so carrying both gave that interface two contracts and its exposer
    /// two resources.</para>
    ///
    /// <para>These are the operations that need BOTH stores. Whether a round is open, who owns the
    /// entity and who has already reviewed all come from Core through <c>IAccessBroker</c>; who
    /// holds a review-tier role comes from the identity store through <c>IIdentityUserService</c>
    /// (§12.7.1). Neither store can answer alone, which is why the composition lives here rather
    /// than in a foundation.</para>
    ///
    /// <para>Public — unlike most orchestration interfaces — because an exposer binds to it, and a
    /// public controller constructor cannot accept a less-accessible parameter type. Only the
    /// contract is public; the implementation and its exceptions stay internal and reach the host
    /// through <c>InternalsVisibleTo</c>.</para>
    ///
    /// <para>DELIBERATELY NOT A DEPENDENCY — <c>IApprovalOrchestrationService</c>. An orchestration
    /// calling an orchestration is what the Standard has coordination services for, and there is
    /// no coordination need here: this service resolves the round it needs for itself, and no
    /// <c>ApprovalOrchestration*</c> type crosses its boundary in either direction.</para>
    /// </summary>
    public partial interface IApprovalReviewerOrchestrationService
    {
        /// <summary>
        /// Who is in scope to review this entity (§16.7.4) — the people holding a review-tier
        /// role for it, minus the entity's own author and minus anyone a <c>ReadOnly</c> in this
        /// entity's scope covers (§18.6 rule 2). Anyone who has already answered, and anyone
        /// already invited, stay IN: the read describes the round's population, and deciding what
        /// is left to do belongs to the surface, which needs the whole set to do it.
        ///
        /// <para>Writes nothing and grants nothing. It is a USER-ENUMERATION surface, so it is
        /// restricted to the requesting tier (§7.9 rule 2) and each candidate carries an account
        /// id, a display name and a username, and nothing else — no roles, no email, no account
        /// state. The username is in that minimum rather than an addition to it (§16.7.4): a
        /// display name is not unique, and choosing between two people called "John" from the
        /// name alone is guessing.</para>
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
        /// What everybody THIS ROUND names is CALLED (§16.7.4) — the one name resolver every
        /// review surface asks, rather than a display-name projection per surface.
        ///
        /// <para><b>The gap it closes.</b> <c>ApprovalReview</c> carries <c>CreatedBy</c>, which
        /// is an account id, and the only route that named other people was
        /// <c>/api/admin/users</c> behind the <c>Administrators</c> role. So a <c>Publisher</c>
        /// who is not an administrator — precisely the tier the review panel exists for — could
        /// render their own name and nobody else's. The candidates read does not close it either:
        /// it returns who is in scope for the round, so somebody who reviewed and then lost the
        /// role vanishes from it entirely.</para>
        ///
        /// <para><b>The round is the answer's boundary, and it is also the gate.</b> The set is
        /// built from the approval's review rows — <b>dismissed and soft-deleted ones
        /// included</b>, because a panel renders those and their authors still need naming — its
        /// outstanding invitations, and the authors of the comments on it. The review TIER is
        /// deliberately not part of it: a caller that supplies no ids gives a tier read nothing to
        /// admit, so it would only re-answer <c>ReviewerCandidates</c>, which the panel already
        /// asks and which already carries display names. What is left is what makes the tier gate
        /// compose with an entity gate rather than stand alone: a <c>Tag-Reviewer</c> can name the
        /// people a tag round involves and nobody else.</para>
        ///
        /// <para><b>The comments are read through <c>IApprovalCommentService</c></b> and never off
        /// the gathering broker (§12.5.4 business rule 3), so §14.7 posture D decides which
        /// authors a given caller can name. Naming somebody whose words are hidden from the reader
        /// would say more than the thread does.</para>
        ///
        /// <para><b>No role filter and no disabled filter, in the ONE identity read there is.</b>
        /// Every id came off a row this approval already stores, so the account belongs to the
        /// record whatever has happened to it since — which is the whole point, since the reviewer
        /// who voted and then lost the role is the case that started this. Ids naming nobody are
        /// absent rather than an error, so one deleted account cannot blank a panel.</para>
        ///
        /// <para>Throws <c>NotFoundApprovalReviewerOrchestrationException</c> when no approval
        /// occupies the key, the same as the candidates read — there is no round, so there is
        /// nobody it names.</para>
        /// </summary>
        ValueTask<IReadOnlyList<ReviewerDisplayName>> RetrieveReviewerDisplayNamesAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Invites somebody to review this entity (§7.9). Refuses unless the round is
        /// <c>Submitted</c> (rule 7), the invited person holds a review-tier role for the entity,
        /// does not own it and is not covered by a <c>ReadOnly</c> in its scope (rule 3), and the
        /// caller is in the requesting tier (rule 2).
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
        /// <para><b>Answered through <c>IApprovalReviewRequestService</c> and never off the
        /// gathered scope</b> (§12.5.4 business rule 5). The scope's request rows are deliberately
        /// unfiltered, because invitability is a fact about storage (§16.7.4), so answering a
        /// person from that view would hand them rows their own posture refuses.</para>
        ///
        /// <para><b>Pending only, and that falls out rather than being filtered for.</b> A
        /// withdrawn invitation is soft-deleted by rule 5 and an answered one is retired by rule
        /// 6, so the foundation's visibility filter — which drops deleted rows — leaves exactly
        /// the outstanding set.</para>
        ///
        /// <para>Same tier as the candidates read, and for the same reason: these rows name
        /// people, and §16.7.4 places them under §14.7 posture D. The foundation applies its own
        /// posture underneath, and §14.6 rule 2 makes that duplicate deliberate.</para>
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
        /// id, never a request id.</para>
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
