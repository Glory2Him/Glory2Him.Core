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
using System.Threading;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Orchestrations.Approvals;

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    internal partial class ApprovalOrchestrationService
    {
        public ValueTask<ApprovalOutcome> ProcessEntityAddedAsync(
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

                // §9.7.3 rule 1. A Draft has not entered a round, so no policy is resolved and
                // nothing can be approved — the record exists only so the later submit has
                // something to transition. Stopping here is the whole of the branch, not an
                // optimisation.
                if (approval.ApprovalStatus != ApprovalStatus.Submitted)
                {
                    return DescribeOutcome(approval, isEntitySyncRequested: false);
                }

                return await EvaluateResolvedApprovalAsync(
                    approval: approval,
                    cancellationToken: cancellationToken);
            });

        // THE ROUND THAT WAS NEVER OPENED. Every read here keys on the Approval, so an entity
        // without one answers NotFound to a caller who can do nothing about it — and two ways in
        // leave entities in exactly that state. Seed data is written straight to the storage
        // broker, so no fact is ever published for it; and a fact that does not land leaves the
        // same gap for content contributed through the API.
        //
        // IT OPENS THE ROUND AND STOPS THERE. Re-running the whole added flow was the earlier
        // shape, and it made a GET a write that could also DECIDE: for an entity at Submitted
        // under a policy with RequireApprovals = false and AutoApproveIfAllApprovalRequirementsMet
        // = true — the shape the seed writes for the personal tier, and one an administrator can
        // set on any tier — the evaluation drove the fresh round to Approved and published the
        // -Approving command under the workflow identity, on a read nobody had audited as a
        // decision.
        //
        // The panel's whole reason for repairing is to have something to REPORT, and reporting
        // needs the row rather than the outcome. So the repair resolves and returns: a round
        // that should have auto-approved does so when its next real fact lands, which is the
        // event the repair is standing in for in the first place. §16.7.2 says what this read
        // may write, and §9.7.2 rule 1 names the read-triggered case beside the fact-triggered
        // one.
        //
        // GATED ON THE ENTITY BEING VISIBLE, and that gate is not optional. ProcessEntityAddedAsync
        // validates the SHAPE of its arguments and nothing else, while the reads that call this
        // take an entity id straight off a route — so without the probe a caller could mint an
        // approval for any GUID they cared to ask about.
        //
        // VISIBLE rather than merely present, which is the question §14.5 rule 3 asks. The
        // earlier gate gathered the entity's AUTHOR and let a non-empty answer through, and a
        // soft-deleted row answers with its author exactly as it did before the takedown: the
        // arms behind the probe are raw by-id reads and this repository has no EF global query
        // filters. So a taken-down entity — which keeps its ApprovalStatus, because removal
        // deliberately leaves the approval alone (§9.7.6) — passed both this gate and the
        // still-in-play gate below, and a read minted a round for a tombstone.
        private async ValueTask RepairMissingApprovalAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken)
        {
            bool isEntityVisible = await this.accessBroker.IsEntityVisibleAsync(
                entityType: entityType,
                entityId: entityId,
                cancellationToken: cancellationToken);

            if (isEntityVisible is false)
            {
                return;
            }

            // AND GATED ON THE ENTITY STILL BEING IN PLAY. A round is opened at the entity's own
            // status (§9.2 rules 1-2), and the foundation admits only Draft or Submitted on an
            // add — so a DECIDED entity has no status this repair could legally open a round at,
            // and opening one at Draft beneath an Approved row would write the divergence §9.8
            // exists to forbid, permanently: the modified flow only follows the entity across
            // the Draft/Submitted pair, so nothing would ever reconcile them again.
            //
            // A decided entity whose round was never recorded is a gap no read may paper over.
            // Left unrepaired, the caller gets the honest NotFound instead of a fabricated round
            // saying its content has not been submitted yet.
            ApprovalStatus? entityApprovalStatus =
                await this.accessBroker.RetrieveEntityApprovalStatusAsync(
                    entityType: entityType,
                    entityId: entityId,
                    cancellationToken: cancellationToken);

            bool isEntityInPlay =
                entityApprovalStatus is ApprovalStatus.Draft or ApprovalStatus.Submitted;

            if (isEntityInPlay is false)
            {
                return;
            }

            // Resolve only. No evaluation, no transition, no command — see the opening note.
            await ResolveApprovalAsync(
                entityType: entityType,
                entityId: entityId,
                cancellationToken: cancellationToken);
        }

        // Reads the conditions and evaluates against them. Every caller that has NOT just
        // changed the review set uses this; the modified flow reads twice on purpose.
        private async ValueTask<ApprovalOutcome> EvaluateResolvedApprovalAsync(
            Approval approval,
            CancellationToken cancellationToken)
        {
            ApprovalConditionsVerdict conditions =
                await this.accessBroker.EvaluateApprovalConditionsByIdAsync(
                    approvalId: approval.Id,
                    cancellationToken: cancellationToken);

            ValidateStorageApprovalConditionsResolved(
                conditions, approval.EntityType, approval.EntityId);

            return await EvaluateApprovalAsync(
                approval: approval,
                conditions: conditions,
                cancellationToken: cancellationToken);
        }

        // §9.7.2. Runs before every reactive branch, and answers with a row that exists.
        private async ValueTask<Approval> ResolveApprovalAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken)
        {
            // UNFILTERED, and that is the whole point of the probe. UX_Approvals_EntityType_EntityId
            // is not filtered on IsDeleted, so a closed approval still occupies the key — and a
            // caller-facing read, being visibility-filtered, would answer "does not exist" for a
            // key that does, inviting an insert that can never succeed (§9.7.2 rule 3).
            ApprovalEntityMatch approvalMatch =
                await this.approvalService.FindApprovalByEntityAsync(
                    entityType: entityType,
                    entityId: entityId,
                    cancellationToken: cancellationToken);

            if (approvalMatch is null)
            {
                // OPENED AT THE STATUS THE ENTITY WAS CREATED AT (§9.2 rules 1–2): a create at
                // Submitted creates the approval at Submitted and enters review immediately; a
                // create at Draft creates it at Draft, and the flow stops there (§9.7.3 rule
                // 1). Read off the entity's own row, never inferred — and anything else,
                // including a row that could not be read, opens at Draft: nothing enters
                // review on a status nobody offered.
                ApprovalStatus? entityApprovalStatus =
                    await this.accessBroker.RetrieveEntityApprovalStatusAsync(
                        entityType: entityType,
                        entityId: entityId,
                        cancellationToken: cancellationToken);

                ApprovalStatus openingStatus = entityApprovalStatus == ApprovalStatus.Submitted
                    ? ApprovalStatus.Submitted
                    : ApprovalStatus.Draft;

                // THE ID IS MINTED HERE. The foundation stamps the audit fields itself but
                // refuses an empty Id, so a row handed over without one was refused on every
                // add this flow ever made — and every unit test mocked the foundation, so the
                // round that never opened was never seen to fail.
                return await this.approvalService.AddApprovalAsync(
                    approval: new Approval
                    {
                        Id = Guid.NewGuid(),
                        EntityType = entityType,
                        EntityId = entityId,
                        ApprovalStatus = openingStatus,
                    },
                    cancellationToken: cancellationToken);
            }

            Approval storageApproval =
                await this.approvalService.RetrieveApprovalByIdAsync(
                    approvalId: approvalMatch.Id,
                    cancellationToken: cancellationToken);

            if (approvalMatch.IsDeleted is false)
            {
                return storageApproval;
            }

            // Reinstated IN PLACE, because the key is occupied and a second insert can never
            // succeed (§9.7.2 rule 2). The review history is deliberately left intact: an entity
            // restored after a takedown resumes where it left off, which is the main advantage of
            // removal not touching the approval at all (§9.7.6).
            storageApproval.IsDeleted = false;
            storageApproval.DeletedBy = null;
            storageApproval.DeletedWhen = null;
            storageApproval.DeletionReason = null;

            return await this.approvalService.ModifyApprovalAsync(
                approval: storageApproval,
                attribution: WorkflowAttribution.System,
                cancellationToken: cancellationToken);
        }

        // §9.7.7, invoked identically by the Added, Modified and Review flows.
        //
        // "Automatic approval" is deliberately not a thing here: RequireApprovals = false makes
        // the conditions trivially met, and AutoApproveIfAllApprovalRequirementsMet applies
        // Approved once they ALREADY are. The second never waives the first, and the verdict
        // reports them as two fields for that reason.
        // Takes the verdict rather than reading it, so a caller that has just changed the
        // review set cannot evaluate against the set it discarded — the modified flow
        // dismisses reviews and must re-read before deciding anything on them.
        private async ValueTask<ApprovalOutcome> EvaluateApprovalAsync(
            Approval approval,
            ApprovalConditionsVerdict conditions,
            CancellationToken cancellationToken)
        {

            // Rules 3 and 5 reach the same place by different routes, and both are "stay
            // Submitted": conditions unmet, or met but nobody asked for the click to be skipped.
            // The manual approve becomes available in the second case — which is what the
            // verdict read exists to tell a UI.
            //
            // A round that is not OPEN reaches the same place, for a different reason: there is
            // no decision to make on it. ProcessEntityModifiedAsync is the one caller with no
            // round-open check of its own, and a Draft round's conditions can be met — so
            // without this, an edit to a draft would compose an approval the foundation then
            // refuses (ValidateStorageApprovalRoundIsOpenForOutcome).
            //
            // Placed HERE rather than at the top of that flow, which was tried and reverted: an
            // early return there skips the stale-review dismissal and the re-read the round
            // legitimately needs. This runs after both, so nothing is lost — and it is a no-op
            // for the two callers that already gate: ProcessEntityAddedAsync in this file, and
            // ProcessApprovalInputsChangedAsync in Flows.cs.
            //
            // The foundation still refuses the write regardless. §14.6 rule 2 makes the
            // duplicate intentional: this stops the flow composing a write it can predict will
            // be refused, and the foundation stops the write whoever composes it.
            bool isRoundOpen = approval.ApprovalStatus == ApprovalStatus.Submitted;

            if (isRoundOpen is false
                || conditions.AreConditionsMet is false
                || conditions.ShouldAutoApprove is false)
            {
                return DescribeOutcome(approval, isEntitySyncRequested: false);
            }

            // §9.7.6 rule 3, asked LAST so it costs a read only where an approval would actually
            // be written. A takedown never touches the approval record, so a round whose subject
            // has gone still reads Submitted and its conditions can still be met — a comment
            // resolved or a review withdrawn after the takedown reaches exactly here.
            //
            // Approving it would write the divergence §9.8 forbids and leave it standing: the
            // entity transition refuses a deleted row, so the approval would reach Approved
            // while the entity stayed where it was, and no reconcile pass exists to settle them.
            // The round is left open instead, which is what a restore resumes from (§9.7.6).
            bool isEntityVisible = await this.accessBroker.IsEntityVisibleAsync(
                entityType: approval.EntityType,
                entityId: approval.EntityId,
                cancellationToken: cancellationToken);

            if (isEntityVisible is false)
            {
                return DescribeOutcome(approval, isEntitySyncRequested: false);
            }

            approval.ApprovalStatus = ApprovalStatus.Approved;

            // Rule 4 is explicit that this is false. An automatic approval waives nothing — it
            // fires precisely because the conditions were met — so recording a bypass would put
            // a waiver on the one approval that provably needed none.
            approval.IsApprovedByBypass = false;
            approval.ApprovedByBypassReason = null;

            Approval approvedApproval = await this.approvalService.ModifyApprovalAsync(
                approval: approval,
                attribution: WorkflowAttribution.System,
                cancellationToken: cancellationToken);

            await PublishEntityApprovalCommandAsync(
                approval: approvedApproval,
                cancellationToken: cancellationToken);

            return DescribeOutcome(approvedApproval, isEntitySyncRequested: true);
        }

        private static ApprovalOutcome DescribeOutcome(
            Approval approval,
            bool isEntitySyncRequested) =>
            new ApprovalOutcome
            {
                ApprovalId = approval.Id,
                EntityType = approval.EntityType,
                EntityId = approval.EntityId,
                ApprovalStatus = approval.ApprovalStatus,
                IsApprovedByBypass = approval.IsApprovedByBypass,
                ApprovedByBypassReason = approval.ApprovedByBypassReason,
                IsEntitySyncRequested = isEntitySyncRequested,
            };
    }
}
