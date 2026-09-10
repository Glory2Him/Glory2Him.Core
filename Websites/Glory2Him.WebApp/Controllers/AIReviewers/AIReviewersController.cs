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
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;
using Glory2Him.Core.Models.Orchestrations.AIReviewers;
using Glory2Him.Core.Models.Orchestrations.AIReviewers.Exceptions;
using Glory2Him.Core.Services.Orchestrations.AIReviewers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RESTFulSense.Controllers;

namespace Glory2Him.WebApp.Controllers.AIReviewers
{
    /// <summary>
    /// The AI reviewer's exposure point (design §8.6.2, §12.6). Thin by construction: it
    /// authenticates through middleware, hands the request to
    /// <see cref="IAIReviewerOrchestrationService"/>, and maps that service's typed exceptions
    /// onto HTTP status codes. It carries no business logic and builds no <c>SecurityContext</c>,
    /// <c>RequestContext</c> or <c>EventEnvelope&lt;T&gt;</c> — those are created only inside the
    /// service (design §10.12).
    ///
    /// <para><b>ITS OWN RESOURCE, AND ITS OWN SERVICE.</b> These three actions began life on
    /// <c>ApprovalsController</c>, which exposes the approval ROUND — its verdict, its decision,
    /// its reset, its human invitations. Berean is a different subject, so hanging it there gave
    /// that controller a second contract and made it reach a second concern through the same
    /// service. One controller, one resource, one injected service: the shape
    /// <c>ApprovalReviewsController</c>, <c>ApprovalCommentsController</c> and
    /// <c>ApprovalSettingsController</c> already have.</para>
    ///
    /// <para><b>No action carries a role list, and that is the codebase rule rather than an
    /// omission.</b> <c>Roles = ...</c> is a <i>fixed</i> list, so it is the right coarse gate
    /// only where the admitted set is closed and enumerable. The set here is neither: §7.9 rule 2
    /// admits the whole review tier — <c>Administrators</c>, the <c>Publishers</c> tier and the
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
    /// <para>This surface is §14.7 <b>posture D</b> — it names resolved policy and reports what a
    /// round's reviewers are doing — so nothing here is <c>[AllowAnonymous]</c>.</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AIReviewersController : RESTFulController
    {
        private readonly IAIReviewerOrchestrationService aiReviewerOrchestrationService;

        public AIReviewersController(
            IAIReviewerOrchestrationService aiReviewerOrchestrationService) =>
            this.aiReviewerOrchestrationService = aiReviewerOrchestrationService;

        /// <summary>
        /// Berean's status on this round (design §8.6.2) — whether it is offered, whether it has
        /// been assigned, and how far its (not-yet-built) automated pass has gotten.
        ///
        /// <para><b>Same tier as the candidates and requests on the approval exposer</b>, not
        /// narrowed to the verdict's Publishers/Administrators: asking for Berean is coordination,
        /// exactly like asking a person (§7.9 rule 2), and this is the one read that answers
        /// whether the picker should even suggest it.</para>
        ///
        /// <para>Not SELECTs only, like the candidates read: resolving the round opens a missing
        /// one (§9.7.2 rule 1's read-triggered case), so the same dependency-validation fault is
        /// reachable here — a concurrent repair losing the race on
        /// <c>UX_Approvals_EntityType_EntityId</c> answers 400, and retrying finds the round the
        /// winner opened.</para>
        /// </summary>
        [HttpGet("{entityType}/{entityId}")]
        [Authorize]
        public async ValueTask<ActionResult<AIReviewerStatus>> GetAIReviewerAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken)
        {
            try
            {
                AIReviewerStatus aiReviewerStatus =
                    await this.aiReviewerOrchestrationService.RetrieveAIReviewerStatusAsync(
                        entityType,
                        entityId,
                        cancellationToken);

                return Ok(aiReviewerStatus);
            }
            catch (AIReviewerOrchestrationValidationException
                aiReviewerOrchestrationValidationException)
                when (aiReviewerOrchestrationValidationException.InnerException
                    is NotFoundAIReviewerOrchestrationException)
            {
                return NotFound(aiReviewerOrchestrationValidationException.InnerException);
            }
            catch (AIReviewerOrchestrationValidationException
                aiReviewerOrchestrationValidationException)
                when (aiReviewerOrchestrationValidationException.InnerException
                    is UnauthorizedAIReviewerOrchestrationException)
            {
                return Unauthorized(aiReviewerOrchestrationValidationException.InnerException);
            }
            catch (AIReviewerOrchestrationValidationException
                aiReviewerOrchestrationValidationException)
            {
                return BadRequest(aiReviewerOrchestrationValidationException.InnerException);
            }
            catch (AIReviewerOrchestrationDependencyValidationException
                aiReviewerOrchestrationDependencyValidationException)
            {
                return BadRequest(
                    aiReviewerOrchestrationDependencyValidationException.InnerException);
            }
            catch (AIReviewerOrchestrationDependencyException
                aiReviewerOrchestrationDependencyException)
            {
                return FailedDependency(aiReviewerOrchestrationDependencyException.InnerException);
            }
            catch (AIReviewerOrchestrationServiceException aiReviewerOrchestrationServiceException)
            {
                return InternalServerError(aiReviewerOrchestrationServiceException);
            }
        }

        /// <summary>
        /// Assigns Berean to this round, or asks it again once a prior assignment has completed
        /// (design §8.6.2). An UPSERT, unlike the human invitation on
        /// <c>POST api/Approvals/{entityType}/{entityId}/ReviewRequests</c>: there is no per-user
        /// dimension for a second row to key on (see <c>AIReviewerAssignment</c>'s own doc), so
        /// the same click means "create", "reset" or "no-op" depending on what the one possible
        /// row is doing — and every one of those is a <c>200</c> carrying the live row, never the
        /// <c>204</c> the human invitation answers with, because there is always something real
        /// to hand back.
        ///
        /// <para>Refused with <c>400</c> when the round is not <c>Submitted</c> or the resolved
        /// <c>IsAIReviewerOffered</c> is false — fail-closed, asked fresh on every write rather
        /// than trusted from whatever the caller's picker last showed. <c>409</c> is reserved for
        /// the one collision the orchestration's re-read cannot dissolve: the winning row
        /// withdrawn between the collision and the re-read.</para>
        /// </summary>
        [HttpPost("{entityType}/{entityId}")]
        [Authorize]
        public async ValueTask<ActionResult<AIReviewerAssignment>> PostAIReviewerAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken)
        {
            try
            {
                AIReviewerAssignment aiReviewerAssignment =
                    await this.aiReviewerOrchestrationService.RequestAIReviewerAsync(
                        entityType,
                        entityId,
                        cancellationToken);

                return Ok(aiReviewerAssignment);
            }
            catch (AIReviewerOrchestrationValidationException
                aiReviewerOrchestrationValidationException)
                when (aiReviewerOrchestrationValidationException.InnerException
                    is NotFoundAIReviewerOrchestrationException)
            {
                return NotFound(aiReviewerOrchestrationValidationException.InnerException);
            }
            catch (AIReviewerOrchestrationValidationException
                aiReviewerOrchestrationValidationException)
                when (aiReviewerOrchestrationValidationException.InnerException
                    is UnauthorizedAIReviewerOrchestrationException)
            {
                return Unauthorized(aiReviewerOrchestrationValidationException.InnerException);
            }
            catch (AIReviewerOrchestrationValidationException
                aiReviewerOrchestrationValidationException)
            {
                return BadRequest(aiReviewerOrchestrationValidationException.InnerException);
            }

            // THE COLLISION THAT OUTLIVED THE RE-READ. RequestAIReviewerAsync answers a losing
            // race by handing back the winner's row, so reaching here means the winner was
            // withdrawn in the sliver between the collision and the re-read — the caller's view
            // of this round is genuinely stale, which is what 409 says and what 400 does not.
            // Mirrors the human invitation exposer's arm for the same state.
            //
            // ORDERED ABOVE the plain dependency-validation arm below and it must stay there: the
            // two catch the same exception type and differ only by filter, so swapping them makes
            // this one unreachable and turns every 409 into a 400.
            catch (AIReviewerOrchestrationDependencyValidationException
                aiReviewerOrchestrationDependencyValidationException)
                when (aiReviewerOrchestrationDependencyValidationException.InnerException
                    is AlreadyExistsAIReviewerAssignmentException)
            {
                return Conflict(
                    aiReviewerOrchestrationDependencyValidationException.InnerException);
            }
            catch (AIReviewerOrchestrationDependencyValidationException
                aiReviewerOrchestrationDependencyValidationException)
            {
                return BadRequest(
                    aiReviewerOrchestrationDependencyValidationException.InnerException);
            }
            catch (AIReviewerOrchestrationDependencyException
                aiReviewerOrchestrationDependencyException)
            {
                return FailedDependency(aiReviewerOrchestrationDependencyException.InnerException);
            }
            catch (AIReviewerOrchestrationServiceException aiReviewerOrchestrationServiceException)
            {
                return InternalServerError(aiReviewerOrchestrationServiceException);
            }
        }

        /// <summary>
        /// Withdraws Berean's assignment (design §8.6.2). Unconditional, unlike the human
        /// withdrawal on <c>DELETE api/Approvals/{entityType}/{entityId}/ReviewRequests</c>:
        /// re-requesting now covers "ask again after completion" (see
        /// <see cref="PostAIReviewerAsync"/>), so there is no answered-invitation state left for
        /// this to refuse.
        ///
        /// <para><c>200</c> carrying the removed row, or <c>204</c> when nothing was assigned —
        /// idempotent, the same posture the human withdrawal takes for the same reason: a stale
        /// panel is not a mistake.</para>
        /// </summary>
        [HttpDelete("{entityType}/{entityId}")]
        [Authorize]
        public async ValueTask<ActionResult<AIReviewerAssignment>> DeleteAIReviewerAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken)
        {
            try
            {
                AIReviewerAssignment aiReviewerAssignment =
                    await this.aiReviewerOrchestrationService.WithdrawAIReviewerAsync(
                        entityType,
                        entityId,
                        cancellationToken);

                return aiReviewerAssignment is null ? NoContent() : Ok(aiReviewerAssignment);
            }
            catch (AIReviewerOrchestrationValidationException
                aiReviewerOrchestrationValidationException)
                when (aiReviewerOrchestrationValidationException.InnerException
                    is NotFoundAIReviewerOrchestrationException)
            {
                return NotFound(aiReviewerOrchestrationValidationException.InnerException);
            }
            catch (AIReviewerOrchestrationValidationException
                aiReviewerOrchestrationValidationException)
                when (aiReviewerOrchestrationValidationException.InnerException
                    is UnauthorizedAIReviewerOrchestrationException)
            {
                return Unauthorized(aiReviewerOrchestrationValidationException.InnerException);
            }
            catch (AIReviewerOrchestrationValidationException
                aiReviewerOrchestrationValidationException)
            {
                return BadRequest(aiReviewerOrchestrationValidationException.InnerException);
            }
            catch (AIReviewerOrchestrationDependencyValidationException
                aiReviewerOrchestrationDependencyValidationException)
            {
                return BadRequest(
                    aiReviewerOrchestrationDependencyValidationException.InnerException);
            }
            catch (AIReviewerOrchestrationDependencyException
                aiReviewerOrchestrationDependencyException)
            {
                return FailedDependency(aiReviewerOrchestrationDependencyException.InnerException);
            }
            catch (AIReviewerOrchestrationServiceException aiReviewerOrchestrationServiceException)
            {
                return InternalServerError(aiReviewerOrchestrationServiceException);
            }
        }
    }
}
