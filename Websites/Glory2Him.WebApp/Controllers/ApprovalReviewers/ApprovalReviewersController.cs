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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Orchestrations.ApprovalReviewers.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Services.Orchestrations.ApprovalReviewers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RESTFulSense.Controllers;

namespace Glory2Him.WebApp.Controllers.ApprovalReviewers
{
    /// <summary>
    /// Who takes part in an approval round's exposure point (design §12.5.4, §12.6 entry 3a).
    /// Thin by construction: it authenticates through middleware, hands the request to
    /// <see cref="IApprovalReviewerOrchestrationService"/>, and maps that service's own typed
    /// exceptions onto HTTP status codes. It carries no business logic and builds no
    /// <c>SecurityContext</c>, <c>RequestContext</c> or <c>EventEnvelope&lt;T&gt;</c> — those are
    /// created only inside the service (design §10.12).
    ///
    /// <para><b>SPLIT FROM <see cref="Glory2Him.WebApp.Controllers.Approvals.ApprovalsController"/>,
    /// NOT A NEW RESOURCE (#523, §12.5.4 business rule 6).</b> These five routes — candidates,
    /// display names and the three review-request verbs — began life there because
    /// <c>ApprovalsController</c> temporarily bound both orchestrations while
    /// <see cref="IApprovalReviewerOrchestrationService"/> and its own exception family were being
    /// built (#521). Now that they exist, each controller goes back to the one-dependency shape
    /// the exposer rule expects. The AI reviewer precedent (<c>AIReviewersController</c>) went the
    /// other way and took a brand new resource, <c>api/AIReviewers</c>, because its routes were
    /// new; these are not, and moving them would be a breaking change bought for nothing.</para>
    ///
    /// <para><b>THE ROUTE IS A LITERAL, "api/Approvals", AND NOT THE
    /// "api/[controller]" CONVENTION EVERY OTHER CONTROLLER ON THIS SITE USES.</b> The class is
    /// named <c>ApprovalReviewersController</c> — plural, like every sibling exposer
    /// (<c>ApprovalsController</c>, <c>ApprovalReviewsController</c>,
    /// <c>ApprovalCommentsController</c>, <c>AIReviewersController</c>) — so the convention would
    /// route it at <c>api/ApprovalReviewers</c>, and every one of these five URLs would move. That
    /// is the one thing this split exists to prevent: the routes are staying exactly where they
    /// already are, on the approval round's own resource, because a caller has always reached them
    /// there and nothing about what they do changed. Do not "fix" this back to the convention —
    /// that would be the regression, not a tidy-up.</para>
    ///
    /// <para><b>No action carries a role list, and that is the codebase rule rather than an
    /// omission.</b> <c>Roles = ...</c> is a <i>fixed</i> list, so it is the right coarse gate only
    /// where the admitted set is closed and enumerable. The set here is neither: §16.7.4 admits the
    /// whole review tier — <c>Administrators</c>, the <c>Publishers</c> tier and the
    /// <c>Reviewers</c> tier — each matched by SUFFIX, so any role ending <c>-Publishers</c> or
    /// <c>-Reviewers</c> qualifies too, including the content-type-scoped
    /// <c>ContentItem-Testimony-Publishers</c> tier of §18.6 rule 5. These routes are also generic
    /// over <c>EntityType</c>, so a fixed list would have to enumerate every entity type AND every
    /// content type, and would silently lock out every future one. The coarse attribute is
    /// therefore a bare <c>[Authorize]</c> and the orchestration takes the whole decision.</para>
    ///
    /// <para>The service gating as well is deliberate, not redundant: §14.6 rule 2 requires the
    /// service to decide against the stored row and never to assume an upstream layer gated the
    /// caller. Removing either half would leave the rule resting on the other.</para>
    ///
    /// <para>This surface is §14.7 <b>posture D</b> throughout — these routes name resolved policy
    /// and name people — so nothing here is <c>[AllowAnonymous]</c>.</para>
    /// </summary>
    [ApiController]
    [Route("api/Approvals")]
    public class ApprovalReviewersController : RESTFulController
    {
        private readonly IApprovalReviewerOrchestrationService
            approvalReviewerOrchestrationService;

        public ApprovalReviewersController(
            IApprovalReviewerOrchestrationService approvalReviewerOrchestrationService)
        {
            this.approvalReviewerOrchestrationService = approvalReviewerOrchestrationService;
        }

        /// <summary>
        /// Who is in scope to review this entity (§16.7.4) — the review tier for it, minus the
        /// entity's own author alone. People who have already reviewed, and people already
        /// invited, are included: a picker renders them inert and under their own heading rather
        /// than hiding them, so a search for a name finds it.
        ///
        /// <para>A <b>user-enumeration surface</b>, and its posture follows from that: the
        /// orchestration admits only the requesting tier (§7.9 rule 2), and each candidate carries
        /// an account id, a display name and a username, and nothing else. The username is part
        /// of that minimum rather than an addition to it: a display name is not unique, and a
        /// moderator choosing between two people called "John" from the name alone is guessing
        /// (18.3.1 keeps a username from ever being an email). No role list, no email, no account
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
                    await this.approvalReviewerOrchestrationService.RetrieveReviewerCandidatesAsync(
                        entityType,
                        entityId,
                        cancellationToken);

                return Ok(reviewerCandidates);
            }
            catch (ApprovalReviewerOrchestrationValidationException approvalReviewerOrchestrationValidationException)
                when (approvalReviewerOrchestrationValidationException.InnerException
                    is NotFoundApprovalReviewerOrchestrationException)
            {
                return NotFound(approvalReviewerOrchestrationValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationValidationException approvalReviewerOrchestrationValidationException)
                when (approvalReviewerOrchestrationValidationException.InnerException
                    is UnauthorizedApprovalReviewerOrchestrationException)
            {
                return Unauthorized(approvalReviewerOrchestrationValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationValidationException approvalReviewerOrchestrationValidationException)
            {
                return BadRequest(approvalReviewerOrchestrationValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationDependencyValidationException
                approvalReviewerOrchestrationDependencyValidationException)
            {
                return BadRequest(approvalReviewerOrchestrationDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationDependencyException approvalReviewerOrchestrationDependencyException)
            {
                return FailedDependency(approvalReviewerOrchestrationDependencyException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationServiceException approvalReviewerOrchestrationServiceException)
            {
                return InternalServerError(approvalReviewerOrchestrationServiceException);
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
                    await this.approvalReviewerOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                        entityType,
                        entityId,
                        cancellationToken);

                return Ok(reviewerDisplayNames);
            }
            catch (ApprovalReviewerOrchestrationValidationException approvalReviewerOrchestrationValidationException)
                when (approvalReviewerOrchestrationValidationException.InnerException
                    is NotFoundApprovalReviewerOrchestrationException)
            {
                return NotFound(approvalReviewerOrchestrationValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationValidationException approvalReviewerOrchestrationValidationException)
                when (approvalReviewerOrchestrationValidationException.InnerException
                    is UnauthorizedApprovalReviewerOrchestrationException)
            {
                return Unauthorized(approvalReviewerOrchestrationValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationValidationException approvalReviewerOrchestrationValidationException)
            {
                return BadRequest(approvalReviewerOrchestrationValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationDependencyValidationException
                approvalReviewerOrchestrationDependencyValidationException)
            {
                return BadRequest(approvalReviewerOrchestrationDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationDependencyException approvalReviewerOrchestrationDependencyException)
            {
                return FailedDependency(approvalReviewerOrchestrationDependencyException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationServiceException approvalReviewerOrchestrationServiceException)
            {
                return InternalServerError(approvalReviewerOrchestrationServiceException);
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
                await this.approvalReviewerOrchestrationService.RequestApprovalReviewAsync(
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
            catch (ApprovalReviewerOrchestrationValidationException approvalReviewerOrchestrationValidationException)
                when (approvalReviewerOrchestrationValidationException.InnerException
                    is NotFoundApprovalReviewerOrchestrationException)
            {
                return NotFound(approvalReviewerOrchestrationValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationValidationException approvalReviewerOrchestrationValidationException)
                when (approvalReviewerOrchestrationValidationException.InnerException
                    is UnauthorizedApprovalReviewerOrchestrationException)
            {
                return Unauthorized(approvalReviewerOrchestrationValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationValidationException approvalReviewerOrchestrationValidationException)
            {
                return BadRequest(approvalReviewerOrchestrationValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationDependencyValidationException
                approvalReviewerOrchestrationDependencyValidationException)
                when (approvalReviewerOrchestrationDependencyValidationException.InnerException
                    is AlreadyExistsApprovalReviewRequestException)
            {
                return Conflict(approvalReviewerOrchestrationDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationDependencyValidationException
                approvalReviewerOrchestrationDependencyValidationException)
            {
                return BadRequest(approvalReviewerOrchestrationDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationDependencyException approvalReviewerOrchestrationDependencyException)
            {
                return FailedDependency(approvalReviewerOrchestrationDependencyException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationServiceException approvalReviewerOrchestrationServiceException)
            {
                return InternalServerError(approvalReviewerOrchestrationServiceException);
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
                    await this.approvalReviewerOrchestrationService.RetrieveApprovalReviewRequestsAsync(
                        entityType,
                        entityId,
                        cancellationToken);

                return Ok(approvalReviewRequests);
            }
            catch (ApprovalReviewerOrchestrationValidationException approvalReviewerOrchestrationValidationException)
                when (approvalReviewerOrchestrationValidationException.InnerException
                    is NotFoundApprovalReviewerOrchestrationException)
            {
                return NotFound(approvalReviewerOrchestrationValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationValidationException approvalReviewerOrchestrationValidationException)
                when (approvalReviewerOrchestrationValidationException.InnerException
                    is UnauthorizedApprovalReviewerOrchestrationException)
            {
                return Unauthorized(approvalReviewerOrchestrationValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationValidationException approvalReviewerOrchestrationValidationException)
            {
                return BadRequest(approvalReviewerOrchestrationValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationDependencyValidationException
                approvalReviewerOrchestrationDependencyValidationException)
            {
                return BadRequest(approvalReviewerOrchestrationDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationDependencyException approvalReviewerOrchestrationDependencyException)
            {
                return FailedDependency(approvalReviewerOrchestrationDependencyException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationServiceException approvalReviewerOrchestrationServiceException)
            {
                return InternalServerError(approvalReviewerOrchestrationServiceException);
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
                await this.approvalReviewerOrchestrationService.WithdrawApprovalReviewRequestAsync(
                    entityType,
                    entityId,
                    requestedUserId,
                    deletionReason,
                    cancellationToken);

                return NoContent();
            }
            catch (ApprovalReviewerOrchestrationValidationException approvalReviewerOrchestrationValidationException)
                when (approvalReviewerOrchestrationValidationException.InnerException
                    is NotFoundApprovalReviewerOrchestrationException)
            {
                return NotFound(approvalReviewerOrchestrationValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationValidationException approvalReviewerOrchestrationValidationException)
                when (approvalReviewerOrchestrationValidationException.InnerException
                    is UnauthorizedApprovalReviewerOrchestrationException)
            {
                return Unauthorized(approvalReviewerOrchestrationValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationValidationException approvalReviewerOrchestrationValidationException)
            {
                return BadRequest(approvalReviewerOrchestrationValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationDependencyValidationException
                approvalReviewerOrchestrationDependencyValidationException)
            {
                return BadRequest(approvalReviewerOrchestrationDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationDependencyException approvalReviewerOrchestrationDependencyException)
            {
                return FailedDependency(approvalReviewerOrchestrationDependencyException.InnerException);
            }
            catch (ApprovalReviewerOrchestrationServiceException approvalReviewerOrchestrationServiceException)
            {
                return InternalServerError(approvalReviewerOrchestrationServiceException);
            }
        }
    }
}
