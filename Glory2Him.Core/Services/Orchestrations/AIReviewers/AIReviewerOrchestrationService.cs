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
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Services.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Services.Foundations.Approvals;

namespace Glory2Him.Core.Services.Orchestrations.AIReviewers
{
    internal partial class AIReviewerOrchestrationService : IAIReviewerOrchestrationService
    {
        private readonly IApprovalWorkflowService approvalService;
        private readonly IAIReviewerAssignmentService aiReviewerAssignmentService;
        private readonly IAccessBroker accessBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ILoggingBroker loggingBroker;

        // Two service references over two foundations, and three brokers — a fraction of what the
        // approval round's orchestration carries, because this contract answers one question
        // about one row. The Approval foundation is reached through the WORKFLOW seam rather than
        // the public one, for the reason that interface documents: what a round IS is a fact
        // about storage, not a view of whoever is asking, and the repair below must see rows the
        // caller-facing read hides.
        //
        // IApprovalSettingService is absent on purpose (§8.6.1 rule 4): resolving §8.4 here would
        // put most-specific-wins in a second place beside the decision function. The §8.6.2 offer
        // arrives as a verdict from IAccessBroker instead.
        //
        // IAIReviewerAssignmentWorkflowService is absent too, and that is the split this service
        // turns on: the workflow's own return-to-pending — the one the edit and reset flows
        // perform on nobody's behalf — stays with the approval orchestration, beside the human
        // dismissal it mirrors. Everything on THIS contract is somebody's act, so everything here
        // goes through the caller-facing foundation under the caller's own identity.
        public AIReviewerOrchestrationService(
            IApprovalWorkflowService approvalService,
            IAIReviewerAssignmentService aiReviewerAssignmentService,
            IAccessBroker accessBroker,
            IEventEnvelopeBroker eventEnvelopeBroker,
            ILoggingBroker loggingBroker)
        {
            this.approvalService = approvalService;
            this.aiReviewerAssignmentService = aiReviewerAssignmentService;
            this.accessBroker = accessBroker;
            this.eventEnvelopeBroker = eventEnvelopeBroker;
            this.loggingBroker = loggingBroker;
        }

        // The shared opening move of all three operations: capture the ambient caller, gate them,
        // and resolve the approval behind the entity. Kept together because doing them in a
        // different order would gate against something other than the stored row.
        //
        // NARROWER THAN THE HUMAN SIBLING'S ResolveReviewerScopeAsync, deliberately, and this is
        // the whole of the sharing decision this service was split on. That resolver ends by
        // gathering an ApprovalReviewerScope — the round's outstanding invitations, its active
        // reviewers, its role subjects and the entity's author — because the human invitation
        // rules are about people: is this person already invited, do they own the entity, are
        // they in the tier. NONE of those rules exist for Berean, which is not a role-bearing
        // identity and has no invitation row. These three operations read exactly TWO fields off
        // the resolved round — its id and its status — and both are on ApprovalEntityMatch, which
        // the probe below already returns. Riding the human gather would charge every AI call a
        // read of invitations and reviewers nothing here looks at.
        //
        // Every gate that resolver asks is still asked here, in the same order and against the
        // same inputs: the requesting tier, then §14.5 rule 3's visibility probe, then the
        // unfiltered lookup, the repair and the exists gate. The two refusal sentences are
        // character-for-character its own, for the reason ValidateStorageEntityIsVisible writes
        // down.
        private async ValueTask<ApprovalEntityMatch> ResolveAIReviewerApprovalAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken)
        {
            // The envelope exists to capture the ambient caller the tier gate runs against.
            // Every operation on this service is privileged — the moderation view, not a public
            // one (§16.7.2).
            var resolutionRequest = new Approval
            {
                EntityType = entityType,
                EntityId = entityId
            };

            EventEnvelope<Approval> envelope =
                await this.eventEnvelopeBroker.CreateAsync(content: resolutionRequest);

            ValidateUserMayRequestAIReviewer(envelope.SecurityContext);

            // §14.5 rule 3, asked BEFORE the approval is looked up and NOT only on the way into
            // the repair. A takedown happens to an item that has already been through the flow,
            // so the ordinary taken-down entity HAS a round — and gating only the repair would
            // leave exactly that case answering with the round's AI status while an id that never
            // existed answered 404, which is the takedown oracle §14.5 rule 3 forbids.
            bool isEntityVisible = await this.accessBroker.IsEntityVisibleAsync(
                entityType: entityType,
                entityId: entityId,
                cancellationToken: cancellationToken);

            ValidateStorageEntityIsVisible(isEntityVisible, entityType, entityId);

            // Unfiltered, for the same reason the round's own reads are: a soft-deleted approval
            // still occupies the key, and a visibility-filtered read would report "no approval"
            // for one that exists (§9.7.2 rule 3).
            ApprovalEntityMatch maybeMatch =
                await this.approvalService.FindApprovalByEntityAsync(
                    entityType: entityType,
                    entityId: entityId,
                    cancellationToken: cancellationToken);

            // The same repair the verdict and the reviewer scope perform, for the same reason
            // (§9.7.2 rule 1's read-triggered case): this panel keys on the approval, so an
            // entity whose round was never opened would answer NotFound to a moderator who can do
            // nothing about it. Asked only when there is nothing there — a repair is the
            // exception, and running its entity probe on every healthy read would buy a storage
            // round trip per call. Re-probed afterwards rather than trusted, so a repair that
            // could not run still ends in the honest NotFound.
            if (maybeMatch is null)
            {
                await RepairMissingApprovalAsync(entityType, entityId, cancellationToken);

                maybeMatch = await this.approvalService.FindApprovalByEntityAsync(
                    entityType: entityType,
                    entityId: entityId,
                    cancellationToken: cancellationToken);
            }

            ValidateStorageApprovalExists(maybeMatch, entityType, entityId);

            return maybeMatch;
        }

        // §9.7.2 rule 1's read-triggered repair, and a NARROWER copy of the approval
        // orchestration's than it first looks. That one delegates to the round's
        // retrieve-or-create, which also REINSTATES a soft-deleted row; this one cannot reach
        // that branch, because it is called only where the unfiltered probe above found nothing
        // at all — there is no row occupying the key to reinstate.
        //
        // RESOLVE ONLY. No evaluation, no transition, no command. A read that also DECIDED would
        // be a GET that publishes: for an entity at Submitted under a policy with
        // RequireApprovals = false and AutoApproveIfAllApprovalRequirementsMet = true — the shape
        // the seed writes for the personal tier — evaluating the fresh round would drive it to
        // Approved under the workflow identity, on a read nobody audited as a decision. A round
        // that should auto-approve does so when its next real fact lands, which is the event this
        // repair is standing in for.
        //
        // GATED ON THE ENTITY BEING VISIBLE, and that gate is not optional even though the
        // resolver above just asked the same question: this helper mints an Approval row for an
        // entity id that came straight off a route, so it defends itself rather than trusting its
        // caller. VISIBLE rather than merely present — the arms behind the probe are raw by-id
        // reads and this repository has no EF global query filters, so a taken-down entity, which
        // keeps its ApprovalStatus because removal deliberately leaves the approval alone
        // (§9.7.6), would otherwise have a round minted for a tombstone.
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
            // exists to forbid, permanently: the modified flow only follows the entity across the
            // Draft/Submitted pair, so nothing would ever reconcile them again.
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

            // PROBED AGAIN BEFORE THE INSERT, and that is not belt and braces — it is what keeps
            // the collision window exactly where the approval orchestration's repair puts it. Its
            // retrieve-or-create re-probes at this same point, so a row that appeared while the
            // two gates above were running is FOUND rather than collided with. Dropping the
            // re-probe would widen the window to span both broker calls and turn a race the
            // round's own repair absorbs into a refusal — new behaviour smuggled in under a move.
            //
            // The collision that OUTLIVES this probe is deliberately not swallowed. It reaches
            // the caller as a dependency validation failure, which is exactly what the approval
            // round's repair does with the same race and what its exposer already documents:
            // retrying finds the round the winner opened.
            ApprovalEntityMatch concurrentMatch =
                await this.approvalService.FindApprovalByEntityAsync(
                    entityType: entityType,
                    entityId: entityId,
                    cancellationToken: cancellationToken);

            if (concurrentMatch is not null)
            {
                return;
            }

            // OPENED AT THE STATUS THE ENTITY WAS CREATED AT (§9.2 rules 1-2): a create at
            // Submitted opens the round at Submitted and enters review immediately; a create at
            // Draft opens it at Draft, and the flow stops there. Read off the entity's own row,
            // never inferred — and anything else, including a status that could not be read,
            // opens at Draft: nothing enters review on a status nobody offered. The gate above
            // has already refused everything but the two entry statuses, so the else branch is
            // unreachable from here; it is written this way so the mapping reads the same as the
            // round's own and stays right if that gate ever widens.
            //
            // THE ID IS MINTED HERE. The foundation stamps the audit fields itself but refuses an
            // empty Id.
            ApprovalStatus openingStatus = entityApprovalStatus == ApprovalStatus.Submitted
                ? ApprovalStatus.Submitted
                : ApprovalStatus.Draft;

            await this.approvalService.AddApprovalAsync(
                approval: new Approval
                {
                    Id = Guid.NewGuid(),
                    EntityType = entityType,
                    EntityId = entityId,
                    ApprovalStatus = openingStatus,
                },
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
