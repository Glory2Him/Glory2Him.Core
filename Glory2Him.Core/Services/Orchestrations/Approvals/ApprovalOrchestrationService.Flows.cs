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
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Orchestrations.Approvals;

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    internal partial class ApprovalOrchestrationService
    {
        public ValueTask<ApprovalOutcome> ProcessEntityModifiedAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default) =>
            ProcessEntityModifiedAsync(
                entityType: entityType,
                entityId: entityId,
                changedWhen: null,
                cancellationToken: cancellationToken);

        // The flow's own body. changedWhen is the change's UpdatedWhen, and only the
        // Association-Repointed ear hands one in (§APR9.7.4); this public method and every other
        // ear pass none.
        private ValueTask<ApprovalOutcome> ProcessEntityModifiedAsync(
            EntityType entityType,
            Guid entityId,
            DateTimeOffset? changedWhen,
            CancellationToken cancellationToken) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnProcessEntity(entityType, entityId);

                Approval approval = await ResolveApprovalAsync(
                    entityType: entityType,
                    entityId: entityId,
                    cancellationToken: cancellationToken);

                // §9.7.4. Without a change time this flow only ever sees Draft and Submitted, and
                // that is a property of the system rather than an assumption: a terminal row is
                // immutable in place, so a versioned entity's edit becomes a DIFFERENT row running
                // the Added flow, and a single-row entity's edit is refused at the foundation
                // before any fact is published. Neither can arrive here.
                //
                // THE ONE DECIDED ROUND THAT DOES is a reader's changed reaction, the in-place
                // change §APR7.5.1 rule 3 admits. Its round starts with no reviews, so a round
                // the old reaction decided goes back to Submitted BEFORE ANYTHING ELSE, and is
                // then evaluated like any other open round (§APR9.7.4).
                bool isRoundDecidedOnTheOldPair = changedWhen is not null
                    && await IsRoundDecidedOnTheOldPairAsync(
                        approval: approval,
                        changedWhen: changedWhen.Value,
                        cancellationToken: cancellationToken);

                if (isRoundDecidedOnTheOldPair)
                {
                    approval = await ReturnDecidedRoundToSubmittedAsync(
                        approval: approval,
                        cancellationToken: cancellationToken);
                }

                // ONE approval-state change CAN arrive on a -Modified, and it is the §9.2 rule 3
                // carve-out: the owner or the publishing tier moving the entity between Draft and
                // Submitted through the general modify — an edit and its submission as one act.
                // §9.2 rule 6 has the approval moved in the same branch, and §9.8 forbids the two
                // to stay divergent, so the round follows the entity before anything is
                // evaluated on it. Every OTHER field of approval state stays writable only
                // through the transition verb, which publishes -Approved/-Rejected and never
                // -Modified.
                approval = await FollowEntityBetweenDraftAndSubmittedAsync(
                    approval: approval,
                    entityType: entityType,
                    entityId: entityId,
                    cancellationToken: cancellationToken);

                // Beyond the carve-out above, the status is NOT moved by an edit. A Draft the
                // owner left at Draft stays Draft — this flow never writes Submitted onto one of
                // its own accord, because submitting is somebody's decision to offer the content,
                // not a side effect of editing it (§9.2). And a Submitted row stays Submitted:
                // the edit re-opens the round rather than withdrawing it.
                //
                // ENFORCED as of #287's review, in TWO places. Until then this paragraph was the
                // only thing between a Draft round and EvaluateApprovalAsync, which approves
                // once the conditions are met — and the conditions verdict carries no approval
                // status at all, so a Draft's conditions genuinely can be met. "A Draft stays
                // Draft" was a comment, not a guard.
                //
                // ValidateStorageApprovalRoundIsOpenForOutcome refuses the illegal WRITE in the
                // foundation, unconditionally and for the workflow too. EvaluateApprovalAsync
                // declines to COMPOSE one, so this flow no longer asks for a write it can
                // predict will fail. §14.6 rule 2 makes the pair intentional.
                //
                // Not short-circuited at the TOP of this flow, which was tried and reverted: an
                // early return here skips the stale-review dismissal and the re-read the round
                // legitimately needs. The guard sits after both, inside the evaluation.
                //
                // A CHANGED REACTION SKIPS THIS READ. Its round starts with no reviews, so the old
                // pair's are dismissed whatever RequireReapprovalOnChange says (§APR8.8
                // regardless-rule 1), and a verdict read only to consult that setting would be
                // work done to throw away. The evaluation reads the conditions once, after the
                // dismissal.
                if (changedWhen is null)
                {
                    ApprovalConditionsVerdict conditions =
                        await this.accessBroker.EvaluateApprovalConditionsByIdAsync(
                            approvalId: approval.Id,
                            cancellationToken: cancellationToken);

                    ValidateStorageApprovalConditionsResolved(
                        conditions, entityType, entityId);

                    if (conditions.ShouldResetStaleReviewsOnChange is false)
                    {
                        // Never dismisses when the setting is off. The reviews stand, and the
                        // conditions already read are the ones to evaluate against.
                        return await EvaluateApprovalAsync(
                            approval: approval,
                            conditions: conditions,
                            cancellationToken: cancellationToken);
                    }
                }

                int dismissedReviewCount = await DismissStaleApprovalReviewsAsync(
                    approvalId: approval.Id,
                    changedWhen: changedWhen,
                    cancellationToken: cancellationToken);

                // RE-READ, and this is the whole reason evaluation takes its verdict rather than
                // fetching one: any conditions read above were measured against reviews that no
                // longer count. Evaluating on them would auto-approve using approvals just
                // discarded — exactly inverting what the dismissal is for.
                ApprovalOutcome outcome = await EvaluateResolvedApprovalAsync(
                    approval: approval,
                    cancellationToken: cancellationToken);

                // AND THE AI HALF OF THE SAME DISMISSAL. §8.8 rule 1 invalidates every verdict on
                // the round, and Berean's is one of them — but its assignment is keyed on the
                // APPROVAL rather than on the round's reviews, so nothing above touches it and it
                // would go on reporting a finished pass, with comments, over text it never saw.
                //
                // It runs through the gathering seam and the workflow seam for exactly the
                // reasons DismissStaleApprovalReviewsAsync documents below: this flow runs under
                // the EDITOR's identity, and the ordinary editor is the author revising their own
                // submission, who holds no review role (HR-1). An identity-filtered read would
                // answer null and decide an invariant on it; a write under that identity would be
                // refused, and would name the wrong actor if it were not.
                //
                // AFTER THE RE-EVALUATION, DELIBERATELY. Nothing orders the two by data — the
                // evaluation reads the round's reviews and comments and never the assignment row,
                // and §8.6.2's re-trigger event is not built, so nothing subscribes in the other
                // direction either. It is last because it is the tidy-up, and because the
                // evaluation the dismissal makes necessary must always run.
                //
                // IT CANNOT FAULT THIS FLOW. The helper logs its own failure and returns (see
                // ResetStaleAIReviewerAssignmentAsync), and here that matters more than it does
                // on the reset: EvaluateResolvedApprovalAsync has already COMMITTED by this line
                // and may have auto-approved the round and published the entity. A throw would
                // fault ProcessEntityModifiedAsync on work that succeeded, the substrate would
                // record the delivery as failed and REDELIVER it, and the whole flow — dismissal
                // and evaluation — would run again over committed work.
                //
                // What a failure costs instead: the two flags stay stale until a moderator asks
                // Berean again, and the failure is in the error log. The same posture Resets.cs
                // writes down for its own AI step, because both call the one helper.
                //
                // A CHANGED REACTION'S RETURN AND DISMISSAL ARE DELTAS, and Berean's half rides
                // with them, so it carries their redelivery check (§APR9.7.4, §EVN20 rule 4).
                // Once the change is processed no old-pair review stands and no round the old
                // pair decided is left decided, so a delivery that returned nothing and dismissed
                // nothing has nothing to take back: a pass Berean finished on the new reaction
                // since stands, as a review written since does.
                bool hasFoundNothingLeftToDo = changedWhen is not null
                    && isRoundDecidedOnTheOldPair is false
                    && dismissedReviewCount is 0;

                if (hasFoundNothingLeftToDo is false)
                {
                    await ResetStaleAIReviewerAssignmentAsync(
                        approvalId: approval.Id,
                        cancellationToken: cancellationToken);
                }

                return outcome;
            });

        // §9.2 rules 3 and 6, §9.8: the carve-out moves the ENTITY between Draft and Submitted
        // through a modify, in either direction, and the approval is moved with it. Only that
        // pair. A round anywhere else — decided, dismissed — is left where the workflow put it,
        // and an entity whose status could not be read, or is not an entry status, moves nothing:
        // the round never follows a status nobody could see.
        //
        // Written as the WORKFLOW (System attribution), because the foundation's caller tiers
        // are about who may set approval state by hand, and this is the workflow keeping §9.8
        // true for a change the owner was entitled to make on the entity.
        private async ValueTask<Approval> FollowEntityBetweenDraftAndSubmittedAsync(
            Approval approval,
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken)
        {
            bool isRoundAtEntryStatus =
                approval.ApprovalStatus is ApprovalStatus.Draft or ApprovalStatus.Submitted;

            if (isRoundAtEntryStatus is false)
            {
                return approval;
            }

            ApprovalStatus? entityApprovalStatus =
                await this.accessBroker.RetrieveEntityApprovalStatusAsync(
                    entityType: entityType,
                    entityId: entityId,
                    cancellationToken: cancellationToken);

            bool isEntityAtEntryStatus =
                entityApprovalStatus is ApprovalStatus.Draft or ApprovalStatus.Submitted;

            if (isEntityAtEntryStatus is false || entityApprovalStatus == approval.ApprovalStatus)
            {
                return approval;
            }

            approval.ApprovalStatus = entityApprovalStatus.Value;

            return await this.approvalService.ModifyApprovalAsync(
                approval: approval,
                attribution: WorkflowAttribution.System,
                cancellationToken: cancellationToken);
        }

        // §APR9.7.4's return, for a round the old pair decided: one decided before the change,
        // which is the old reaction's; one approved after it while one of the old pair's reviews
        // still stood, because that approval may have counted it and nothing records which
        // reviews an approval counted; or one rejected after it while one of the old pair's
        // rejections still stood, because then an old review is what blocks it.
        //
        // The round's active reviews are read UNFILTERED, for the reason the dismissal reads them
        // so: the flow runs as the reader, who may see none of them.
        private async ValueTask<bool> IsRoundDecidedOnTheOldPairAsync(
            Approval approval,
            DateTimeOffset changedWhen,
            CancellationToken cancellationToken)
        {
            bool isApproved = approval.ApprovalStatus is ApprovalStatus.Approved;

            // What rejected the round is on the row: the workflow records a standing rejection
            // under the system identity and a direct rejection under the person who took it
            // (WorkflowAttribution, §APR9.7.5). A direct rejection counts no review, so it is
            // never returned, whatever reviews stand beside it.
            bool isStandingRejection =
                approval.ApprovalStatus is ApprovalStatus.Rejected
                    && approval.UpdatedBy == SystemIdentity.UserId;

            if (isApproved is false && isStandingRejection is false)
            {
                return false;
            }

            if (approval.UpdatedWhen < changedWhen)
            {
                return true;
            }

            IReadOnlyList<DismissableApprovalReview> activeReviews =
                await this.accessBroker.FindDismissableApprovalReviewsAsync(
                    approvalId: approval.Id,
                    cancellationToken: cancellationToken);

            if (isApproved)
            {
                return activeReviews.Any(activeReview => activeReview.CreatedWhen < changedWhen);
            }

            // A rejection written since the change is the new pair's own verdict, and returning
            // the round would erase it: the evaluation that follows can only approve or leave
            // it open. So an old rejection returns the round only when no new one stands.
            IEnumerable<DismissableApprovalReview> activeRejections =
                activeReviews.Where(activeReview => activeReview.IsRejection);

            return activeRejections.Any(activeRejection =>
                    activeRejection.CreatedWhen < changedWhen)
                && activeRejections.Any(activeRejection =>
                    activeRejection.CreatedWhen >= changedWhen) is false;
        }

        // Written as the WORKFLOW: nobody asked for the round back, the change did. The bypass
        // pair is cleared in the same write, because a round back at Submitted must not still
        // claim a waiver for a decision it no longer holds (§APR9.7.5). The association follows
        // as a sync, and is unpublished until the round is approved again (§APR9.8).
        private async ValueTask<Approval> ReturnDecidedRoundToSubmittedAsync(
            Approval approval,
            CancellationToken cancellationToken)
        {
            approval.ApprovalStatus = ApprovalStatus.Submitted;
            approval.IsApprovedByBypass = false;
            approval.ApprovedByBypassReason = null;

            Approval returnedApproval = await this.approvalService.ModifyApprovalAsync(
                approval: approval,
                attribution: WorkflowAttribution.System,
                cancellationToken: cancellationToken);

            await PublishEntityApprovalCommandAsync(
                approval: returnedApproval,
                cancellationToken: cancellationToken);

            return returnedApproval;
        }

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
            // §9.7.6 rule 3 names the approve, reject AND bypass operations, and this is the
            // reject one. It is reached BEFORE EvaluateApprovalAsync — the branch above returns
            // straight into it — so the gate inside the evaluation never covered this path.
            //
            // The write it would otherwise make is the §9.8 divergence exactly: the approval
            // moves to Rejected, the command is published, and the entity's own transition
            // refuses the deleted row — leaving Approval = Rejected against an entity still at
            // Submitted, with no reconcile pass to settle them, and a later restore resuming at
            // Rejected instead of the open round §9.7.6 promises.
            //
            // Reached only where a review was recorded against a subject that has since been
            // taken down, which is why the probe sits here rather than on the caller: it costs a
            // read only where a rejection would actually be written.
            bool isEntityVisible = await this.accessBroker.IsEntityVisibleAsync(
                entityType: approval.EntityType,
                entityId: approval.EntityId,
                cancellationToken: cancellationToken);

            if (isEntityVisible is false)
            {
                return DescribeOutcome(approval, isEntitySyncRequested: false);
            }

            approval.ApprovalStatus = ApprovalStatus.Rejected;
            approval.IsApprovedByBypass = false;
            approval.ApprovedByBypassReason = null;

            Approval rejectedApproval = await this.approvalService.ModifyApprovalAsync(
                approval: approval,
                attribution: WorkflowAttribution.System,
                cancellationToken: cancellationToken);

            // §7.9 rule 8's retirement is heard off the write above rather than called here —
            // the rejection route needs no site of its own, because all three routes to an
            // outcome publish Approval-Modified (§12.5.4 business rule 4).
            await PublishEntityApprovalCommandAsync(
                approval: rejectedApproval,
                cancellationToken: cancellationToken);

            return DescribeOutcome(rejectedApproval, isEntitySyncRequested: true);
        }

        // §9.7.4. Dismissed, not deleted: the review is a record that somebody looked, and the
        // audit trail keeps it. Dismissal is what stops it counting toward the threshold.
        //
        // BOUNDED FOR A CHANGED REACTION, and only for one. Its round starts with no reviews, so
        // what goes is the old pair's: every active review written before the change. A review
        // written since is the new pair's own and stands (§APR9.7.4). Every other caller passes
        // no bound and dismisses every active review.
        //
        // Answers how many it dismissed, which is how a changed reaction tells a delivery that
        // still had something to take back from one that found nothing left to do.
        private async ValueTask<int> DismissStaleApprovalReviewsAsync(
            Guid approvalId,
            DateTimeOffset? changedWhen,
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
            List<Guid> staleReviewIds = changedWhen is null
                ? await this.accessBroker.FindDismissableApprovalReviewIdsAsync(
                    approvalId: approvalId,
                    cancellationToken: cancellationToken)
                : await FindOldPairReviewIdsAsync(
                    approvalId: approvalId,
                    changedWhen: changedWhen.Value,
                    cancellationToken: cancellationToken);

            // Each dismissal publishes ApprovalReview-Dismissed, and this service subscribes to
            // that address (§10.17(a)). Delivery is synchronous, so without this the handler
            // would re-test the round INSIDE the loop — once per review, each time against a
            // set that is still being torn down, and the earliest of those sees a population
            // that has never existed in storage as a settled state.
            //
            // Announced for THIS approval only, so a dismissal arriving for any other round is
            // still heard while this loop runs. try/finally rather than a plain restore because
            // the dismissal can throw — storage can fail, and a review dismissed by a concurrent
            // flow between the read above and its turn in the loop is refused a second time —
            // and a suppression that leaked would silently disable the handler for the rest of
            // the request.
            Guid previouslySuppressedApprovalId = suppressedDismissalApprovalId.Value;
            suppressedDismissalApprovalId.Value = approvalId;

            try
            {
                foreach (Guid staleReviewId in staleReviewIds)
                {
                    // Under the SYSTEM identity, not the editor's. The owner whose edit
                    // invalidated these reviews holds no publisher tier, and the reviewers
                    // being withdrawn are the last parties who should withdraw them — this is
                    // a write the workflow must make and no human is permitted to (#196
                    // decision 9). The service mints that context itself; nothing is asserted
                    // from here.
                    await this.approvalReviewWorkflowService.DismissStaleApprovalReviewAsync(
                        approvalReviewId: staleReviewId,
                        cancellationToken: cancellationToken);
                }
            }
            finally
            {
                suppressedDismissalApprovalId.Value = previouslySuppressedApprovalId;
            }

            return staleReviewIds.Count;
        }

        // The old pair's reviews: those whose CreatedWhen precedes the change's (§APR9.7.4). The
        // gather is not bounded by the time, so the comparison is made here.
        private async ValueTask<List<Guid>> FindOldPairReviewIdsAsync(
            Guid approvalId,
            DateTimeOffset changedWhen,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<DismissableApprovalReview> activeReviews =
                await this.accessBroker.FindDismissableApprovalReviewsAsync(
                    approvalId: approvalId,
                    cancellationToken: cancellationToken);

            return activeReviews
                .Where(activeReview => activeReview.CreatedWhen < changedWhen)
                .Select(activeReview => activeReview.Id)
                .ToList();
        }

        // Static because the handler is bound into the singleton broker as a method group while
        // the WebApp registers this service scoped, so the instance that runs a handler is not
        // the instance that serves the request. AsyncLocal rather than a field because the flow
        // and the handler it suppresses are the same logical call, and a field would leak the
        // suppression across concurrent evaluations of different rounds.
        //
        // That an AsyncLocal set here is READABLE inside a delivery is a property of the
        // substrate rather than of this file, and it is measured — ExecutionContextFlowTests.
        // Were it ever untrue, everything below would be dead code that still compiled.
        private static readonly AsyncLocal<Guid> suppressedDismissalApprovalId = new();

        private static bool IsDismissalReTestSuppressedFor(Guid approvalId) =>
            approvalId != Guid.Empty
                && suppressedDismissalApprovalId.Value == approvalId;
    }
}
