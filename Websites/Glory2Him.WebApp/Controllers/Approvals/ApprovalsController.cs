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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
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
    /// The approval-workflow exposure point (design §12.6). Thin by construction: it authenticates
    /// through middleware, hands the request to <see cref="IApprovalOrchestrationService"/>, and
    /// maps the service's typed exceptions onto HTTP status codes. It carries no business logic
    /// and builds no <c>SecurityContext</c>, <c>RequestContext</c> or <c>EventEnvelope&lt;T&gt;</c>
    /// — those are created only inside the service (design §10.12).
    ///
    /// <para>It binds to the ORCHESTRATION rather than <c>IApprovalService</c>, which is §10.17
    /// rule 3 — bind to the entity's top-layer service. Both operations here span the approval,
    /// its reviews and its comments, and the decision additionally publishes the entity sync
    /// command (§16.7.1); a caller reaching the foundation directly would get the row without any
    /// of that.</para>
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

        public ApprovalsController(IApprovalOrchestrationService approvalOrchestrationService) =>
            this.approvalOrchestrationService = approvalOrchestrationService;

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
        /// The <c>Locked</c> clause stays, because the reset writes the <c>Approval</c> row and a
        /// concurrent decision on the same round is exactly what that answers.</para>
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

        /// <summary>
        /// Who is in scope to review this entity (§16.7.4) — the review tier for it, minus the
        /// entity's own author alone. People who have already reviewed, and people already
        /// invited, are included: a picker renders them inert and under their own heading rather
        /// than hiding them, so a search for a name finds it.
        ///
        /// <para>A <b>user-enumeration surface</b>, and its posture follows from that: the
        /// orchestration admits only the requesting tier (§7.9 rule 2), and each candidate carries
        /// an account id and a display name and nothing else. No role list, no email, no account
        /// state — a moderator learns only that somebody is invitable, which they would learn
        /// anyway by inviting them.</para>
        ///
        /// <para>Bare <c>[Authorize]</c> for the same reason the verdict carries one: the admitted
        /// set is matched by SUFFIX across every entity type and content type (§18.6), so no fixed
        /// <c>Roles = ...</c> list could express it without locking out the scoped tiers and every
        /// entity type added later.</para>
        ///
        /// <para>No <c>Conflict</c> or <c>Locked</c> clause — neither state is reachable here.
        /// Like the verdict, this read is not SELECTs only: it resolves the reviewer scope, which
        /// opens a missing round (§16.7.2), so a dependency-validation fault from a concurrent
        /// repair is reachable and is answered 400 by the clause already below.</para>
        /// </summary>
        [HttpGet("{entityType}/{entityId}/ReviewerCandidates")]
        [Authorize]
        public async ValueTask<ActionResult<IReadOnlyList<ReviewerCandidate>>>
            GetReviewerCandidatesAsync(
                EntityType entityType,
                Guid entityId,
                CancellationToken cancellationToken)
        {
            try
            {
                IReadOnlyList<ReviewerCandidate> reviewerCandidates =
                    await this.approvalOrchestrationService.RetrieveReviewerCandidatesAsync(
                        entityType,
                        entityId,
                        cancellationToken);

                return Ok(reviewerCandidates);
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
        /// What everybody this round names is called (§16.7.4) — the review panel's one name
        /// resolver.
        ///
        /// <para><b>The gap it closes.</b> An <c>ApprovalReview</c> row names its reviewer by
        /// account id, and the only route that named other people was <c>/api/admin/users</c>
        /// behind the <c>Administrators</c> role. So a <c>Publisher</c> who is not an
        /// administrator — precisely the tier this panel exists for — could render their own name
        /// and nobody else's. The candidates read does not close it: it returns who is in scope
        /// for the round, so a reviewer who has since lost the role is absent from it entirely.
        /// This read applies no role filter and no disabled filter for exactly that reason.</para>
        ///
        /// <para><b>One resolver rather than a projection per read.</b> A display name hung off
        /// the review read would have answered that surface and left the next to invent its own,
        /// and three lookups are three chances to render one person under two names.</para>
        ///
        /// <para><b>Keyed on the round, like every other operation here.</b> The set it names is
        /// built from the approval's review rows — dismissed and soft-deleted included, since the
        /// panel renders those — and its outstanding invitations, and nothing else: the review
        /// tier is not read here, because a caller that supplies no ids gives a tier read nothing
        /// to admit and it would only repeat <c>ReviewerCandidates</c>, which this panel already
        /// calls for names of its own. So the tier gate composes with an entity gate instead of
        /// standing alone: a <c>Tag-Reviewer</c> can name the people a tag round involves and
        /// nobody else. The caller names no ids, which is what leaves nothing to probe with, no
        /// batch to cap, and no route parameter that is not part of the key.</para>
        ///
        /// <para>Bare <c>[Authorize]</c> and the tier decided beneath, matching the candidates
        /// read: the admitted set is suffix-matched across every entity and content type (§18.6),
        /// so no fixed <c>Roles = ...</c> list could express it. No <c>Conflict</c> or
        /// <c>Locked</c> clause — neither state is reachable. Not SELECTs only, though: like the
        /// candidates read it resolves the reviewer scope and can open a missing round
        /// (§16.7.2).</para>
        /// </summary>
        [HttpGet("{entityType}/{entityId}/ReviewerDisplayNames")]
        [Authorize]
        public async ValueTask<ActionResult<IReadOnlyList<ReviewerDisplayName>>>
            GetReviewerDisplayNamesAsync(
                EntityType entityType,
                Guid entityId,
                CancellationToken cancellationToken)
        {
            try
            {
                IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                    await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                        entityType,
                        entityId,
                        cancellationToken);

                return Ok(reviewerDisplayNames);
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
        /// Invites somebody to review this entity (§7.9).
        ///
        /// <para><b>204 on every success.</b> Rule 4 dissolves both duplicate shapes — a person
        /// already invited, and a person who has already answered — so the outcomes are "already
        /// there", "created" and "nothing to create", and a caller has no use for the
        /// difference. A UI asking twice, or asking from a panel a few seconds stale, is a
        /// harmless thing to do; turning it into an error would make every caller carry an
        /// existence check the server can make correctly and they cannot.</para>
        ///
        /// <para><c>requestedUserId</c> rides the query string rather than a body for the same
        /// reason the decision's scalars do: it is a value the operation owns outright, and a body
        /// would invite a caller to restate the entity key twice and let the two disagree.</para>
        ///
        /// <para><b>The race dissolves too.</b> Rule 4's check reads a scope taken a moment
        /// earlier, so two callers inviting the same person can both find nothing and both try to
        /// write. The index refuses the loser — one active invitation per person is the invariant
        /// — and the orchestration answers that by re-reading and returning the winner's row,
        /// because "somebody asked them half a second before you" is the same outcome as "you
        /// asked twice". The <c>409</c> below survives only for the case the re-read cannot
        /// explain: the winning row withdrawn between the collision and the second look.</para>
        /// </summary>
        [HttpPost("{entityType}/{entityId}/ReviewRequests")]
        [Authorize]
        public async ValueTask<ActionResult<ApprovalReviewRequest>> PostReviewRequestAsync(
            EntityType entityType,
            Guid entityId,
            [FromQuery][BindRequired] string requestedUserId,
            CancellationToken cancellationToken)
        {
            try
            {
                await this.approvalOrchestrationService.RequestApprovalReviewAsync(
                    entityType,
                    entityId,
                    requestedUserId,
                    cancellationToken);

                // 204 on every success, and the same 204 for all of them. The operation is a
                // presence check plus an add (7.9 rule 4), so its outcomes are "already there",
                // "created" and "already answered, nothing to create" - and a caller has no use
                // for the difference. It refreshes from the round either way, which is the only
                // source that stays right when somebody else is working the same item.
                //
                // The answered case has nothing to return at all: rule 6 retired the invitation
                // when the person answered. Ok(null) would hand that caller a 200 with a null
                // body to special-case.
                return NoContent();
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
                    is AlreadyExistsApprovalReviewRequestException)
            {
                return Conflict(approvalOrchestrationDependencyValidationException.InnerException);
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
        /// Who has been asked to review this entity and has not yet answered (§7.9). The read
        /// §7.9 was written around: it opens by saying the request rows exist so a moderation
        /// surface can show who has been asked, and until this route there was nothing to ask.
        ///
        /// <para><b>Pending only.</b> A withdrawn invitation is soft-deleted (rule 5) and an
        /// answered one is retired (rule 6), so the outstanding set is what the visibility filter
        /// leaves rather than something this route selects for.</para>
        ///
        /// <para>Same posture as the candidates read beside it, and for the same reason: these
        /// rows name people. §16.7.4 places them under §14.7 posture D, the orchestration admits
        /// only the requesting tier, and the foundation applies the posture again underneath —
        /// §14.6 rule 2 makes that duplicate deliberate.</para>
        ///
        /// <para>No <c>Conflict</c> or <c>Locked</c> clause — neither state is reachable, as
        /// with the candidates read. And not SELECTs only, as with the candidates read either:
        /// resolving the reviewer scope opens a missing round (§16.7.2).</para>
        /// </summary>
        [HttpGet("{entityType}/{entityId}/ReviewRequests")]
        [Authorize]
        public async ValueTask<ActionResult<IReadOnlyList<ApprovalReviewRequest>>>
            GetReviewRequestsAsync(
                EntityType entityType,
                Guid entityId,
                CancellationToken cancellationToken)
        {
            try
            {
                IReadOnlyList<ApprovalReviewRequest> approvalReviewRequests =
                    await this.approvalOrchestrationService.RetrieveApprovalReviewRequestsAsync(
                        entityType,
                        entityId,
                        cancellationToken);

                return Ok(approvalReviewRequests);
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
        /// Withdraws a pending invitation (§7.9 rule 5).
        ///
        /// <para><b>Keyed on the round and the person</b>, matching the POST beside it exactly, so
        /// withdrawal is that operation's undo rather than a separate addressing scheme. The old
        /// <c>DELETE /api/ApprovalReviewRequests/{id}</c> is gone with the controller that carried
        /// it: the row id it needed was only ever visible in the create's response body, and #352
        /// correctly made that a <c>204</c>, which left the route unreachable from a browser.</para>
        ///
        /// <para><b>204 on every success</b>, including nothing to withdraw. Withdrawing an
        /// invitation already withdrawn, or one a rule 6 retirement has taken, is a stale panel
        /// rather than a mistake — and the caller refreshes from the round either way. A
        /// <c>400</c> survives for the one case rule 5 genuinely refuses: an invitation that has
        /// been ANSWERED and whose row is somehow still live.</para>
        /// </summary>
        [HttpDelete("{entityType}/{entityId}/ReviewRequests")]
        [Authorize]
        public async ValueTask<ActionResult<ApprovalReviewRequest>> DeleteReviewRequestAsync(
            EntityType entityType,
            Guid entityId,
            [FromQuery][BindRequired] string requestedUserId,
            [FromQuery] string? deletionReason,
            CancellationToken cancellationToken)
        {
            try
            {
                await this.approvalOrchestrationService.WithdrawApprovalReviewRequestAsync(
                    entityType,
                    entityId,
                    requestedUserId,
                    deletionReason,
                    cancellationToken);

                return NoContent();
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
    }
}
