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

using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;

namespace G2H.Security.Client.Clients.Access
{
    /// <summary>
    /// Approval policy, decided in one place. Approving is an access question, so it is answered
    /// beside identity rather than by a second client of its own.
    ///
    /// <para><b>This is a pure decision function.</b> It holds no store, no connection, no clock
    /// and no ambient identity — everything a decision consults arrives on the request. The
    /// consuming application gathers the rows and passes them; this package must not read them
    /// back, because the project reference runs from that application to here and a read port
    /// pointing the other way would be a build cycle rather than merely poor layering.</para>
    ///
    /// <para>The consequence a caller must respect: it cannot fetch what it was not given, so an
    /// ungathered list reads as <i>empty</i>, and empty is the permissive answer. Every section
    /// of every request is therefore <c>required</c>, which turns a forgotten gather into a
    /// compile error rather than a rule that silently passes.</para>
    ///
    /// <para>Each method answers one question with the inputs that question consults, and no
    /// others. Verdicts are answers, never settings — handing back the resolved policy would mean
    /// each calling service re-implemented the decision over it.</para>
    /// </summary>
    public interface IAccessClient
    {
        /// <summary>
        /// Evaluates whether the approval conditions are satisfied — the approval count, the
        /// rejection block, comment resolution and the zero-score block, as one formula.
        ///
        /// <para>Answers the shared evaluation the added, modified and review flows all run, and
        /// reports separately whether the system should apply the decision without a human click.
        /// It never decides <i>who</i> may act; that is
        /// <see cref="MayDecideApprovalAsync"/>.</para>
        /// </summary>
        ValueTask<ApprovalConditionsVerdict> EvaluateApprovalConditionsAsync(
            ApprovalConditionsRequest approvalConditionsRequest);

        /// <summary>
        /// Decides whether an actor may record or amend an approval review — the review tier, the
        /// unconditional bar on reviewing your own content, the open-round window, and the bar on
        /// a second active review by the same reviewer.
        /// </summary>
        ValueTask<AccessVerdict> MayRecordApprovalReviewAsync(
            RecordReviewRequest recordReviewRequest);

        /// <summary>
        /// Decides whether an actor may add a comment to an approval — that the parent approval
        /// is alive and its round still open.
        ///
        /// <para>Carries no tier: commenting is not reviewing. The contribution gate is row-local
        /// and stays in the foundation service (§14.6); what this answers is the pair of facts
        /// about the parent that a single-entity service may not read.</para>
        /// </summary>
        ValueTask<AccessVerdict> MayRecordApprovalCommentAsync(
            RecordApprovalCommentRequest recordApprovalCommentRequest);

        /// <summary>
        /// Decides whether an actor may change or withdraw a comment — authorship, and the open
        /// round.
        ///
        /// <para>Serves editing and soft-deleting alike, because both ask the same question. No
        /// role widens it: an <c>Admin</c> gets past an unresolved comment by resolving it or by
        /// bypassing the block, never by rewriting it.</para>
        /// </summary>
        ValueTask<AccessVerdict> MayAmendApprovalCommentAsync(
            AmendApprovalCommentRequest amendApprovalCommentRequest);

        /// <summary>
        /// Decides whether an actor may mark a comment resolved or unresolved — the author, or an
        /// <c>Admin</c> acting on their behalf, while the round is open.
        ///
        /// <para>Separate from <see cref="MayAmendApprovalCommentAsync"/> because
        /// <c>IsResolved</c> is a narrower field scope with a wider audience, and answering both
        /// through one method would need a "which fields may I touch here" branch — which the
        /// codebase treats as the signal that there are two operations (§9.7.1 rule 3).</para>
        /// </summary>
        ValueTask<AccessVerdict> MayResolveApprovalCommentAsync(
            ResolveApprovalCommentRequest resolveApprovalCommentRequest);

        /// <summary>
        /// Decides whether an actor may dismiss a review — the publisher tier for the entity
        /// behind the approval, and nothing else.
        ///
        /// <para>Dismissal is the workflow's instrument rather than anyone's button: §8.8 has an
        /// amendment to the reviewed content dismiss every active verdict on it. So this consults
        /// no round window (the round is being re-opened exactly when it fires) and no authorship
        /// (a verdict belongs to whoever filed it and is never self-retracted, §7.7 rule 2).</para>
        ///
        /// <para>What it does narrow is the tier to the approval's own subject, which is what
        /// stops a publisher scoped to one entity type clearing a verdict on another — for an
        /// association, either endpoint (§14.7 posture A′ rule 2).</para>
        /// </summary>
        ValueTask<AccessVerdict> MayDismissApprovalReviewAsync(
            DismissReviewRequest dismissReviewRequest);

        /// <summary>
        /// Decides whether an actor may apply an approval decision — the publisher tier, the bar
        /// on deciding a round you reviewed, self-approval, the approval conditions, and whether
        /// a bypass is available and explained.
        /// </summary>
        ValueTask<AccessVerdict> MayDecideApprovalAsync(
            DecideApprovalRequest decideApprovalRequest);
    }
}
