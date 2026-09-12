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
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Services.Orchestrations.Approvals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RESTFulSense.Controllers;

namespace Glory2Him.WebApp.Controllers.Approvals
{
    /// <summary>
    /// The approval ROUND's exposure point (design §12.6). Thin by construction: it authenticates
    /// through middleware, hands the request to <see cref="IApprovalOrchestrationService"/>, and
    /// maps that service's own typed exceptions onto HTTP status codes. It carries no business
    /// logic and builds no <c>SecurityContext</c>, <c>RequestContext</c> or
    /// <c>EventEnvelope&lt;T&gt;</c> — those are created only inside the service (design §10.12).
    ///
    /// <para>It binds to the ORCHESTRATION rather than <c>IApprovalService</c>, which is §10.17
    /// rule 3 — bind to the entity's top-layer service. Both operations here span the approval,
    /// its reviews and its comments, and the decision additionally publishes the entity sync
    /// command (§16.7.1); a caller reaching the foundation directly would get the row without any
    /// of that.</para>
    ///
    /// <para><b>ONE SERVICE, three actions.</b> The five reviewer-coordination routes —
    /// candidates, display names and the three review-request verbs — moved to
    /// <see cref="ApprovalReviewersController"/> (#523, §12.5.4 business rule 6), which binds
    /// <see cref="Core.Services.Orchestrations.ApprovalReviewers.IApprovalReviewerOrchestrationService"/>
    /// alone. Nothing here changed as part of that split beyond the actions leaving with their own
    /// service: the verdict, the decision and the reset are unaffected, and this controller now
    /// carries the one dependency the exposer rule expects.</para>
    ///
    /// <para><b>Neither action carries a role list, and that is the codebase rule rather than an
    /// omission.</b> <c>Roles = ...</c> is a <i>fixed</i> list, so it is the right coarse gate only
    /// where the admitted set is closed and enumerable — which is why <c>POST api/Tags/Approve</c>
    /// can name <c>Administrators,Publishers,Tag-Publishers</c> and why hard removal on the two sibling
    /// approval exposers can name <c>Administrators</c>. The set here is neither. §16.7.2 restricts the
    /// verdict to the <b>moderation tier</b> — <c>Administrators</c>, the <c>Publishers</c> tier and the
    /// <c>Reviewers</c> tier — and each tier is matched by SUFFIX: global <c>Publishers</c> or
    /// <c>Reviewers</c>, global <c>Administrators</c>, or any role ending <c>-Publishers</c> or
    /// <c>-Reviewers</c>, including the content-type-scoped
    /// <c>ContentItem-Testimony-Publishers</c> tier of §18.6 rule 5. The two actions do not admit
    /// the same set: a reviewer may see the verdict but may never decide (§8.6 HR-3), and
    /// refusing the decision is the orchestration's job rather than the attribute's. These routes
    /// are also generic over <c>EntityType</c>, so a fixed list would have to enumerate
    /// every entity type AND every content type, and would silently lock out every future one. The
    /// coarse attribute is therefore a bare <c>[Authorize]</c> and the orchestration takes the
    /// whole decision — the same call the review exposer makes for dismissal, for the same
    /// reason.</para>
    ///
    /// <para>The service gating as well is deliberate, not redundant: §14.6 rule 2 requires the
    /// service to decide against the stored row and never to assume an upstream layer gated the
    /// caller. Removing either half would leave the rule resting on the other.</para>
    ///
    /// <para>This surface is §14.7 <b>posture D</b> — an approval verdict names resolved policy
    /// and is never public — so nothing here is <c>[AllowAnonymous]</c>.</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ApprovalsController : RESTFulController
    {
        private readonly IApprovalOrchestrationService approvalOrchestrationService;

        public ApprovalsController(
            IApprovalOrchestrationService approvalOrchestrationService)
        {
            this.approvalOrchestrationService = approvalOrchestrationService;
        }

        /// <summary>
        /// What may happen to this entity's approval now, and everything stopping it, answered for
        /// the CURRENT caller (§16.7.2). Keyed by the entity rather than by <c>ApprovalId</c>
        /// because that is what a moderation screen holds: it is rendering an item, and the
        /// approval behind it is an implementation detail it should not have to resolve first.
        ///
        /// <para>No <c>Conflict</c> or <c>Locked</c> clause, and both sibling exposers omit them
        /// on their reads for the same reason: neither state is reachable here. This operation is
        /// NOT SELECTs only — §16.7.2 has it open a missing round, so it can insert exactly one
        /// <c>Approval</c> — and a concurrent repair losing the race on
        /// <c>UX_Approvals_EntityType_EntityId</c> surfaces as the
        /// <c>ApprovalOrchestrationDependencyValidationException</c> already caught below, which
        /// answers 400. That is the honest answer for a caller whose read collided with another's:
        /// retrying finds the round the winner opened.</para>
        ///
        /// <para>A <c>Draft</c> approval is a 200, not a 404: it exists, and answers blocked with
        /// <c>BlockedDueToDraftStatus</c>, because "not submitted yet" is a state a moderator can
        /// clear (§16.7.3). Only an unoccupied key is <c>NotFound</c>.</para>
        /// </summary>
        [HttpGet("{entityType}/{entityId}/Verdict")]
        [Authorize]
        public async ValueTask<ActionResult<ApprovalVerdict>> GetApprovalVerdictAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalVerdict approvalVerdict =
                    await this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                        entityType,
                        entityId,
                        cancellationToken);

                return Ok(approvalVerdict);
            }
            catch (ApprovalOrchestrationValidationException approvalOrchestrationValidationException)
                when (approvalOrchestrationValidationException.InnerException
                    is NotFoundApprovalOrchestrationException)
            {
                return NotFound(approvalOrchestrationValidationException.InnerException);
            }
            catch (ApprovalOrchestrationValidationException approvalOrchestrationValidationException)
                when (approvalOrchestrationValidationException.InnerException
                    is UnauthorizedApprovalOrchestrationException)
            {
                return Unauthorized(approvalOrchestrationValidationException.InnerException);
            }
            catch (ApprovalOrchestrationValidationException approvalOrchestrationValidationException)
            {
                return BadRequest(approvalOrchestrationValidationException.InnerException);
            }
            catch (ApprovalOrchestrationDependencyValidationException
                approvalOrchestrationDependencyValidationException)
            {
                return BadRequest(approvalOrchestrationDependencyValidationException.InnerException);
            }
            catch (ApprovalOrchestrationDependencyException approvalOrchestrationDependencyException)
            {
                return FailedDependency(approvalOrchestrationDependencyException.InnerException);
            }
            catch (ApprovalOrchestrationServiceException approvalOrchestrationServiceException)
            {
                return InternalServerError(approvalOrchestrationServiceException);
            }
        }

        /// <summary>
        /// Records a human's approve or reject on the <c>Approval</c> row — the source of truth
        /// (§9.8) — and requests the matching entity write as a command event (§16.7.1). The
        /// response reports the sync as REQUESTED rather than confirmed, because it travels as an
        /// event.
        ///
        /// <para>The three scalars ride the query string for the same reason <c>deletionReason</c>
        /// and <c>isResolved</c> do on the sibling exposers: they are values the operation owns
        /// outright, not a body the caller composes. A body would also invite a caller to restate
        /// the entity key twice and let the two disagree.</para>
        ///
        /// <para><b><c>decision</c> is bind-required, and that is load-bearing.</b> An absent enum
        /// binds to its zero member with a valid model state, and the zero member here is
        /// <c>Approve</c> — so without this a caller who hit the obvious URL and said nothing would
        /// APPROVE the item, write a terminal status onto the source of truth and publish a command
        /// that publishes the entity. <c>[BindRequired]</c> is binding metadata rather than a
        /// validation rule: it decides whether a request is addressed to this operation at all, in
        /// the same way <c>[FromQuery]</c> beside it decides where the value comes from. Nothing
        /// about the stored approval is judged here; that stays in the orchestration.</para>
        ///
        /// <para><c>isBypassRequested</c> needs no such guard: absent binds to <c>false</c>, which
        /// is the no-waiver request, and a waiver that was asked for but not needed records none
        /// anyway (§9.7.1 rule 3). An unexplained bypass is refused by the service before any
        /// policy is read, so a missing <c>bypassReason</c> arrives here as a 400 rather than
        /// being second-guessed at the boundary.</para>
        /// </summary>
        [HttpPost("{entityType}/{entityId}/Decision")]
        [Authorize]
        public async ValueTask<ActionResult<ApprovalOutcome>> PostApprovalDecisionAsync(
            EntityType entityType,
            Guid entityId,
            [FromQuery][BindRequired] ApprovalDecision decision,
            [FromQuery] bool isBypassRequested,
            [FromQuery] string? bypassReason,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalOutcome approvalOutcome =
                    await this.approvalOrchestrationService.DecideApprovalAsync(
                        entityType,
                        entityId,
                        decision,
                        isBypassRequested,

                        // Null is the ordinary "no reason supplied" case the orchestration
                        // defaults to and validates for, so the absent query value is passed
                        // through rather than coerced to an empty string a bypass check would
                        // then have to treat as blank all over again.
                        bypassReason!,
                        cancellationToken);

                return Ok(approvalOutcome);
            }
            catch (ApprovalOrchestrationValidationException approvalOrchestrationValidationException)
                when (approvalOrchestrationValidationException.InnerException
                    is NotFoundApprovalOrchestrationException)
            {
                return NotFound(approvalOrchestrationValidationException.InnerException);
            }
            catch (ApprovalOrchestrationValidationException approvalOrchestrationValidationException)
                when (approvalOrchestrationValidationException.InnerException
                    is UnauthorizedApprovalOrchestrationException)
            {
                return Unauthorized(approvalOrchestrationValidationException.InnerException);
            }
            catch (ApprovalOrchestrationValidationException approvalOrchestrationValidationException)
            {
                return BadRequest(approvalOrchestrationValidationException.InnerException);
            }
            catch (ApprovalOrchestrationDependencyValidationException
                approvalOrchestrationDependencyValidationException)
                when (approvalOrchestrationDependencyValidationException.InnerException
                    is AlreadyExistsApprovalException)
            {
                return Conflict(approvalOrchestrationDependencyValidationException.InnerException);
            }
            catch (ApprovalOrchestrationDependencyValidationException
                approvalOrchestrationDependencyValidationException)
                when (approvalOrchestrationDependencyValidationException.InnerException
                    is LockedApprovalException)
            {
                return Locked(approvalOrchestrationDependencyValidationException.InnerException);
            }
            catch (ApprovalOrchestrationDependencyValidationException
                approvalOrchestrationDependencyValidationException)
            {
                return BadRequest(approvalOrchestrationDependencyValidationException.InnerException);
            }
            catch (ApprovalOrchestrationDependencyException approvalOrchestrationDependencyException)
            {
                return FailedDependency(approvalOrchestrationDependencyException.InnerException);
            }
            catch (ApprovalOrchestrationServiceException approvalOrchestrationServiceException)
            {
                return InternalServerError(approvalOrchestrationServiceException);
            }
        }

        /// <summary>
        /// §8.6 HR-4's administrator override, exposed: puts a round that reached <c>Approved</c>
        /// or <c>Rejected</c> back to <c>Submitted</c> and dismisses every active review, so an
        /// outcome applied by accident can be recovered from without starting a new round.
        ///
        /// <para><b>It unpublishes.</b> The entity follows through the ordinary approval command,
        /// and a transition landing anywhere but <c>Approved</c> forces <c>IsPublished = false</c>
        /// and clears <c>PublishDate</c>. Resetting an approved item therefore takes it off the
        /// public site until it is approved again, and for a versioned item its group has nothing
        /// published meanwhile. That is the operation working, not a side effect of it — a reset
        /// that left the accident publicly visible would recover nothing.</para>
        ///
        /// <para>Takes no body and no query values. The entity key is the whole request: there is
        /// one thing a reset can do and no parameter that could vary it.</para>
        ///
        /// <para>Bare <c>[Authorize]</c> with the tier decided beneath, matching every sibling on
        /// this controller. The service admits <c>Administrators</c> alone — deciding an open
        /// round belongs to the publisher tier, undeciding a closed one is the override, and the
        /// override has one holder. A fixed <c>Roles = ...</c> list here would put the same rule
        /// in a second place, and the one that matters is the one nearest the write.</para>
        ///
        /// <para>No <c>Conflict</c> clause: the round already exists, so no insert can collide.
        /// The <c>Locked</c> clause is carried for the same reason every sibling write carries
        /// one — it answers the foundation's own lock fault on the <c>Approval</c> row, whatever
        /// raised it. It is not a claim that a concurrent decision produces one: two writers on
        /// this round race on the stored status, and the loser is refused by the decided-round
        /// check or by the outcome gate rather than by a lock.</para>
        /// </summary>
        [HttpPost("{entityType}/{entityId}/Reset")]
        [Authorize]
        public async ValueTask<ActionResult<ApprovalOutcome>> PostApprovalResetAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalOutcome approvalOutcome =
                    await this.approvalOrchestrationService.ResetApprovalAsync(
                        entityType,
                        entityId,
                        cancellationToken);

                return Ok(approvalOutcome);
            }
            catch (ApprovalOrchestrationValidationException approvalOrchestrationValidationException)
                when (approvalOrchestrationValidationException.InnerException
                    is NotFoundApprovalOrchestrationException)
            {
                return NotFound(approvalOrchestrationValidationException.InnerException);
            }
            catch (ApprovalOrchestrationValidationException approvalOrchestrationValidationException)
                when (approvalOrchestrationValidationException.InnerException
                    is UnauthorizedApprovalOrchestrationException)
            {
                return Unauthorized(approvalOrchestrationValidationException.InnerException);
            }
            catch (ApprovalOrchestrationValidationException approvalOrchestrationValidationException)
            {
                return BadRequest(approvalOrchestrationValidationException.InnerException);
            }
            catch (ApprovalOrchestrationDependencyValidationException
                approvalOrchestrationDependencyValidationException)
                when (approvalOrchestrationDependencyValidationException.InnerException
                    is LockedApprovalException)
            {
                return Locked(approvalOrchestrationDependencyValidationException.InnerException);
            }
            catch (ApprovalOrchestrationDependencyValidationException
                approvalOrchestrationDependencyValidationException)
            {
                return BadRequest(approvalOrchestrationDependencyValidationException.InnerException);
            }
            catch (ApprovalOrchestrationDependencyException approvalOrchestrationDependencyException)
            {
                return FailedDependency(approvalOrchestrationDependencyException.InnerException);
            }
            catch (ApprovalOrchestrationServiceException approvalOrchestrationServiceException)
            {
                return InternalServerError(approvalOrchestrationServiceException);
            }
        }
    }
}
