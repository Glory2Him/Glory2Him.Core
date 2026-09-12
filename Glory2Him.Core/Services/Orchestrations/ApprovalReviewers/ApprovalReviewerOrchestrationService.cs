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
using G2H.Security.Client.Models.Securities;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.ApprovalComments;
using Glory2Him.Core.Services.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Services.Foundations.Approvals;
using Glory2Him.Core.Services.Foundations.IdentityUsers;

namespace Glory2Him.Core.Services.Orchestrations.ApprovalReviewers
{
    internal partial class ApprovalReviewerOrchestrationService
        : IApprovalReviewerOrchestrationService
    {
        private readonly IApprovalReviewRequestService approvalReviewRequestService;

        private readonly IApprovalReviewRequestWorkflowService
            approvalReviewRequestWorkflowService;
        private readonly IApprovalCommentService approvalCommentService;
        private readonly IIdentityUserService identityUserService;
        private readonly IApprovalWorkflowService approvalService;
        private readonly IAccessBroker accessBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly IEnvelopeIntegrityBroker envelopeIntegrityBroker;
        private readonly ILoggingBroker loggingBroker;

        // FOUR service dependencies and FOUR exception arms — fifteen downstream blocks, which is
        // 4 + 4 + 3 + 4 — and that is OVER the two-to-three the Florance guidance allows. The
        // deviation is deliberate, argued and signed off (§12.5.4 business rule 1), not a licence
        // anybody else may cite: the design records it in §12.5's register of approved deviations
        // beside the rule itself, and every other orchestration stays within two-to-three.
        //
        // Four is this service's CEILING rather than a new baseline. A fifth service dependency is
        // the tripwire to split again, exactly as seven on ApprovalOrchestrationService was.
        //
        // What each one is here for:
        //
        //   IApprovalReviewRequestService — the invitation rows themselves, read and written
        //   through the CALLER-FACING door so §14.7 posture D is applied beneath this service's
        //   own tier gate (§14.6 rule 2).
        //
        //   IApprovalCommentService — §12.5.4 business rule 3. The display-name resolver names
        //   comment authors through a caller-facing read, so the set is the authors of the
        //   comments THIS caller can see. Reading them off IAccessBroker would name people whose
        //   words the caller cannot read, and that trade is "not tradeable".
        //
        //   IIdentityUserService — role membership lives in the identity store (§12.7.1), and the
        //   tier NAMES are composed here so §18.6's convention keeps one home. A THREE-block arm:
        //   the contract is read-only, so no dependency-validation family exists for a fourth
        //   block to carry.
        //
        //   IApprovalWorkflowService — the read-triggered repair alone (§9.7.2 rule 1). The panel
        //   keys on the approval, so an entity whose round was never opened would answer NotFound
        //   to a moderator who can do nothing about it. Reached through the WORKFLOW seam because
        //   what a round IS is a fact about storage rather than a view of whoever is asking.
        //
        // IAccessBroker and the envelope, integrity and logging brokers oblige no arm, so §12.5's
        // broker rule keeps them outside the count.
        //
        // IApprovalReviewRequestWorkflowService is a FIFTH PARAMETER AND NOT A FIFTH DEPENDENCY,
        // which is the one place the count and the constructor are allowed to disagree. It is a
        // second door onto ApprovalReviewRequestService, and both doors share that class's own
        // TryCatch — ApprovalReviewRequestService.Transitions.cs wraps the two retirement verbs in
        // the same one the caller-facing operations use — so they throw ApprovalReviewRequest*
        // between them and cost ONE arm, which arm 1 above already pays. §16.7.1 rule 2 counts the
        // shared FAMILY rather than the shared instance, and says why the instance is the wrong
        // thing to count.
        //
        // IEventBroker is DELIBERATELY ABSENT, unlike on the approval round's service. Nothing on
        // this contract publishes: the two subscriptions verify an inbound envelope and cause
        // their write through the workflow seam, which publishes for itself. Subscribing is not a
        // reason to hold it either — EventSubscriptionRegistration holds the broker and binds the
        // handler, exactly as it does for every other subscriber (§12.5.4 business rule 1).
        //
        // IApprovalOrchestrationService is DELIBERATELY NOT A DEPENDENCY. An orchestration calling
        // an orchestration is what the Standard has coordination services for, and there is no
        // coordination need: this service resolves the round it needs for itself.
        public ApprovalReviewerOrchestrationService(
            IApprovalReviewRequestService approvalReviewRequestService,
            IApprovalReviewRequestWorkflowService approvalReviewRequestWorkflowService,
            IApprovalCommentService approvalCommentService,
            IIdentityUserService identityUserService,
            IApprovalWorkflowService approvalService,
            IAccessBroker accessBroker,
            IEventEnvelopeBroker eventEnvelopeBroker,
            IEnvelopeIntegrityBroker envelopeIntegrityBroker,
            ILoggingBroker loggingBroker)
        {
            this.approvalReviewRequestService = approvalReviewRequestService;
            this.approvalReviewRequestWorkflowService = approvalReviewRequestWorkflowService;
            this.approvalCommentService = approvalCommentService;
            this.identityUserService = identityUserService;
            this.approvalService = approvalService;
            this.accessBroker = accessBroker;
            this.eventEnvelopeBroker = eventEnvelopeBroker;
            this.envelopeIntegrityBroker = envelopeIntegrityBroker;
            this.loggingBroker = loggingBroker;
        }

        // The shared opening move of every caller-facing operation here: capture the ambient
        // caller, gate them, resolve the approval behind the entity, and gather the scope. Kept
        // together because doing them in a different order would gate against something other
        // than the stored row.
        private async ValueTask<ApprovalReviewerScope> ResolveReviewerScopeAsync(
            EntityType entityType,
            Guid entityId,
            Action<SecurityContext> onSecurityContext,
            CancellationToken cancellationToken)
        {
            var scopeRequest = new Approval
            {
                EntityType = entityType,
                EntityId = entityId
            };

            EventEnvelope<Approval> envelope =
                await this.eventEnvelopeBroker.CreateAsync(content: scopeRequest);

            onSecurityContext(envelope.SecurityContext);

            // The same visibility gate the verdict keeps, for the same reason (14.5 rule 3): the
            // candidates, the display names and the outstanding requests all key on the entity,
            // and a taken-down one must answer not-found on every one of them rather than only
            // on the path that would have repaired a missing round.
            bool isEntityVisible = await this.accessBroker.IsEntityVisibleAsync(
                entityType: entityType,
                entityId: entityId,
                cancellationToken: cancellationToken);

            ValidateStorageEntityIsVisible(isEntityVisible, entityType, entityId);

            // ONE read, keyed on the ENTITY (§12.5.4 business rule 2). The by-id form would need
            // the approval resolved first, through IApprovalWorkflowService.FindApprovalByEntity
            // Async, and the gather would then read the same row a second time. An economy rather
            // than a new capability: the overload is this broker's own entity-keyed approval
            // lookup, unfiltered on IsDeleted for the reason that lookup already is — a
            // soft-deleted approval still occupies the key, and a filtered read would report "no
            // approval" for one that exists (§9.7.2 rule 3) — followed by the identical gather.
            ApprovalReviewerScope maybeScope =
                await this.accessBroker.RetrieveApprovalReviewerScopeByEntityAsync(
                    entityType: entityType,
                    entityId: entityId,
                    cancellationToken: cancellationToken);

            // The same repair the verdict does, for the same reason: the picker and the
            // outstanding-requests list both key on the approval, so an entity whose round was
            // never opened answers NotFound to a moderator who can do nothing about it. Asked
            // only when there is nothing there — a repair is the exception, and running its
            // entity probe on every healthy read would buy a storage round trip per call.
            //
            // Null means NO ROW AT ALL, which is what makes the narrow repair below sound: the
            // overload is unfiltered, so a soft-deleted approval would have answered its scope.
            //
            // RE-READ afterwards rather than trusted, so a repair that could not run still ends
            // in the honest NotFound.
            if (maybeScope is null)
            {
                await RepairMissingApprovalAsync(entityType, entityId, cancellationToken);

                maybeScope = await this.accessBroker.RetrieveApprovalReviewerScopeByEntityAsync(
                    entityType: entityType,
                    entityId: entityId,
                    cancellationToken: cancellationToken);
            }

            ValidateStorageReviewerScopeResolved(maybeScope, entityType, entityId);

            return maybeScope;
        }

        // §9.7.2 rule 1's read-triggered repair, and a NARROWER copy of the approval round's than
        // it first looks. That one delegates to ResolveApprovalAsync, the round's
        // retrieve-or-create, which also REINSTATES a soft-deleted row; this one cannot reach that
        // branch, because it is called only where the unfiltered probe above found nothing at all
        // — there is no row occupying the key to reinstate. The same reachability argument
        // AIReviewerOrchestrationService's own copy is written on, and it holds here for the same
        // reason.
        //
        // Duplicating the round's retrieve-or-create instead was refused: it would put a branch
        // this path cannot reach into a second service, where it would then have to be maintained
        // by somebody who could not tell it was dead.
        //
        // RESOLVE ONLY. No evaluation, no transition, no command. A read that also DECIDED would
        // be a GET that publishes: for an entity at Submitted under a policy with
        // RequireApprovals = false and AutoApproveIfAllApprovalRequirementsMet = true — the shape
        // the seed writes for the personal tier — evaluating the fresh round would drive it to
        // Approved under the workflow identity, on a read nobody audited as a decision. A round
        // that should auto-approve does so when its next real fact lands, which is the event this
        // repair is standing in for.
        //
        // GATED ON THE ENTITY BEING VISIBLE. The resolver above asks the same question first and
        // refuses there, so this copy is UNREACHABLE from the only caller there is today and
        // deleting it would turn no test red. It is kept for the caller that does not exist yet:
        // this helper mints an Approval row for an entity id that came straight off a route, so
        // it defends itself rather than trusting whatever calls it next. §14.6 rule 2 makes that
        // kind of duplicate deliberate. VISIBLE rather than merely present — the probe's arms are
        // raw by-id reads and this repository has no EF global query filters, so a taken-down
        // entity — which keeps its ApprovalStatus because removal deliberately leaves the
        // approval alone (§9.7.6) — would otherwise have a round minted for a tombstone.
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
            // the collision window exactly where the approval round's repair puts it. Its
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
    }
}
