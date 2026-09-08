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
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Securities;

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

                AIReviewerAssignment maybeAssignment =
                    await this.aiReviewerAssignmentService
                        .RetrieveAIReviewerAssignmentByApprovalIdAsync(
                            scope.ApprovalId,
                            cancellationToken);

                return new AIReviewerStatus
                {
                    IsOffered = scope.IsAIReviewerOffered,
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
                ValidateAIReviewerIsOffered(scope, entityType, entityId);

                AIReviewerAssignment maybeAssignment =
                    await this.aiReviewerAssignmentService
                        .RetrieveAIReviewerAssignmentByApprovalIdAsync(
                            scope.ApprovalId,
                            cancellationToken);

                if (maybeAssignment is null)
                {
                    return await this.aiReviewerAssignmentService.AddAIReviewerAssignmentAsync(
                        new AIReviewerAssignment
                        {
                            Id = Guid.NewGuid(),
                            ApprovalId = scope.ApprovalId,
                        },
                        cancellationToken);
                }

                if (maybeAssignment.IsAIReviewCompleted is false)
                {
                    return maybeAssignment;
                }

                // THE RESET. The stored row is what goes back, with only the two flags moved —
                // it already carries the audit values the foundation's modify compares against
                // storage, because it was just read from there.
                maybeAssignment.IsAIReviewCompleted = false;
                maybeAssignment.IsAIReviewCommentsPresent = false;

                return await this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    maybeAssignment,
                    cancellationToken);
            });

        /// <summary>
        /// Withdraws Berean's assignment. Unconditional, unlike the human withdrawal (§7.9 rule
        /// 5): re-requesting now covers "ask again after it has finished", so there is no
        /// answered-invitation state left for this to refuse.
        ///
        /// <para>Idempotent — nothing standing is a no-op, not a not-found, the same posture the
        /// human withdrawal takes for the same reason: a stale panel is not a mistake.</para>
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
    }
}
