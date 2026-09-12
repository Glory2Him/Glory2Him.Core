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
using System.Threading;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Brokers.Securities
{
    /// <summary>
    /// The policy broker. It gathers the rows an approval decision depends on — the settings, the
    /// approval, its reviews and its comments — and hands them to
    /// <c>ISecurityClient.Access</c>, which decides.
    ///
    /// <para>This is what keeps a foundation service single-entity while still enforcing rules
    /// that span four tables: the service calls a <i>broker</i>, exactly as it calls a storage or
    /// clock broker, and never a second service.</para>
    ///
    /// <para><b>It returns a verdict, never settings.</b> Handing back an <c>ApprovalSetting</c>
    /// would put the decision logic back inside each of the seven approvable services, which is
    /// seven places for it to drift.</para>
    /// </summary>
    internal interface IAccessBroker
    {
        /// <summary>
        /// Whether the caller may apply an approval decision to this entity.
        /// </summary>
        ValueTask<AccessVerdict> MayDecideApprovalAsync(
            ApprovalDecisionQuery approvalDecisionQuery,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether the caller may record or amend a review on this approval.
        ///
        /// <para>Takes only the approval's id because everything else is reachable from it: the
        /// approval carries the entity's type and row, and the entity carries the author the
        /// self-review bar compares against and the content type the narrow review role is scoped
        /// to. A caller holding only a review row could not have supplied those.</para>
        /// </summary>
        /// <param name="isAmendingOwnReview">
        /// True when the caller is changing a review they already hold rather than filing a new
        /// one — an amendment must not be refused for finding its own review.
        /// </param>
        ValueTask<AccessVerdict> MayRecordApprovalReviewAsync(
            Guid approvalId,
            bool isAmendingOwnReview,
            Models.Events.SecurityContext securityContext,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The approval-modify gate: is this actor the approval's submitter, or in the REVIEW
        /// tier for the entity behind it. For an association that is either endpoint, which a
        /// single-entity service cannot see for itself.
        ///
        /// <para>Asks nothing about the round — §14.7 posture D rule 3 has reviewers move the
        /// status through this very path. It does ask about authorship, because rule 3 admits
        /// the submitter as well and the service composes this with its row-local check as an
        /// AND: a decision answering only the tier half would delete the owner branch.</para>
        /// </summary>
        ValueTask<AccessVerdict> MayAmendApprovalAsync(
            Guid approvalId,
            Models.Events.SecurityContext securityContext,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The account id of whoever authored the ENTITY an approval is about — the person §14.7
        /// posture D rule 3 calls the submitter, and the only meaning "owner" has on an approval.
        ///
        /// <para>Not <c>Approval.CreatedBy</c>. The workflow owns approval rows outright — it
        /// opens them itself when content is submitted — so that column records the system and
        /// never a person. A gate anchored there refuses every author their own work, silently,
        /// since a submitter holds no role to fall back on.</para>
        ///
        /// <para>Read from STORAGE, like everything else here. It lives on this broker because
        /// resolving it means knowing which table an <c>EntityType</c> points at, which is the
        /// one thing a single-entity foundation service cannot work out for itself.</para>
        /// </summary>
        ValueTask<string> RetrieveEntityAuthorAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The <c>ApprovalStatus</c> the entity's own row carries, or null when the row could not
        /// be read. What the approval resolution (§9.7.2) opens a round AT: a create at
        /// <c>Submitted</c> creates the approval at <c>Submitted</c> and a create at <c>Draft</c>
        /// at <c>Draft</c> (§9.2 rules 1–2), and only the row knows which the caller asked for.
        ///
        /// <para>Read from STORAGE, like the author, and for the same reason: resolving it means
        /// knowing which table an <c>EntityType</c> points at.</para>
        /// </summary>
        ValueTask<ApprovalStatus?> RetrieveEntityApprovalStatusAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether the entity is VISIBLE — it was read back and is not soft-deleted. False for a
        /// row that could not be read at all, and false for a taken-down one, because §14.5 rule
        /// 3 makes those one answer: a soft-deleted entity is not found for <b>every</b> caller,
        /// <c>Administrators</c> included.
        ///
        /// <para>This is the probe an approval gate wants, and it is deliberately not
        /// <see cref="RetrieveEntityAuthorAsync"/> with a non-empty test. That one answers "does
        /// a row exist", which a takedown does not change: the arms behind it are raw by-id
        /// reads and this repository has no EF global query filters, so a tombstone answers with
        /// its author exactly as it did the day before it was removed.</para>
        ///
        /// <para>Read from STORAGE, like the author and the status, and for the same reason:
        /// resolving it means knowing which table an <c>EntityType</c> points at.</para>
        /// </summary>
        ValueTask<bool> IsEntityVisibleAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// <see cref="RetrieveEntityAuthorAsync"/> asked of a whole SET rather than one row:
        /// narrows an approvals query to those whose entity this actor authored. The collection
        /// half of §14.7 posture D rule 1's "authenticated callers see their own".
        ///
        /// <para><b>Why the query goes in and a query comes out.</b> The single-row gates can
        /// afford to resolve the entity behind one approval and compare. A collection read cannot:
        /// <c>EntityType</c> is a discriminator pointing at a different table per value, so the
        /// caller-side answer would be either a lookup per row or a materialised list of
        /// everything the actor ever wrote. This composes instead — one correlated
        /// <c>EXISTS</c> per approvable type, all inside the query the caller was already going
        /// to run — so nothing is loaded to decide what may be seen, and a prolific author costs
        /// no more than a new one.</para>
        ///
        /// <para>It lives here for the same reason its single-row twin does, and more so: the
        /// composition needs a queryable per entity table, which is precisely what a single-entity
        /// foundation service has no way to reach.</para>
        ///
        /// <para>A blank or whitespace <paramref name="authorUserId"/> yields an EMPTY query, never
        /// an unfiltered one. Blank never matches blank on the single-row path either — an
        /// unresolvable author must not become a skeleton key.</para>
        /// </summary>
        ValueTask<IQueryable<Approval>> FilterApprovalsToEntityAuthorAsync(
            IQueryable<Approval> approvals,
            string authorUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The §8.6.1 approval decision, asked from the APPROVAL side. The entity services ask
        /// <see cref="MayDecideApprovalAsync"/> with their own row in hand; ApprovalService
        /// serves the workflow record and cannot supply the entity's author, content type or
        /// confidence score, so this overload resolves all three from storage — off the STORED
        /// approval's target, never a payload's — before asking the same decision function.
        ///
        /// <para>The bypass inputs are the caller's REQUEST, not what will land: the decision
        /// refuses a bypass the policy closes or one with no reason, and its verdict's
        /// <c>IsBypassUsed</c> — false when the conditions were already met, because nothing was
        /// waived — is what the service derives the stored pair from (§9.7.5).</para>
        /// </summary>
        ValueTask<AccessVerdict> MayDecideApprovalByIdAsync(
            Guid approvalId,
            ApprovalDecision decision,
            bool isBypassRequested,
            string? bypassReason,
            Models.Events.SecurityContext securityContext,
            CancellationToken cancellationToken = default);

        /// <param name="commentType">
        /// What the comment being created IS. With <paramref name="isResolved"/> it forms the
        /// birth pairing the decision function rules on: an ask may not be born settled, because
        /// creating one that way IS resolving it, through a gate that never asks who may resolve.
        /// </param>
        /// <param name="isResolved">The resolution the caller is asking to be born with.</param>
        ValueTask<AccessVerdict> MayRecordApprovalCommentAsync(
            Guid approvalId,
            Models.Enums.ApprovalCommentType commentType,
            bool isResolved,
            Models.Events.SecurityContext securityContext,
            CancellationToken cancellationToken = default);

        /// <param name="commentCreatedBy">
        /// The stored comment's <c>CreatedBy</c>. The caller must read it from storage — this
        /// broker passes it through without verifying it, so a payload-supplied value would
        /// defeat the ownership gate it feeds.
        /// </param>
        /// <param name="commentType">What the comment WOULD BE once this write lands.</param>
        /// <param name="isResolved">The resolution this write would leave the row in.</param>
        /// <param name="storageCommentType">
        /// What the STORED row is. With <paramref name="storageIsResolved"/> it lets the decision
        /// rule on the transition rather than the state, so a question somebody already settled
        /// stays editable by its author.
        /// </param>
        /// <param name="storageIsResolved">The resolution the STORED row already carries.</param>
        /// <remarks>
        /// Withdrawal passes the stored pairing as both halves: a soft delete moves neither
        /// field, so the transition veto has nothing to catch.
        /// </remarks>
        ValueTask<AccessVerdict> MayAmendApprovalCommentAsync(
            Guid approvalId,
            string commentCreatedBy,
            Models.Enums.ApprovalCommentType commentType,
            bool isResolved,
            Models.Enums.ApprovalCommentType storageCommentType,
            bool storageIsResolved,
            Models.Events.SecurityContext securityContext,
            CancellationToken cancellationToken = default);

        ValueTask<AccessVerdict> MayResolveApprovalCommentAsync(
            Guid approvalId,
            string commentCreatedBy,
            Models.Events.SecurityContext securityContext,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Evaluates the §8.5 approval conditions for a stored approval and returns the FULL
        /// verdict — every failing condition, the approval count against the requirement, and
        /// the outstanding comment count (§16.7.2).
        ///
        /// <para>Distinct from <see cref="MayDecideApprovalByIdAsync"/>, which answers "may this
        /// actor decide?" and collapses to one denial reason because
        /// <c>AccessVerdict.DenialReason</c> is single-valued by design. This answers "what is
        /// stopping this approval?", which has no single answer worth giving: an approver told
        /// only about the threshold adds a reviewer, retries, and only then learns about the
        /// comments they could have settled in the same visit.</para>
        ///
        /// <para>Actor-independent — the conditions are a property of the approval, not of who
        /// is asking. Whether the caller may act on them is the separate question
        /// <see cref="MayDecideApprovalByIdAsync"/> answers.</para>
        ///
        /// <para>Returns <c>null</c> when no approval carries the id, so a caller can report
        /// not-found rather than inferring it from an empty verdict.</para>
        /// </summary>
        ValueTask<ApprovalConditionsVerdict?> EvaluateApprovalConditionsByIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves §8.6.2's AI-reviewer feature switch for a stored approval — whether Berean is
        /// offered on this round and may be assigned to it at all.
        ///
        /// <para>Asked ON ITS OWN rather than gathered with
        /// <see cref="RetrieveApprovalReviewerScopeByIdAsync"/>, and for two reasons. Berean is not
        /// in the population that scope exists to describe: it holds no role and no
        /// <c>ApprovalReviewRequest</c>, so a switch for it was never a field of a per-person
        /// invitation gather. And carrying it there made every caller of that gather — including
        /// the §16.7.4 name resolver a moderation panel polls — pay a full <c>ApprovalSetting</c>
        /// scan to answer a question only the AI-reviewer paths ask.</para>
        ///
        /// <para>Resolution stays behind <c>IAccessClient</c> like every other policy question
        /// here, so §8.4's most-specific-wins keeps ONE home and no caller re-implements the
        /// tiering (§8.6.1 rule 4).</para>
        ///
        /// <para>Actor-independent — whether the feature is switched on is a property of the
        /// approval's subject, not of who is asking. Whether the caller may act on it is the
        /// separate question the requesting-tier gate answers.</para>
        ///
        /// <para>Returns <c>null</c> when no approval carries the id, so a caller fails closed on
        /// an unresolved round rather than reading a manufactured <c>false</c> as a verdict.</para>
        /// </summary>
        ValueTask<AIReviewerPolicyVerdict?> ResolveAIReviewerPolicyByIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The ids of the reviews on an approval that still count toward it — not deleted, not
        /// already dismissed — read from storage without regard to who is asking.
        /// </summary>
        /// <remarks>
        /// <para>Actor-independent, for the same reason
        /// <see cref="EvaluateApprovalConditionsByIdAsync"/> is: what a round's reviews ARE is a
        /// property of the approval, not of the caller.</para>
        ///
        /// <para>It exists because the caller-facing read is not. An actor holding no review
        /// role sees only reviews they wrote themselves, and HR-1 forbids reviewing your own
        /// content — so an author revising their own submission sees NONE of the round's real
        /// approvals. Deciding what to dismiss from that view dismisses nothing, throws nothing,
        /// and then lets the unfiltered evaluation approve the edit on the strength of a review
        /// of the text it just replaced. Both halves of one decision have to read one view.</para>
        ///
        /// <para>This answers WHAT to dismiss, and nothing answers "who may" any more: dismissal
        /// is the approval workflow's own act and no human's (#295), so there is no caller whose
        /// authority to weigh.</para>
        /// </remarks>
        ValueTask<List<Guid>> FindDismissableApprovalReviewIdsAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The id of the round's ONE live <c>AIReviewerAssignment</c> when it still reports a
        /// finished pass — Berean's half of what a dismissal has to take back — or <c>null</c>
        /// when there is nothing to return to pending.
        /// </summary>
        /// <remarks>
        /// <para>Actor-independent, for the same reason
        /// <see cref="FindDismissableApprovalReviewIdsAsync"/> above is: what a round's reviewers
        /// have recorded is a property of the approval, not of the caller.</para>
        ///
        /// <para>It exists because the caller-facing read is not.
        /// <c>IAIReviewerAssignmentService.RetrieveAIReviewerAssignmentByApprovalIdAsync</c>
        /// answers null — and logs a denial — for anyone outside the review tier, and the editor
        /// whose change made the assignment stale is ordinarily the AUTHOR revising their own
        /// submission, who holds no review role at all (HR-1 forbids reviewing your own content).
        /// Deciding what to reset from that view resets nothing, throws nothing, and leaves the
        /// two flags claiming a completed pass over text Berean never saw.</para>
        ///
        /// <para>The staleness predicate lives HERE rather than in the caller, exactly as its
        /// neighbour above filters out the reviews that are already dismissed: "what needs
        /// returning to pending" then has one home, and no caller receives a row it is meant to
        /// skip. It is an optimisation and not the correctness boundary — the transition that
        /// performs the write re-reads the row and re-checks it, because a flag read a moment
        /// earlier is exactly the payload that verb must not trust.</para>
        /// </remarks>
        ValueTask<Guid?> FindResettableAIReviewerAssignmentIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The ids of the review invitations still outstanding on an approval — what a round
        /// closing on an outcome has to retire (§7.9 rule 8) — read from storage without regard
        /// to who is asking.
        /// </summary>
        /// <remarks>
        /// <para>Actor-independent, for the same reason
        /// <see cref="FindDismissableApprovalReviewIdsAsync"/> above is: who a round is still
        /// waiting on is a property of the approval, not of the caller.</para>
        ///
        /// <para>It exists because the caller-facing read is not.
        /// <c>IApprovalReviewRequestService</c> applies §14.7 posture D, and only ONE of the
        /// three routes to an outcome runs under a moderator's identity. The automatic approval
        /// runs under whoever's edit or review tipped the round — ordinarily the AUTHOR revising
        /// their own submission, who holds no review role at all — and the rejection branch under
        /// whoever recorded the rejecting review. Retiring from that view retires nothing and
        /// throws nothing, and the panel goes on showing an ask nobody can answer.</para>
        ///
        /// <para>Narrow rather than a second use of
        /// <see cref="RetrieveApprovalReviewerScopeByIdAsync"/>, which carries the same rows on
        /// its <c>ActiveRequests</c>: that gather resolves the approval's entity and builds the
        /// whole review snapshot to produce them, and a path that needs a list of ids should not
        /// pay for the population behind them.</para>
        ///
        /// <para>The pending predicate lives HERE, exactly as its neighbours filter out the
        /// reviews already dismissed and the assignment already pending: "what is still
        /// outstanding" then has one home, and no caller receives a row it is meant to skip.
        /// It is an optimisation and not the correctness boundary — the transition that performs
        /// the write re-reads the row and returns an already-deleted one unchanged.</para>
        /// </remarks>
        ValueTask<List<Guid>> FindRetirableApprovalReviewRequestIdsAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gathers everything the invitation flow needs about an approval's subject (§7.9,
        /// §16.7.4) — the round's status, the entity's owner, the role subjects the review tier
        /// composes from, and who already holds an active review.
        ///
        /// <para>The reviewers come back TWICE, filtered and unfiltered, because two questions
        /// are asked of the same rows. Invitability turns on a review that still stands, so a
        /// dismissed or withdrawn one releases the person; who the round INVOLVED is released by
        /// nothing, and the unfiltered set — with the active requests beside it — is the WHOLE
        /// of what the name resolver names (§16.7.4), since that resolver reads no tier. A panel
        /// renders a dismissed review, so it has to be able to name its author.</para>
        ///
        /// <para>Gather-only, like every other read here: it writes nothing and decides nothing.
        /// Whether a particular person may be invited is composed above this, because the tier
        /// naming convention (§18.6) belongs in one place and the role MEMBERSHIP behind it lives
        /// in the identity store (§12.7.1), which this broker does not read.</para>
        ///
        /// <para>Gathers nothing about Berean. §8.6.2's feature switch has its own member,
        /// <see cref="ResolveAIReviewerPolicyByIdAsync"/> — it once travelled on this scope, and
        /// every caller here paid an <c>ApprovalSetting</c> scan for a field only the AI-reviewer
        /// paths read.</para>
        ///
        /// <para>Returns <c>null</c> when no approval carries the id, so a caller can report
        /// not-found rather than inferring it from an empty scope.</para>
        /// </summary>
        ValueTask<ApprovalReviewerScope?> RetrieveApprovalReviewerScopeByIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default);
    }
}
