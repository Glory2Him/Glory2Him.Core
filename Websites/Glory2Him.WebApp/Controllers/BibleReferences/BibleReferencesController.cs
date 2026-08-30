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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.BibleReferences.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.BibleReferences;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RESTFulSense.Controllers;

namespace Glory2Him.WebApp.Controllers.BibleReferences
{
    /// <summary>
    /// The bibleReference exposure point (design §12.6). Thin by construction: it authenticates through
    /// middleware, hands the request to <see cref="IBibleReferenceService"/>, and maps the service's typed
    /// exceptions onto HTTP status codes. It carries no business logic and builds no
    /// <c>SecurityContext</c>, <c>RequestContext</c> or <c>EventEnvelope&lt;T&gt;</c> — those are
    /// created only inside the service (design §10.12).
    ///
    /// <para>It binds to the foundation rather than an orchestration because <c>BibleReference</c> is
    /// approvable but Single-Row, so it needs nothing above its foundation service — the
    /// withdrawn <c>BibleReferenceOrchestration</c> of §12.5's withdrawn entries 4-9 is not coming (design
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
    /// <para>The two reads are <c>[AllowAnonymous]</c> on purpose. BibleReference is a §14.7 posture A
    /// entity, and rule 4 of that posture is "anonymous callers see public only" — the service
    /// implements it directly, returning a publicly visible row before it consults the security
    /// context at all and degrading the collection filter to the public predicate when the caller
    /// is unauthenticated. An <c>[Authorize]</c> here would answer 401 before the service was
    /// reached and make the entire public read surface unreachable over HTTP.</para>
    ///
    /// <para><b><c>USFM</c> is immutable, and a <c>PUT</c> that changes it is a 400 rather than
    /// a 409.</b> It is the canonical passage key — <c>JHN.3.16.NIV</c>, translation included,
    /// because Scripture is translation-specific — and the foundation pins it against the stored
    /// row on modify (design §12.3.1 rule 2a, §7.5.1 rule 4). That natural key is also why this
    /// entity is Single-Row rather than versioned: a fork would produce a second row holding the
    /// same key, and versioning it was withdrawn for exactly that reason. <c>Tag</c> and
    /// <c>Reaction</c> permit a rename today; this one does not.</para>
    ///
    /// <para><b>Unlike those two, a soft-deleted key here is genuinely released.</b>
    /// <c>UX_BibleReferences_USFM</c> carries <c>HasFilter("[IsDeleted] = 0")</c>, where
    /// <c>IX_Tags_Name</c> and <c>IX_Reactions_Name</c> do not — so #201's reserved-forever
    /// defect does not reach this exposer, and a taken-down passage can be re-created. The
    /// acceptance suite asserts it, because the two behaviours are one index filter apart and
    /// the wrong assumption is the natural one to carry over.</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BibleReferencesController : RESTFulController
    {
        private readonly IBibleReferenceService bibleReferenceService;

        public BibleReferencesController(IBibleReferenceService bibleReferenceService) =>
            this.bibleReferenceService = bibleReferenceService;

        [HttpPost]
        [Authorize]
        public async ValueTask<ActionResult<BibleReference>> PostBibleReferenceAsync(
            [FromBody] BibleReference bibleReference,
            CancellationToken cancellationToken)
        {
            try
            {
                BibleReference addedBibleReference =
                    await this.bibleReferenceService.AddBibleReferenceAsync(bibleReference, cancellationToken);

                return Created(addedBibleReference);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
                when (bibleReferenceValidationException.InnerException is UnauthorizedBibleReferenceException)
            {
                return Unauthorized(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
            {
                return BadRequest(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceDependencyValidationException bibleReferenceDependencyValidationException)
                when (bibleReferenceDependencyValidationException.InnerException is AlreadyExistsBibleReferenceException)
            {
                return Conflict(bibleReferenceDependencyValidationException.InnerException);
            }
            catch (BibleReferenceDependencyValidationException bibleReferenceDependencyValidationException)
            {
                return BadRequest(bibleReferenceDependencyValidationException.InnerException);
            }
            catch (BibleReferenceDependencyException bibleReferenceDependencyException)
            {
                return FailedDependency(bibleReferenceDependencyException.InnerException);
            }
            catch (BibleReferenceServiceException bibleReferenceServiceException)
            {
                return InternalServerError(bibleReferenceServiceException);
            }
        }

        [HttpGet]
        [EnableQuery]
        [AllowAnonymous]
        public async ValueTask<ActionResult<IQueryable<BibleReference>>> Get(CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<BibleReference> retrievedBibleReferences =
                    await this.bibleReferenceService.RetrieveAllBibleReferencesAsync(cancellationToken);

                return Ok(retrievedBibleReferences);
            }
            catch (BibleReferenceDependencyException bibleReferenceDependencyException)
            {
                return FailedDependency(bibleReferenceDependencyException.InnerException);
            }
            catch (BibleReferenceServiceException bibleReferenceServiceException)
            {
                return InternalServerError(bibleReferenceServiceException);
            }
        }

        [HttpGet("{bibleReferenceId}")]
        [AllowAnonymous]
        public async ValueTask<ActionResult<BibleReference>> GetBibleReferenceByIdAsync(
            Guid bibleReferenceId,
            CancellationToken cancellationToken)
        {
            try
            {
                BibleReference bibleReference = await this.bibleReferenceService.RetrieveBibleReferenceByIdAsync(bibleReferenceId, cancellationToken);

                return Ok(bibleReference);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
                when (bibleReferenceValidationException.InnerException is NotFoundBibleReferenceException)
            {
                return NotFound(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
            {
                return BadRequest(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceDependencyValidationException bibleReferenceDependencyValidationException)
            {
                return BadRequest(bibleReferenceDependencyValidationException.InnerException);
            }
            catch (BibleReferenceDependencyException bibleReferenceDependencyException)
            {
                return FailedDependency(bibleReferenceDependencyException.InnerException);
            }
            catch (BibleReferenceServiceException bibleReferenceServiceException)
            {
                return InternalServerError(bibleReferenceServiceException);
            }
        }

        [HttpPut]
        [Authorize]
        public async ValueTask<ActionResult<BibleReference>> PutBibleReferenceAsync(
            [FromBody] BibleReference bibleReference,
            CancellationToken cancellationToken)
        {
            try
            {
                BibleReference modifiedBibleReference =
                    await this.bibleReferenceService.ModifyBibleReferenceAsync(bibleReference, cancellationToken);

                return Ok(modifiedBibleReference);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
                when (bibleReferenceValidationException.InnerException is NotFoundBibleReferenceException)
            {
                return NotFound(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
                when (bibleReferenceValidationException.InnerException is UnauthorizedBibleReferenceException)
            {
                return Unauthorized(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
            {
                return BadRequest(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceDependencyValidationException bibleReferenceDependencyValidationException)
                when (bibleReferenceDependencyValidationException.InnerException is AlreadyExistsBibleReferenceException)
            {
                return Conflict(bibleReferenceDependencyValidationException.InnerException);
            }
            catch (BibleReferenceDependencyValidationException bibleReferenceDependencyValidationException)
                when (bibleReferenceDependencyValidationException.InnerException is LockedBibleReferenceException)
            {
                return Locked(bibleReferenceDependencyValidationException.InnerException);
            }
            catch (BibleReferenceDependencyValidationException bibleReferenceDependencyValidationException)
            {
                return BadRequest(bibleReferenceDependencyValidationException.InnerException);
            }
            catch (BibleReferenceDependencyException bibleReferenceDependencyException)
            {
                return FailedDependency(bibleReferenceDependencyException.InnerException);
            }
            catch (BibleReferenceServiceException bibleReferenceServiceException)
            {
                return InternalServerError(bibleReferenceServiceException);
            }
        }

        /// <summary>
        /// Soft removal (design §14.6): the row is marked deleted and keeps its audit trail.
        /// The optional reason is carried through to <c>DeletionReason</c>.
        /// </summary>
        [HttpDelete("{bibleReferenceId}")]
        [Authorize]
        public async ValueTask<ActionResult<BibleReference>> DeleteBibleReferenceByIdAsync(
            Guid bibleReferenceId,
            [FromQuery] string? deletionReason,
            CancellationToken cancellationToken)
        {
            try
            {
                BibleReference deletedBibleReference =
                    await this.bibleReferenceService.RemoveBibleReferenceByIdAsync(bibleReferenceId, deletionReason, cancellationToken);

                return Ok(deletedBibleReference);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
                when (bibleReferenceValidationException.InnerException is NotFoundBibleReferenceException)
            {
                return NotFound(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
                when (bibleReferenceValidationException.InnerException is UnauthorizedBibleReferenceException)
            {
                return Unauthorized(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
            {
                return BadRequest(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceDependencyValidationException bibleReferenceDependencyValidationException)
                when (bibleReferenceDependencyValidationException.InnerException is AlreadyExistsBibleReferenceException)
            {
                return Conflict(bibleReferenceDependencyValidationException.InnerException);
            }

            catch (BibleReferenceDependencyValidationException bibleReferenceDependencyValidationException)
                when (bibleReferenceDependencyValidationException.InnerException is LockedBibleReferenceException)
            {
                return Locked(bibleReferenceDependencyValidationException.InnerException);
            }
            catch (BibleReferenceDependencyValidationException bibleReferenceDependencyValidationException)
            {
                return BadRequest(bibleReferenceDependencyValidationException.InnerException);
            }
            catch (BibleReferenceDependencyException bibleReferenceDependencyException)
            {
                return FailedDependency(bibleReferenceDependencyException.InnerException);
            }
            catch (BibleReferenceServiceException bibleReferenceServiceException)
            {
                return InternalServerError(bibleReferenceServiceException);
            }
        }

        /// <summary>
        /// Permanent removal. Design §14.6 restricts hard removal to <c>Admin</c>; the attribute
        /// below is the coarse half of that and the foundation re-decides it against the row.
        /// </summary>
        [HttpDelete("{bibleReferenceId}/Hard")]
        [Authorize(Roles = Roles.Administrators)]
        public async ValueTask<ActionResult<BibleReference>> HardDeleteBibleReferenceByIdAsync(
            Guid bibleReferenceId,
            CancellationToken cancellationToken)
        {
            try
            {
                BibleReference hardDeletedBibleReference =
                    await this.bibleReferenceService.HardRemoveBibleReferenceByIdAsync(bibleReferenceId, cancellationToken);

                return Ok(hardDeletedBibleReference);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
                when (bibleReferenceValidationException.InnerException is NotFoundBibleReferenceException)
            {
                return NotFound(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
                when (bibleReferenceValidationException.InnerException is UnauthorizedBibleReferenceException)
            {
                return Unauthorized(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
            {
                return BadRequest(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceDependencyValidationException bibleReferenceDependencyValidationException)
                when (bibleReferenceDependencyValidationException.InnerException is AlreadyExistsBibleReferenceException)
            {
                return Conflict(bibleReferenceDependencyValidationException.InnerException);
            }

            catch (BibleReferenceDependencyValidationException bibleReferenceDependencyValidationException)
                when (bibleReferenceDependencyValidationException.InnerException is LockedBibleReferenceException)
            {
                return Locked(bibleReferenceDependencyValidationException.InnerException);
            }
            catch (BibleReferenceDependencyValidationException bibleReferenceDependencyValidationException)
            {
                return BadRequest(bibleReferenceDependencyValidationException.InnerException);
            }
            catch (BibleReferenceDependencyException bibleReferenceDependencyException)
            {
                return FailedDependency(bibleReferenceDependencyException.InnerException);
            }
            catch (BibleReferenceServiceException bibleReferenceServiceException)
            {
                return InternalServerError(bibleReferenceServiceException);
            }
        }

        /// <summary>
        /// Draft → Submitted (design §9.7.1). The owner or the publisher tier may submit, and
        /// the service decides which against the stored row — the attribute only establishes
        /// that somebody is signed in.
        /// </summary>
        [HttpPost("{bibleReferenceId}/Submit")]
        [Authorize]
        public async ValueTask<ActionResult<BibleReference>> SubmitBibleReferenceByIdAsync(
            Guid bibleReferenceId,
            CancellationToken cancellationToken)
        {
            try
            {
                BibleReference submittedBibleReference =
                    await this.bibleReferenceService.SubmitBibleReferenceByIdAsync(bibleReferenceId, cancellationToken);

                return Ok(submittedBibleReference);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
                when (bibleReferenceValidationException.InnerException is NotFoundBibleReferenceException)
            {
                return NotFound(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
                when (bibleReferenceValidationException.InnerException is UnauthorizedBibleReferenceException)
            {
                return Unauthorized(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
            {
                return BadRequest(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceDependencyValidationException bibleReferenceDependencyValidationException)
                when (bibleReferenceDependencyValidationException.InnerException is AlreadyExistsBibleReferenceException)
            {
                return Conflict(bibleReferenceDependencyValidationException.InnerException);
            }

            catch (BibleReferenceDependencyValidationException bibleReferenceDependencyValidationException)
                when (bibleReferenceDependencyValidationException.InnerException is LockedBibleReferenceException)
            {
                return Locked(bibleReferenceDependencyValidationException.InnerException);
            }
            catch (BibleReferenceDependencyValidationException bibleReferenceDependencyValidationException)
            {
                return BadRequest(bibleReferenceDependencyValidationException.InnerException);
            }
            catch (BibleReferenceDependencyException bibleReferenceDependencyException)
            {
                return FailedDependency(bibleReferenceDependencyException.InnerException);
            }
            catch (BibleReferenceServiceException bibleReferenceServiceException)
            {
                return InternalServerError(bibleReferenceServiceException);
            }
        }

        /// <summary>
        /// Moves a bibleReference's approval state — Approved, Rejected, or back to Submitted (design
        /// §9.7.1, §8.6). The publisher tier is the coarse gate here because the design names it;
        /// the service still takes the real decision against the stored row, including the
        /// no-self-approval rule (HR-2) and the <c>Admin</c>-only override that re-opens a
        /// terminal row (HR-4). The route keeps its name: the ordinary decision is what nearly
        /// every caller reaches it for.
        /// </summary>
        [HttpPost("Approve")]
        [Authorize(Roles = Roles.Administrators + "," + Roles.Publishers + "," + Roles.BibleReferencePublishers)]
        public async ValueTask<ActionResult<BibleReference>> TransitionBibleReferenceApprovalAsync(
            [FromBody] BibleReference bibleReference,
            CancellationToken cancellationToken)
        {
            try
            {
                BibleReference approvedBibleReference =
                    await this.bibleReferenceService.TransitionBibleReferenceApprovalAsync(bibleReference, cancellationToken);

                return Ok(approvedBibleReference);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
                when (bibleReferenceValidationException.InnerException is NotFoundBibleReferenceException)
            {
                return NotFound(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
                when (bibleReferenceValidationException.InnerException is UnauthorizedBibleReferenceException)
            {
                return Unauthorized(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceValidationException bibleReferenceValidationException)
            {
                return BadRequest(bibleReferenceValidationException.InnerException);
            }
            catch (BibleReferenceDependencyValidationException bibleReferenceDependencyValidationException)
                when (bibleReferenceDependencyValidationException.InnerException is AlreadyExistsBibleReferenceException)
            {
                return Conflict(bibleReferenceDependencyValidationException.InnerException);
            }

            catch (BibleReferenceDependencyValidationException bibleReferenceDependencyValidationException)
                when (bibleReferenceDependencyValidationException.InnerException is LockedBibleReferenceException)
            {
                return Locked(bibleReferenceDependencyValidationException.InnerException);
            }
            catch (BibleReferenceDependencyValidationException bibleReferenceDependencyValidationException)
            {
                return BadRequest(bibleReferenceDependencyValidationException.InnerException);
            }
            catch (BibleReferenceDependencyException bibleReferenceDependencyException)
            {
                return FailedDependency(bibleReferenceDependencyException.InnerException);
            }
            catch (BibleReferenceServiceException bibleReferenceServiceException)
            {
                return InternalServerError(bibleReferenceServiceException);
            }
        }
    }
}
