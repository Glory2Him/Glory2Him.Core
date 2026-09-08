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
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.AIReviewerAssignments;

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    /// <summary>
    /// Berean's half of the invitation flow (design §8.6.2, issue #354 Track A) — asking for it,
    /// asking again once it has finished, withdrawing it, and reporting where it stands.
    ///
    /// <para>Deliberately NOT folded into <see cref="ApprovalOrchestrationService"/>'s human
    /// review-request methods: Berean is not a role-bearing identity, so it has no
    /// <c>ApprovalReviewRequest</c> row and none of ReviewRequests.cs's tier-membership or
    /// duplicate-invitation machinery applies to it. It shares only what genuinely IS shared —
    /// <see cref="ResolveReviewerScopeAsync"/> and the requesting-tier gate — with the human
    /// flow.</para>
    ///
    /// <para>§8.6.2's offer is resolved through its own narrow broker read rather than gathered
    /// with that scope, for the same reason: a switch for a non-person does not belong to a
    /// per-person invitation gather, and riding along on it charged every human-flow caller an
    /// <c>ApprovalSetting</c> scan for a field only these methods read.</para>
    /// </summary>
    internal partial class ApprovalOrchestrationService
    {
        /// <summary>
        /// Berean's status on this round (§8.6.2) — whether it is offered, whether it has been
        /// assigned, and how far its (not-yet-built) automated pass has gotten.
        /// </summary>
        public ValueTask<AIReviewerStatus> RetrieveAIReviewerStatusAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveAIReviewerStatus(entityType, entityId);

                ApprovalReviewerScope scope = await ResolveReviewerScopeAsync(
                    entityType: entityType,
                    entityId: entityId,
                    onSecurityContext: ValidateUserMayRequestApprovalReviews,
                    cancellationToken: cancellationToken);

                bool isAIReviewerOffered = await IsAIReviewerOfferedAsync(
                    approvalId: scope.ApprovalId,
                    cancellationToken: cancellationToken);

                AIReviewerAssignment maybeAssignment =
                    await this.aiReviewerAssignmentService
                        .RetrieveAIReviewerAssignmentByApprovalIdAsync(
                            scope.ApprovalId,
                            cancellationToken);

                return new AIReviewerStatus
                {
                    IsOffered = isAIReviewerOffered,
                    IsRequested = maybeAssignment is not null,
                    IsAIReviewCompleted = maybeAssignment?.IsAIReviewCompleted ?? false,
                    IsAIReviewCommentsPresent = maybeAssignment?.IsAIReviewCommentsPresent ?? false,
                };
            });

        /// <summary>
        /// Assigns Berean to this round, or — where a completed assignment already stands — asks
        /// it again. An UPSERT rather than a plain create, because there is no per-user dimension
        /// to key a second row on (see <see cref="AIReviewerAssignment"/>'s own doc): the same
        /// click means something different depending on what the one possible row is doing.
        ///
        /// <list type="bullet">
        /// <item>No live row — creates one, pending.</item>
        /// <item>A completed row — resets it to pending. This is the re-request action: a
        /// moderator asking Berean to look again, after an edit.</item>
        /// <item>A still-pending row — a no-op that returns the standing row, matching §7.9 rule
        /// 4's "the server dissolves a duplicate quietly" for the human path.</item>
        /// </list>
        /// </summary>
        public ValueTask<AIReviewerAssignment> RequestAIReviewerAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRequestAIReviewer(entityType, entityId);

                ApprovalReviewerScope scope = await ResolveReviewerScopeAsync(
                    entityType: entityType,
                    entityId: entityId,
                    onSecurityContext: ValidateUserMayRequestApprovalReviews,
                    cancellationToken: cancellationToken);

                // §7.9 rule 7's window applies here identically: an assignment on a closed round
                // could never be answered.
                ValidateApprovalRoundIsOpenForRequests(scope, entityType, entityId);

                // Fail-closed (§8.4 rule 2), asked fresh on every write rather than trusted from
                // whatever the caller's own screen last showed.
                bool isAIReviewerOffered = await IsAIReviewerOfferedAsync(
                    approvalId: scope.ApprovalId,
                    cancellationToken: cancellationToken);

                ValidateAIReviewerIsOffered(isAIReviewerOffered, entityType, entityId);

                AIReviewerAssignment maybeAssignment =
                    await this.aiReviewerAssignmentService
                        .RetrieveAIReviewerAssignmentByApprovalIdAsync(
                            scope.ApprovalId,
                            cancellationToken);

                if (maybeAssignment is null)
                {
                    // The foundation re-decides the caller's tier and stamps the audit values;
                    // this layer never assumes its own gate was the only one (§14.6 rule 2).
                    try
                    {
                        return await this.aiReviewerAssignmentService
                            .AddAIReviewerAssignmentAsync(
                                new AIReviewerAssignment
                                {
                                    Id = Guid.NewGuid(),
                                    ApprovalId = scope.ApprovalId,
                                },
                                cancellationToken);
                    }

                    // THE RACE, and this method's own doc already promises the answer to it. The
                    // presence check reads a view a moment old, so two callers — a double click,
                    // two moderators — can both find nothing and both write.
                    // UX_AIReviewerAssignments_ApprovalId refuses the loser, correctly, because
                    // one live assignment per round is the invariant; but "somebody else asked
                    // half a second before you" is the same outcome as "you asked twice", which
                    // the still-pending branch below answers with the standing row. Left
                    // uncaught, the caller whose assignment actually stands is told it failed.
                    //
                    // Re-read rather than assumed: the row is the winner's and this caller has
                    // never seen it. Narrower than the human sibling's re-read, which re-resolves
                    // the whole ApprovalReviewerScope because a standing invitation exists only
                    // as a field on it — the round's one AI row is readable by ApprovalId
                    // directly, and the caller who cleared the requesting tier above clears the
                    // foundation's read gate too.
                    //
                    // If it has already gone — withdrawn between the collision and the re-read, a
                    // narrow window but a real one — the collision is the honest answer and goes
                    // back to the caller unchanged.
                    catch (AIReviewerAssignmentDependencyValidationException collisionException)
                        when (collisionException.InnerException
                            is AlreadyExistsAIReviewerAssignmentException)
                    {
                        AIReviewerAssignment winningAssignment =
                            await this.aiReviewerAssignmentService
                                .RetrieveAIReviewerAssignmentByApprovalIdAsync(
                                    scope.ApprovalId,
                                    cancellationToken);

                        if (winningAssignment is null)
                        {
                            throw;
                        }

                        return winningAssignment;
                    }
                }

                if (maybeAssignment.IsAIReviewCompleted is false)
                {
                    return maybeAssignment;
                }

                // THE RESET, and it carries no collision handling of its own. There is no unique
                // index for a modify to violate, so two re-requests arriving together settle one
                // of three ways, and none of them writes anything wrong.
                //
                // ORDINARILY the second is an idempotent repeat: the foundation stamps
                // UpdatedWhen itself before comparing the input against storage, so both callers
                // write the same two false flags.
                //
                // A WITHDRAWAL landing between the read above and this write is refused as a
                // write on a removed row (ValidateStorageAIReviewerAssignmentIsNotDeleted), which
                // is the honest answer to a request that no longer has a subject.
                //
                // AND THE THIRD IS A REFUSAL THIS LAYER TOLERATES rather than dissolves.
                // ValidateAgainstStorageAIReviewerAssignmentOnModify pins the stamped
                // UpdatedWhen against storage's and refuses when the two are the SAME, so a
                // second re-request whose stamp falls in the same clock tick as the first one's
                // write is refused instead of repeated. It reaches the caller as a dependency
                // validation error rather than a 424, and pressing the control again answers it —
                // which is cheaper, and far easier to reason about, than a retry loop around a
                // write whose whole effect is to set two flags that are already set.
                return await ReturnAIReviewerAssignmentToPendingAsync(
                    storageAIReviewerAssignment: maybeAssignment,
                    cancellationToken: cancellationToken);
            });

        /// <summary>
        /// Withdraws Berean's assignment. Unconditional, unlike the human withdrawal (§7.9 rule
        /// 5): re-requesting now covers "ask again after it has finished", so there is no
        /// answered-invitation state left for this to refuse.
        ///
        /// <para>Idempotent — nothing standing is a no-op, not a not-found, the same posture the
        /// human withdrawal takes for the same reason: a stale panel is not a mistake.</para>
        ///
        /// <para>The §8.6.2 offer is NOT asked here, unlike the request path. Switching the
        /// feature off must not strand the assignments made while it was on: taking Berean off a
        /// round is the one act that has to keep working after the switch closes.</para>
        /// </summary>
        public ValueTask<AIReviewerAssignment> WithdrawAIReviewerAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnWithdrawAIReviewer(entityType, entityId);

                ApprovalReviewerScope scope = await ResolveReviewerScopeAsync(
                    entityType: entityType,
                    entityId: entityId,
                    onSecurityContext: ValidateUserMayRequestApprovalReviews,
                    cancellationToken: cancellationToken);

                AIReviewerAssignment maybeAssignment =
                    await this.aiReviewerAssignmentService
                        .RetrieveAIReviewerAssignmentByApprovalIdAsync(
                            scope.ApprovalId,
                            cancellationToken);

                if (maybeAssignment is null)
                {
                    return null;
                }

                return await this.aiReviewerAssignmentService
                    .RemoveAIReviewerAssignmentByIdAsync(
                        aiReviewerAssignmentId: maybeAssignment.Id,
                        cancellationToken: cancellationToken);
            });

        /// <summary>
        /// Puts an assignment back to PENDING — Berean is still on the round, and whatever it
        /// last reported no longer describes the content.
        ///
        /// <para>The RE-REQUEST's write alone, and it goes through the caller-facing foundation
        /// under the caller's own identity because asking Berean to look again is a person's act
        /// and <c>UpdatedBy</c> must name them. The workflow's own reset writes the same two
        /// fields and is a different act — nobody asked for it — so it goes through
        /// <see cref="IAIReviewerAssignmentWorkflowService"/> instead and records the system. The
        /// same split a moderator's withdrawal and the workflow's retirement already have on the
        /// human side.</para>
        ///
        /// <para>The STORED row is what goes back, with only the two flags moved: it already
        /// carries the audit values the foundation's modify compares against storage, because it
        /// was just read from there.</para>
        /// </summary>
        private async ValueTask<AIReviewerAssignment> ReturnAIReviewerAssignmentToPendingAsync(
            AIReviewerAssignment storageAIReviewerAssignment,
            CancellationToken cancellationToken)
        {
            storageAIReviewerAssignment.IsAIReviewCompleted = false;
            storageAIReviewerAssignment.IsAIReviewCommentsPresent = false;

            return await this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                storageAIReviewerAssignment,
                cancellationToken);
        }

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
        // review tier.
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
        private async ValueTask ResetStaleAIReviewerAssignmentAsync(
            Guid approvalId,
            CancellationToken cancellationToken)
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

        // §8.6.2's feature switch, resolved through IAccessBroker rather than read here. This
        // service holds no IApprovalSettingService on purpose (see the constructor's own note):
        // resolving §8.4 here would put most-specific-wins in a second place beside the decision
        // function, which §8.6.1 rule 4 forbids.
        //
        // Fail-closed (§8.4 rule 2). The broker answers null for a round it cannot resolve, and
        // that collapses to false here: a policy nobody could read is not a permission, and a
        // round whose settings could not be resolved is not one Berean may be assigned to.
        private async ValueTask<bool> IsAIReviewerOfferedAsync(
            Guid approvalId,
            CancellationToken cancellationToken)
        {
            AIReviewerPolicyVerdict maybeAIReviewerPolicy =
                await this.accessBroker.ResolveAIReviewerPolicyByIdAsync(
                    approvalId: approvalId,
                    cancellationToken: cancellationToken);

            return maybeAIReviewerPolicy?.IsOffered ?? false;
        }
    }
}
