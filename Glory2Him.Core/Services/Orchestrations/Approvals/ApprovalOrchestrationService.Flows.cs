// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'"
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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals;

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    internal partial class ApprovalOrchestrationService
    {
        public ValueTask<ApprovalOutcome> ProcessEntityModifiedAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnProcessEntity(entityType, entityId);

                Approval approval = await ResolveApprovalAsync(
                    entityType: entityType,
                    entityId: entityId,
                    cancellationToken: cancellationToken);

                // §9.7.4. This flow only ever sees Draft and Submitted, and that is a property of
                // the system rather than an assumption: a terminal row is immutable in place, so a
                // versioned entity's edit becomes a DIFFERENT row running the Added flow, and a
                // single-row entity's edit is refused at the foundation before any fact is
                // published. Neither can arrive here.
                //
                // Every -Modified reaching this point is a content change by construction, so
                // there is no field-comparison gate: approval state is writable only through the
                // transition verb, which publishes -Approved/-Rejected/-Submitted and never
                // -Modified.
                ApprovalConditionsVerdict conditions =
                    await this.accessBroker.EvaluateApprovalConditionsByIdAsync(
                        approvalId: approval.Id,
                        cancellationToken: cancellationToken);

                ValidateStorageApprovalConditionsResolved(
                    conditions, entityType, entityId);

                // The status is deliberately NOT moved. A Draft stays Draft — this flow may never
                // write Submitted onto one, because submitting is somebody's decision to offer
                // the content, not a side effect of editing it (§9.2). And a Submitted row stays
                // Submitted: the edit re-opens the round rather than withdrawing it.
                if (conditions.ShouldResetStaleReviewsOnChange is false)
                {
                    // Never dismisses when the setting is off. The reviews stand, and the
                    // conditions already read are the ones to evaluate against.
                    return await EvaluateApprovalAsync(
                        approval: approval,
                        conditions: conditions,
                        cancellationToken: cancellationToken);
                }

                await DismissStaleApprovalReviewsAsync(
                    approvalId: approval.Id,
                    cancellationToken: cancellationToken);

                // RE-READ, and this is the whole reason evaluation takes its verdict rather than
                // fetching one: the conditions above were measured against reviews that no longer
                // count. Evaluating on them would auto-approve using approvals just discarded —
                // exactly inverting what RequireReapprovalOnChange asked for.
                return await EvaluateResolvedApprovalAsync(
                    approval: approval,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalOutcome> ProcessApprovalInputsChangedAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnProcessApprovalInputsChanged(approvalId);

                Approval approval =
                    await this.approvalService.RetrieveApprovalByIdAsync(
                        approvalId: approvalId,
                        cancellationToken: cancellationToken);

                ValidateStorageApprovalExists(
                    approval is null ? null : new ApprovalEntityMatch
                    {
                        Id = approval.Id,
                        ApprovalStatus = approval.ApprovalStatus,
                        IsDeleted = approval.IsDeleted,
                    },
                    approval?.EntityType ?? default,
                    approval?.EntityId ?? Guid.Empty);

                // A review recorded against a round that is not open decides nothing. The gates
                // on recording it are the review service's; this flow only reacts.
                if (approval.ApprovalStatus != ApprovalStatus.Submitted)
                {
                    return DescribeOutcome(approval, isEntitySyncRequested: false);
                }

                ApprovalConditionsVerdict conditions =
                    await this.accessBroker.EvaluateApprovalConditionsByIdAsync(
                        approvalId: approval.Id,
                        cancellationToken: cancellationToken);

                ValidateStorageApprovalConditionsResolved(
                    conditions, approval.EntityType, approval.EntityId);

                // §9.7.5 rejection branch. A standing rejection under BlockOnReject ends the round
                // IMMEDIATELY — independent of the threshold, and even where approvals have
                // already been recorded. It is reported by the conditions as a block rather than
                // counted, so no evaluation runs and nothing waits for a second opinion.
                //
                // Under BlockOnReject = false the same rejection appears in neither place: it is
                // recorded for audit, never counts toward the threshold, and reviewing continues.
                bool isBlockedByRejection = conditions.BlockReasons
                    .Contains(AccessDenialReason.BlockedByRejection);

                if (isBlockedByRejection)
                {
                    return await RejectApprovalOnStandingRejectionAsync(
                        approval: approval,
                        cancellationToken: cancellationToken);
                }

                return await EvaluateApprovalAsync(
                    approval: approval,
                    conditions: conditions,
                    cancellationToken: cancellationToken);
            });

        // §9.7.5 rule 2. Rejection withholds approval rather than granting it, so nothing is
        // waived and the bypass pair is CLEARED rather than left alone — a row bypass-approved,
        // re-opened and then rejected must stop claiming a waiver it no longer carries.
        //
        // IsLatestVersion and IsPublished are deliberately untouched: a rejection leaves any
        // previously published version of the group exactly where it was, and visibility is
        // gated by ApprovalStatus rather than by unpublishing something (§14.1).
        private async ValueTask<ApprovalOutcome> RejectApprovalOnStandingRejectionAsync(
            Approval approval,
            CancellationToken cancellationToken)
        {
            approval.ApprovalStatus = ApprovalStatus.Rejected;
            approval.IsApprovedByBypass = false;
            approval.ApprovedByBypassReason = null;

            Approval rejectedApproval = await this.approvalService.ModifyApprovalAsync(
                approval: approval,
                cancellationToken: cancellationToken);

            await PublishEntityApprovalCommandAsync(
                approval: rejectedApproval,
                cancellationToken: cancellationToken);

            return DescribeOutcome(rejectedApproval, isEntitySyncRequested: true);
        }

        // §9.7.4. Dismissed, not deleted: the review is a record that somebody looked, and the
        // audit trail keeps it. Dismissal is what stops it counting toward the threshold.
        private async ValueTask DismissStaleApprovalReviewsAsync(
            Guid approvalId,
            CancellationToken cancellationToken)
        {
            // Read UNFILTERED, through the gathering seam rather than the caller-facing service.
            //
            // The caller-facing read is identity-filtered: an actor with no review role sees
            // only reviews they wrote. HR-1 forbids reviewing your own content, so an author
            // revising their own submission sees none of the round's real approvals — the
            // ordinary case. Deciding what to dismiss from that view dismisses nothing and
            // throws nothing, and the evaluation that follows reads storage unfiltered and
            // approves the edit on the strength of a review of the text it just replaced.
            //
            // What a round's reviews ARE is a fact about storage, not about who is asking. An
            // identity-filtered read must never be the input to an invariant.
            List<Guid> staleReviewIds =
                await this.accessBroker.FindDismissableApprovalReviewIdsAsync(
                    approvalId: approvalId,
                    cancellationToken: cancellationToken);

            // Each dismissal publishes ApprovalReview-Dismissed, and this service subscribes to
            // that address (§10.17(a)). Delivery is synchronous, so without this the handler
            // would re-test the round INSIDE the loop — once per review, each time against a
            // set that is still being torn down, and the earliest of those sees a population
            // that has never existed in storage as a settled state.
            //
            // Announced for THIS approval only, so a dismissal arriving for any other round is
            // still heard while this loop runs. try/finally rather than a plain restore because
            // DismissApprovalReviewAsync can throw — today it usually does, since the loop runs
            // under the editor's identity and the dismissal wants the publisher tier (#287) —
            // and a suppression that leaked would silently disable the handler for the rest of
            // the request.
            Guid previouslySuppressedApprovalId = suppressedDismissalApprovalId.Value;
            suppressedDismissalApprovalId.Value = approvalId;

            try
            {
                foreach (Guid staleReviewId in staleReviewIds)
                {
                    await this.approvalReviewService.DismissApprovalReviewAsync(
                        approvalReviewId: staleReviewId,
                        cancellationToken: cancellationToken);
                }
            }
            finally
            {
                suppressedDismissalApprovalId.Value = previouslySuppressedApprovalId;
            }
        }

        // Static because the handler is bound into the singleton broker as a method group while
        // the WebApp registers this service scoped, so the instance that runs a handler is not
        // the instance that serves the request. AsyncLocal rather than a field because the flow
        // and the handler it suppresses are the same logical call, and a field would leak the
        // suppression across concurrent evaluations of different rounds.
        private static readonly AsyncLocal<Guid> suppressedDismissalApprovalId = new();

        private static bool IsDismissalReTestSuppressedFor(Guid approvalId) =>
            approvalId != Guid.Empty
                && suppressedDismissalApprovalId.Value == approvalId;
    }
}
