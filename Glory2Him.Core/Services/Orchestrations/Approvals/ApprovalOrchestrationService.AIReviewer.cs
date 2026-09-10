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

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    /// <summary>
    /// The WORKFLOW's own half of Berean's lifecycle (design §8.6.2) — the step the edit and
    /// reset flows take on nobody's behalf when a round's verdicts stop describing its content.
    ///
    /// <para>The caller-facing trio that used to live here — asking for Berean, asking again,
    /// withdrawing it, and reporting where it stands — has MOVED to
    /// <c>IAIReviewerOrchestrationService</c>. Those are a contract about the AI reviewer, and
    /// this service's contract is the approval ROUND; carrying both gave one interface two
    /// contracts and its exposer two resources.</para>
    ///
    /// <para>This one stayed, and for exactly the reason <c>DismissStaleApprovalReviewsAsync</c>
    /// lives here: it is an internal step of this service's own reset and edit flows, it appears
    /// on no public contract, and nobody asks for it. It is the AI twin of that method, doing for
    /// the one reviewer that is not a person what it does for the ones that are. Moving it would
    /// make this orchestration depend on another orchestration to complete its own flow, which is
    /// what the Standard has coordination services for and there is no coordination need
    /// here.</para>
    /// </summary>
    internal partial class ApprovalOrchestrationService
    {
        // The AI half of §9.7.4's dismissal, wherever a round's verdicts stop describing its
        // content. TWO callers, and they are the same two the human dismissal has: §8.8 rule 1's
        // RequireReapprovalOnChange branch of ProcessEntityModifiedAsync, which is the commoner
        // of the two, and §8.6 HR-4's administrator override in Resets.cs. Sharing one helper is
        // what stops the two drifting on what returning to pending means.
        //
        // NEITHER HALF GOES THROUGH THE CALLER-FACING FOUNDATION, and both for the reason
        // DismissStaleApprovalReviewsAsync writes down for the human rows.
        //
        // The READ is the gathering seam because the caller-facing one is identity-filtered:
        // RetrieveAIReviewerAssignmentByApprovalIdAsync answers null — and logs a denial — for
        // anyone outside the review tier, and the edit path runs under the EDITOR's identity,
        // which for the ordinary case (an author revising their own submission) carries no review
        // role at all. An identity-filtered read must never be the input to an invariant.
        //
        // The WRITE is the workflow seam because returning Berean to pending is the WORKFLOW's
        // act at both sites and nobody else's. Recording the editor, or the administrator who
        // pressed Reset, would make UpdatedBy name somebody who did not perform it — and the
        // public modify would refuse the editor outright anyway, since its gate asks for the
        // review tier. Asking Berean again IS a person's act, and that verb lives on the AI
        // reviewer's own orchestration, under the caller's own identity. Two different acts, and
        // the audit trail has to tell them apart.
        //
        // The human reviews are dismissed and KEPT — the record that somebody looked
        // survives, and dismissal is only what stops it counting. Berean's assignment is both
        // halves in one row: the record of the ask AND the carrier of the two flags a panel reads
        // as a finished pass. So the row STAYS, exactly as an invitation stays, and only the
        // flags go back — withdrawing outright would remove an assignment nobody asked to
        // withdraw, and leave the re-opened round without the reviewer it had.
        //
        // Silent when Berean was never asked, which is the common case, and silent again when the
        // assignment is already pending: there is nothing to take back, and a write would spend
        // an AIReviewerAssignment-Modified fact restating what storage already says. The null
        // answer is what buys both — the gather applies that predicate itself, so this layer
        // never receives a row it is meant to skip.
        //
        // A FAILURE HERE IS LOGGED AND NOT PROPAGATED, and that is the whole of what this step
        // may cost either caller. Both reach it LAST, after work that has already committed —
        // Resets.cs has moved the status, dismissed the reviews and unpublished the entity;
        // ProcessEntityModifiedAsync has run an evaluation that may already have auto-approved
        // and published — and neither has any way to take that back. Letting a storage failure on
        // two boolean flags fault the operation therefore reported a SUCCESSFUL reset as a 424,
        // whose advised retry then hit ValidateStorageApprovalIsDecided and produced a second,
        // unrelated error on a round that was never broken; and on the edit path it faulted a
        // substrate delivery, which is recorded as failed and REDELIVERED, re-running an
        // evaluation on committed work.
        //
        // What a failure actually costs, then: the two flags stay stale, so a panel goes on
        // reporting a completed pass over content that has moved on until a moderator asks Berean
        // again, which clears them. The failure is in the error log and reaches no caller.
        // Nothing is swallowed silently.
        //
        // Cancellation still propagates untouched: a cancelled operation is not a failed one, and
        // the caller's TryCatch is what decides whether it reads as a timeout.
        //
        // THE SAME SHAPE, AND THE SAME ARGUMENT, as the onVerified hook in Substrate.cs: an act
        // that is bookkeeping on somebody else's path does not get to decide that path's outcome,
        // a defect in it is still a defect and still logged, and cancellation is the one thing
        // that passes through. Written with that one's exception FILTER rather than a rethrowing
        // catch, so the cancellation case never unwinds at all.
        private async ValueTask ResetStaleAIReviewerAssignmentAsync(
            Guid approvalId,
            CancellationToken cancellationToken)
        {
            try
            {
                Guid? maybeStaleAssignmentId =
                    await this.accessBroker.FindResettableAIReviewerAssignmentIdAsync(
                        approvalId: approvalId,
                        cancellationToken: cancellationToken);

                if (maybeStaleAssignmentId is null)
                {
                    return;
                }

                await this.aiReviewerAssignmentWorkflowService
                    .ReturnStaleAIReviewerAssignmentToPendingAsync(
                        aiReviewerAssignmentId: maybeStaleAssignmentId.Value,
                        cancellationToken: cancellationToken);
            }
            catch (Exception aiReviewerResetException)
                when (aiReviewerResetException is not OperationCanceledException)
            {
                await this.loggingBroker.LogErrorAsync(aiReviewerResetException);
            }
        }
    }
}
