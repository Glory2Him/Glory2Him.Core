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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.Reactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RESTFulSense.Controllers;

namespace Glory2Him.WebApp.Controllers.Reactions
{
    /// <summary>
    /// The reaction exposure point (design §12.6). Thin by construction: it authenticates through
    /// middleware, hands the request to <see cref="IReactionService"/>, and maps the service's typed
    /// exceptions onto HTTP status codes. It carries no business logic and builds no
    /// <c>SecurityContext</c>, <c>RequestContext</c> or <c>EventEnvelope&lt;T&gt;</c> — those are
    /// created only inside the service (design §10.12).
    ///
    /// <para>It binds to the foundation rather than an orchestration because <c>Reaction</c> is
    /// approvable but Single-Row, so it needs nothing above its foundation service — the
    /// withdrawn <c>ReactionOrchestration</c> of the old §12.5 entry 5 is not coming (design
    /// §12.1 rule 3, §12.3.1, §10.17 rule 3, which requires binding to the entity's
    /// <i>top-layer</i> service).</para>
    ///
    /// <para>The <c>[Authorize]</c> attributes are a <b>coarse</b> gate only (design §10.16
    /// rule 2): they establish that a caller is authenticated and, where the design names a fixed
    /// tier, that the caller holds a role in it. Every row-level rule — the contribution gate,
    /// owner-or-moderation write permission, read visibility, no self-approval — is decided by
    /// the foundation service against the stored row, which never assumes an upstream layer
    /// gated the caller (design §14.6).</para>
    ///
    /// <para>The two reads are <c>[AllowAnonymous]</c> on purpose. Reaction is a §14.7 posture A
    /// entity, and rule 4 of that posture is "anonymous callers see public only" — the service
    /// implements it directly, returning a publicly visible row before it consults the security
    /// context at all and degrading the collection filter to the public predicate when the caller
    /// is unauthenticated. An <c>[Authorize]</c> here would answer 401 before the service was
    /// reached and make the entire public read surface unreachable over HTTP.</para>
    ///
    /// <para><b>A 409 from this controller may name a row the caller cannot see.</b>
    /// <c>Reaction.Name</c> is not pinned against storage on modify — a rename is permitted —
    /// and <c>IX_Reactions_Name</c> is unique but carries no <c>IsDeleted</c> term, unlike
    /// <c>UX_BibleReferences_USFM</c> (design §12.3.1 rule 2a). So a soft-deleted reaction holds
    /// its name permanently, and an administrator who removes "Amen" and re-creates it is
    /// refused with nothing to explain why. That is #201's subject and is fixed there with a
    /// filtered index, not worked around here: translating the conflict would mean this exposer
    /// deciding something about storage, which is not its job (§10.12). The acceptance suite
    /// pins the behaviour so the fix cannot land unnoticed.</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ReactionsController : RESTFulController
    {
        private readonly IReactionService reactionService;

        public ReactionsController(IReactionService reactionService) =>
            this.reactionService = reactionService;

        [HttpPost]
        [Authorize]
        public async ValueTask<ActionResult<Reaction>> PostReactionAsync(
            [FromBody] Reaction reaction,
            CancellationToken cancellationToken)
        {
            try
            {
                Reaction addedReaction =
                    await this.reactionService.AddReactionAsync(reaction, cancellationToken);

                return Created(addedReaction);
            }
            catch (ReactionValidationException reactionValidationException)
                when (reactionValidationException.InnerException is UnauthorizedReactionException)
            {
                return Unauthorized(reactionValidationException.InnerException);
            }
            catch (ReactionValidationException reactionValidationException)
            {
                return BadRequest(reactionValidationException.InnerException);
            }
            catch (ReactionDependencyValidationException reactionDependencyValidationException)
                when (reactionDependencyValidationException.InnerException is AlreadyExistsReactionException)
            {
                return Conflict(reactionDependencyValidationException.InnerException);
            }
            catch (ReactionDependencyValidationException reactionDependencyValidationException)
            {
                return BadRequest(reactionDependencyValidationException.InnerException);
            }
            catch (ReactionDependencyException reactionDependencyException)
            {
                return FailedDependency(reactionDependencyException.InnerException);
            }
            catch (ReactionServiceException reactionServiceException)
            {
                return InternalServerError(reactionServiceException);
            }
        }

        [HttpGet]
        [EnableQuery]
        [AllowAnonymous]
        public async ValueTask<ActionResult<IQueryable<Reaction>>> Get(CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<Reaction> retrievedReactions =
                    await this.reactionService.RetrieveAllReactionsAsync(cancellationToken);

                return Ok(retrievedReactions);
            }
            catch (ReactionDependencyException reactionDependencyException)
            {
                return FailedDependency(reactionDependencyException.InnerException);
            }
            catch (ReactionServiceException reactionServiceException)
            {
                return InternalServerError(reactionServiceException);
            }
        }

        [HttpGet("{reactionId}")]
        [AllowAnonymous]
        public async ValueTask<ActionResult<Reaction>> GetReactionByIdAsync(
            Guid reactionId,
            CancellationToken cancellationToken)
        {
            try
            {
                Reaction reaction = await this.reactionService.RetrieveReactionByIdAsync(reactionId, cancellationToken);

                return Ok(reaction);
            }
            catch (ReactionValidationException reactionValidationException)
                when (reactionValidationException.InnerException is NotFoundReactionException)
            {
                return NotFound(reactionValidationException.InnerException);
            }
            catch (ReactionValidationException reactionValidationException)
            {
                return BadRequest(reactionValidationException.InnerException);
            }
            catch (ReactionDependencyValidationException reactionDependencyValidationException)
            {
                return BadRequest(reactionDependencyValidationException.InnerException);
            }
            catch (ReactionDependencyException reactionDependencyException)
            {
                return FailedDependency(reactionDependencyException.InnerException);
            }
            catch (ReactionServiceException reactionServiceException)
            {
                return InternalServerError(reactionServiceException);
            }
        }

        [HttpPut]
        [Authorize]
        public async ValueTask<ActionResult<Reaction>> PutReactionAsync(
            [FromBody] Reaction reaction,
            CancellationToken cancellationToken)
        {
            try
            {
                Reaction modifiedReaction =
                    await this.reactionService.ModifyReactionAsync(reaction, cancellationToken);

                return Ok(modifiedReaction);
            }
            catch (ReactionValidationException reactionValidationException)
                when (reactionValidationException.InnerException is NotFoundReactionException)
            {
                return NotFound(reactionValidationException.InnerException);
            }
            catch (ReactionValidationException reactionValidationException)
                when (reactionValidationException.InnerException is UnauthorizedReactionException)
            {
                return Unauthorized(reactionValidationException.InnerException);
            }
            catch (ReactionValidationException reactionValidationException)
            {
                return BadRequest(reactionValidationException.InnerException);
            }
            catch (ReactionDependencyValidationException reactionDependencyValidationException)
                when (reactionDependencyValidationException.InnerException is AlreadyExistsReactionException)
            {
                return Conflict(reactionDependencyValidationException.InnerException);
            }
            catch (ReactionDependencyValidationException reactionDependencyValidationException)
                when (reactionDependencyValidationException.InnerException is LockedReactionException)
            {
                return Locked(reactionDependencyValidationException.InnerException);
            }
            catch (ReactionDependencyValidationException reactionDependencyValidationException)
            {
                return BadRequest(reactionDependencyValidationException.InnerException);
            }
            catch (ReactionDependencyException reactionDependencyException)
            {
                return FailedDependency(reactionDependencyException.InnerException);
            }
            catch (ReactionServiceException reactionServiceException)
            {
                return InternalServerError(reactionServiceException);
            }
        }

        /// <summary>
        /// Soft removal (design §14.6): the row is marked deleted and keeps its audit trail.
        /// The optional reason is carried through to <c>DeletionReason</c>.
        /// </summary>
        [HttpDelete("{reactionId}")]
        [Authorize]
        public async ValueTask<ActionResult<Reaction>> DeleteReactionByIdAsync(
            Guid reactionId,
            [FromQuery] string? deletionReason,
            CancellationToken cancellationToken)
        {
            try
            {
                Reaction deletedReaction =
                    await this.reactionService.RemoveReactionByIdAsync(reactionId, deletionReason, cancellationToken);

                return Ok(deletedReaction);
            }
            catch (ReactionValidationException reactionValidationException)
                when (reactionValidationException.InnerException is NotFoundReactionException)
            {
                return NotFound(reactionValidationException.InnerException);
            }
            catch (ReactionValidationException reactionValidationException)
                when (reactionValidationException.InnerException is UnauthorizedReactionException)
            {
                return Unauthorized(reactionValidationException.InnerException);
            }
            catch (ReactionValidationException reactionValidationException)
            {
                return BadRequest(reactionValidationException.InnerException);
            }
            catch (ReactionDependencyValidationException reactionDependencyValidationException)
                when (reactionDependencyValidationException.InnerException is AlreadyExistsReactionException)
            {
                return Conflict(reactionDependencyValidationException.InnerException);
            }

            catch (ReactionDependencyValidationException reactionDependencyValidationException)
                when (reactionDependencyValidationException.InnerException is LockedReactionException)
            {
                return Locked(reactionDependencyValidationException.InnerException);
            }
            catch (ReactionDependencyValidationException reactionDependencyValidationException)
            {
                return BadRequest(reactionDependencyValidationException.InnerException);
            }
            catch (ReactionDependencyException reactionDependencyException)
            {
                return FailedDependency(reactionDependencyException.InnerException);
            }
            catch (ReactionServiceException reactionServiceException)
            {
                return InternalServerError(reactionServiceException);
            }
        }

        /// <summary>
        /// Permanent removal. Design §14.6 restricts hard removal to <c>Admin</c>; the attribute
        /// below is the coarse half of that and the foundation re-decides it against the row.
        /// </summary>
        [HttpDelete("{reactionId}/Hard")]
        [Authorize(Roles = Roles.Admin)]
        public async ValueTask<ActionResult<Reaction>> HardDeleteReactionByIdAsync(
            Guid reactionId,
            CancellationToken cancellationToken)
        {
            try
            {
                Reaction hardDeletedReaction =
                    await this.reactionService.HardRemoveReactionByIdAsync(reactionId, cancellationToken);

                return Ok(hardDeletedReaction);
            }
            catch (ReactionValidationException reactionValidationException)
                when (reactionValidationException.InnerException is NotFoundReactionException)
            {
                return NotFound(reactionValidationException.InnerException);
            }
            catch (ReactionValidationException reactionValidationException)
                when (reactionValidationException.InnerException is UnauthorizedReactionException)
            {
                return Unauthorized(reactionValidationException.InnerException);
            }
            catch (ReactionValidationException reactionValidationException)
            {
                return BadRequest(reactionValidationException.InnerException);
            }
            catch (ReactionDependencyValidationException reactionDependencyValidationException)
                when (reactionDependencyValidationException.InnerException is AlreadyExistsReactionException)
            {
                return Conflict(reactionDependencyValidationException.InnerException);
            }

            catch (ReactionDependencyValidationException reactionDependencyValidationException)
                when (reactionDependencyValidationException.InnerException is LockedReactionException)
            {
                return Locked(reactionDependencyValidationException.InnerException);
            }
            catch (ReactionDependencyValidationException reactionDependencyValidationException)
            {
                return BadRequest(reactionDependencyValidationException.InnerException);
            }
            catch (ReactionDependencyException reactionDependencyException)
            {
                return FailedDependency(reactionDependencyException.InnerException);
            }
            catch (ReactionServiceException reactionServiceException)
            {
                return InternalServerError(reactionServiceException);
            }
        }

        /// <summary>
        /// Draft → Submitted (design §9.7.1). The owner or the publisher tier may submit, and
        /// the service decides which against the stored row — the attribute only establishes
        /// that somebody is signed in.
        /// </summary>
        [HttpPost("{reactionId}/Submit")]
        [Authorize]
        public async ValueTask<ActionResult<Reaction>> SubmitReactionByIdAsync(
            Guid reactionId,
            CancellationToken cancellationToken)
        {
            try
            {
                Reaction submittedReaction =
                    await this.reactionService.SubmitReactionByIdAsync(reactionId, cancellationToken);

                return Ok(submittedReaction);
            }
            catch (ReactionValidationException reactionValidationException)
                when (reactionValidationException.InnerException is NotFoundReactionException)
            {
                return NotFound(reactionValidationException.InnerException);
            }
            catch (ReactionValidationException reactionValidationException)
                when (reactionValidationException.InnerException is UnauthorizedReactionException)
            {
                return Unauthorized(reactionValidationException.InnerException);
            }
            catch (ReactionValidationException reactionValidationException)
            {
                return BadRequest(reactionValidationException.InnerException);
            }
            catch (ReactionDependencyValidationException reactionDependencyValidationException)
                when (reactionDependencyValidationException.InnerException is AlreadyExistsReactionException)
            {
                return Conflict(reactionDependencyValidationException.InnerException);
            }

            catch (ReactionDependencyValidationException reactionDependencyValidationException)
                when (reactionDependencyValidationException.InnerException is LockedReactionException)
            {
                return Locked(reactionDependencyValidationException.InnerException);
            }
            catch (ReactionDependencyValidationException reactionDependencyValidationException)
            {
                return BadRequest(reactionDependencyValidationException.InnerException);
            }
            catch (ReactionDependencyException reactionDependencyException)
            {
                return FailedDependency(reactionDependencyException.InnerException);
            }
            catch (ReactionServiceException reactionServiceException)
            {
                return InternalServerError(reactionServiceException);
            }
        }

        /// <summary>
        /// Moves a reaction's approval state — Approved, Rejected, or back to Submitted (design
        /// §9.7.1, §8.6). The publisher tier is the coarse gate here because the design names it;
        /// the service still takes the real decision against the stored row, including the
        /// no-self-approval rule (HR-2) and the <c>Admin</c>-only override that re-opens a
        /// terminal row (HR-4). The route keeps its name: the ordinary decision is what nearly
        /// every caller reaches it for.
        /// </summary>
        [HttpPost("Approve")]
        [Authorize(Roles = Roles.Admin + "," + Roles.Publisher + "," + Roles.ReactionPublisher)]
        public async ValueTask<ActionResult<Reaction>> TransitionReactionApprovalAsync(
            [FromBody] Reaction reaction,
            CancellationToken cancellationToken)
        {
            try
            {
                Reaction approvedReaction =
                    await this.reactionService.TransitionReactionApprovalAsync(reaction, cancellationToken);

                return Ok(approvedReaction);
            }
            catch (ReactionValidationException reactionValidationException)
                when (reactionValidationException.InnerException is NotFoundReactionException)
            {
                return NotFound(reactionValidationException.InnerException);
            }
            catch (ReactionValidationException reactionValidationException)
                when (reactionValidationException.InnerException is UnauthorizedReactionException)
            {
                return Unauthorized(reactionValidationException.InnerException);
            }
            catch (ReactionValidationException reactionValidationException)
            {
                return BadRequest(reactionValidationException.InnerException);
            }
            catch (ReactionDependencyValidationException reactionDependencyValidationException)
                when (reactionDependencyValidationException.InnerException is AlreadyExistsReactionException)
            {
                return Conflict(reactionDependencyValidationException.InnerException);
            }

            catch (ReactionDependencyValidationException reactionDependencyValidationException)
                when (reactionDependencyValidationException.InnerException is LockedReactionException)
            {
                return Locked(reactionDependencyValidationException.InnerException);
            }
            catch (ReactionDependencyValidationException reactionDependencyValidationException)
            {
                return BadRequest(reactionDependencyValidationException.InnerException);
            }
            catch (ReactionDependencyException reactionDependencyException)
            {
                return FailedDependency(reactionDependencyException.InnerException);
            }
            catch (ReactionServiceException reactionServiceException)
            {
                return InternalServerError(reactionServiceException);
            }
        }
    }
}
