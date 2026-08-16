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
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.ApprovalComments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RESTFulSense.Controllers;

namespace Glory2Him.WebApp.Controllers.ApprovalComments
{
    /// <summary>
    /// The approval-comment exposure point (design §12.6). Thin by construction: it authenticates
    /// through middleware, hands the request to <see cref="IApprovalCommentService"/>, and maps the
    /// service's typed exceptions onto HTTP status codes. It carries no business logic and builds
    /// no <c>SecurityContext</c>, <c>RequestContext</c> or <c>EventEnvelope&lt;T&gt;</c> — those are
    /// created only inside the service (design §10.12).
    ///
    /// <para><b>Only hard removal carries a role list, and that is not the template drifting.</b>
    /// <c>Roles = ...</c> is a <i>fixed</i> list, so it is only the right coarse gate when the
    /// admitted set has no owner branch. Every other gate here has one:</para>
    ///
    /// <list type="bullet">
    /// <item>Add consults no role at all — any authenticated caller who is not blocked.</item>
    /// <item>Both reads are the row's author <b>or</b> a review-role holder, and the collection
    /// read degrades to the caller's own comments rather than refusing.</item>
    /// <item>Modify and soft removal are the author and nobody else — not Publisher, not Admin.</item>
    /// <item>Resolve is the author <b>or</b> an <c>Admin</c>. That widening is the operation's
    /// reason to exist: an Admin cannot reach the flag through modify without also being handed
    /// the author's words (§14.7 rule 5).</item>
    /// </list>
    ///
    /// <para>A role list on any of those would lock the legitimate author out of their own comment,
    /// so they take a bare <c>[Authorize]</c> and the foundation decides the rest against the
    /// stored row (design §14.6). Unlike Tag, this entity is §14.7 <b>posture D</b> — anonymous
    /// callers see an empty set rather than public rows — so the reads are gated, not
    /// <c>[AllowAnonymous]</c>.</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ApprovalCommentsController : RESTFulController
    {
        private readonly IApprovalCommentService approvalCommentService;

        public ApprovalCommentsController(IApprovalCommentService approvalCommentService) =>
            this.approvalCommentService = approvalCommentService;

        [HttpPost]
        [Authorize]
        public async ValueTask<ActionResult<ApprovalComment>> PostApprovalCommentAsync(
            [FromBody] ApprovalComment approvalComment,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalComment addedApprovalComment =
                    await this.approvalCommentService.AddApprovalCommentAsync(
                        approvalComment,
                        cancellationToken);

                return Created(addedApprovalComment);
            }
            catch (ApprovalCommentValidationException approvalCommentValidationException)
                when (approvalCommentValidationException.InnerException
                    is UnauthorizedApprovalCommentException)
            {
                return Unauthorized(approvalCommentValidationException.InnerException);
            }
            catch (ApprovalCommentValidationException approvalCommentValidationException)
            {
                return BadRequest(approvalCommentValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyValidationException approvalCommentDependencyValidationException)
                when (approvalCommentDependencyValidationException.InnerException
                    is AlreadyExistsApprovalCommentException)
            {
                return Conflict(approvalCommentDependencyValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyValidationException approvalCommentDependencyValidationException)
                when (approvalCommentDependencyValidationException.InnerException
                    is LockedApprovalCommentException)
            {
                return Locked(approvalCommentDependencyValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyValidationException approvalCommentDependencyValidationException)
            {
                return BadRequest(approvalCommentDependencyValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyException approvalCommentDependencyException)
            {
                return FailedDependency(approvalCommentDependencyException.InnerException);
            }
            catch (ApprovalCommentServiceException approvalCommentServiceException)
            {
                return InternalServerError(approvalCommentServiceException);
            }
        }

        [HttpGet]
#if !DEBUG
        [EnableQuery(PageSize = 50)]
#endif
#if DEBUG
        [EnableQuery(PageSize = 5000)]
#endif
        [Authorize]
        public async ValueTask<ActionResult<IQueryable<ApprovalComment>>> Get(
            CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<ApprovalComment> retrievedApprovalComments =
                    await this.approvalCommentService.RetrieveAllApprovalCommentsAsync(
                        cancellationToken);

                return Ok(retrievedApprovalComments);
            }
            catch (ApprovalCommentDependencyException approvalCommentDependencyException)
            {
                return FailedDependency(approvalCommentDependencyException.InnerException);
            }
            catch (ApprovalCommentServiceException approvalCommentServiceException)
            {
                return InternalServerError(approvalCommentServiceException);
            }
        }

        [HttpGet("{approvalCommentId}")]
        [Authorize]
        public async ValueTask<ActionResult<ApprovalComment>> GetApprovalCommentByIdAsync(
            Guid approvalCommentId,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalComment approvalComment =
                    await this.approvalCommentService.RetrieveApprovalCommentByIdAsync(
                        approvalCommentId,
                        cancellationToken);

                return Ok(approvalComment);
            }
            catch (ApprovalCommentValidationException approvalCommentValidationException)
                when (approvalCommentValidationException.InnerException
                    is NotFoundApprovalCommentException)
            {
                return NotFound(approvalCommentValidationException.InnerException);
            }
            catch (ApprovalCommentValidationException approvalCommentValidationException)
                when (approvalCommentValidationException.InnerException
                    is UnauthorizedApprovalCommentException)
            {
                return Unauthorized(approvalCommentValidationException.InnerException);
            }
            catch (ApprovalCommentValidationException approvalCommentValidationException)
            {
                return BadRequest(approvalCommentValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyValidationException approvalCommentDependencyValidationException)
            {
                return BadRequest(approvalCommentDependencyValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyException approvalCommentDependencyException)
            {
                return FailedDependency(approvalCommentDependencyException.InnerException);
            }
            catch (ApprovalCommentServiceException approvalCommentServiceException)
            {
                return InternalServerError(approvalCommentServiceException);
            }
        }

        [HttpPut]
        [Authorize]
        public async ValueTask<ActionResult<ApprovalComment>> PutApprovalCommentAsync(
            [FromBody] ApprovalComment approvalComment,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalComment modifiedApprovalComment =
                    await this.approvalCommentService.ModifyApprovalCommentAsync(
                        approvalComment,
                        cancellationToken);

                return Ok(modifiedApprovalComment);
            }
            catch (ApprovalCommentValidationException approvalCommentValidationException)
                when (approvalCommentValidationException.InnerException
                    is NotFoundApprovalCommentException)
            {
                return NotFound(approvalCommentValidationException.InnerException);
            }
            catch (ApprovalCommentValidationException approvalCommentValidationException)
                when (approvalCommentValidationException.InnerException
                    is UnauthorizedApprovalCommentException)
            {
                return Unauthorized(approvalCommentValidationException.InnerException);
            }
            catch (ApprovalCommentValidationException approvalCommentValidationException)
            {
                return BadRequest(approvalCommentValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyValidationException approvalCommentDependencyValidationException)
                when (approvalCommentDependencyValidationException.InnerException
                    is AlreadyExistsApprovalCommentException)
            {
                return Conflict(approvalCommentDependencyValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyValidationException approvalCommentDependencyValidationException)
                when (approvalCommentDependencyValidationException.InnerException
                    is LockedApprovalCommentException)
            {
                return Locked(approvalCommentDependencyValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyValidationException approvalCommentDependencyValidationException)
            {
                return BadRequest(approvalCommentDependencyValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyException approvalCommentDependencyException)
            {
                return FailedDependency(approvalCommentDependencyException.InnerException);
            }
            catch (ApprovalCommentServiceException approvalCommentServiceException)
            {
                return InternalServerError(approvalCommentServiceException);
            }
        }

        /// <summary>
        /// Soft removal. The reason rides the query string for the same reason it does on tags:
        /// it is a scalar the operation owns outright, not a body the caller composes.
        /// </summary>
        [HttpDelete("{approvalCommentId}")]
        [Authorize]
        public async ValueTask<ActionResult<ApprovalComment>> DeleteApprovalCommentByIdAsync(
            Guid approvalCommentId,
            [FromQuery] string? deletionReason,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalComment deletedApprovalComment =
                    await this.approvalCommentService.RemoveApprovalCommentByIdAsync(
                        approvalCommentId,
                        deletionReason,
                        cancellationToken);

                return Ok(deletedApprovalComment);
            }
            catch (ApprovalCommentValidationException approvalCommentValidationException)
                when (approvalCommentValidationException.InnerException
                    is NotFoundApprovalCommentException)
            {
                return NotFound(approvalCommentValidationException.InnerException);
            }
            catch (ApprovalCommentValidationException approvalCommentValidationException)
                when (approvalCommentValidationException.InnerException
                    is UnauthorizedApprovalCommentException)
            {
                return Unauthorized(approvalCommentValidationException.InnerException);
            }
            catch (ApprovalCommentValidationException approvalCommentValidationException)
            {
                return BadRequest(approvalCommentValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyValidationException approvalCommentDependencyValidationException)
                when (approvalCommentDependencyValidationException.InnerException
                    is AlreadyExistsApprovalCommentException)
            {
                return Conflict(approvalCommentDependencyValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyValidationException approvalCommentDependencyValidationException)
                when (approvalCommentDependencyValidationException.InnerException
                    is LockedApprovalCommentException)
            {
                return Locked(approvalCommentDependencyValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyValidationException approvalCommentDependencyValidationException)
            {
                return BadRequest(approvalCommentDependencyValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyException approvalCommentDependencyException)
            {
                return FailedDependency(approvalCommentDependencyException.InnerException);
            }
            catch (ApprovalCommentServiceException approvalCommentServiceException)
            {
                return InternalServerError(approvalCommentServiceException);
            }
        }

        /// <summary>
        /// Permanent removal, and the one gate here with no owner branch: the foundation requires
        /// the global <c>Admin</c> role and admits neither the author nor any review role, so the
        /// set is closed and enumerable and belongs in the attribute.
        /// </summary>
        [HttpDelete("{approvalCommentId}/Hard")]
        [Authorize(Roles = Roles.Admin)]
        public async ValueTask<ActionResult<ApprovalComment>> HardDeleteApprovalCommentByIdAsync(
            Guid approvalCommentId,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalComment hardDeletedApprovalComment =
                    await this.approvalCommentService.HardRemoveApprovalCommentByIdAsync(
                        approvalCommentId,
                        cancellationToken);

                return Ok(hardDeletedApprovalComment);
            }
            catch (ApprovalCommentValidationException approvalCommentValidationException)
                when (approvalCommentValidationException.InnerException
                    is NotFoundApprovalCommentException)
            {
                return NotFound(approvalCommentValidationException.InnerException);
            }
            catch (ApprovalCommentValidationException approvalCommentValidationException)
                when (approvalCommentValidationException.InnerException
                    is UnauthorizedApprovalCommentException)
            {
                return Unauthorized(approvalCommentValidationException.InnerException);
            }
            catch (ApprovalCommentValidationException approvalCommentValidationException)
            {
                return BadRequest(approvalCommentValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyValidationException approvalCommentDependencyValidationException)
                when (approvalCommentDependencyValidationException.InnerException
                    is AlreadyExistsApprovalCommentException)
            {
                return Conflict(approvalCommentDependencyValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyValidationException approvalCommentDependencyValidationException)
                when (approvalCommentDependencyValidationException.InnerException
                    is LockedApprovalCommentException)
            {
                return Locked(approvalCommentDependencyValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyValidationException approvalCommentDependencyValidationException)
            {
                return BadRequest(approvalCommentDependencyValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyException approvalCommentDependencyException)
            {
                return FailedDependency(approvalCommentDependencyException.InnerException);
            }
            catch (ApprovalCommentServiceException approvalCommentServiceException)
            {
                return InternalServerError(approvalCommentServiceException);
            }
        }

        /// <summary>
        /// Records whether the comment is settled, and owns <c>IsResolved</c> and nothing else.
        /// The flag rides the query string because it is a scalar the transition owns, not a body
        /// the caller composes — the same reason <c>deletionReason</c> does on the soft delete.
        /// Unsettling uses this same route: the operation is symmetric by design, so a prematurely
        /// settled comment can block again.
        /// </summary>
        [HttpPost("{approvalCommentId}/Resolve")]
        [Authorize]
        public async ValueTask<ActionResult<ApprovalComment>> ResolveApprovalCommentAsync(
            Guid approvalCommentId,
            [FromQuery] bool isResolved,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalComment resolvedApprovalComment =
                    await this.approvalCommentService.ResolveApprovalCommentAsync(
                        approvalCommentId,
                        isResolved,
                        cancellationToken);

                return Ok(resolvedApprovalComment);
            }
            catch (ApprovalCommentValidationException approvalCommentValidationException)
                when (approvalCommentValidationException.InnerException
                    is NotFoundApprovalCommentException)
            {
                return NotFound(approvalCommentValidationException.InnerException);
            }
            catch (ApprovalCommentValidationException approvalCommentValidationException)
                when (approvalCommentValidationException.InnerException
                    is UnauthorizedApprovalCommentException)
            {
                return Unauthorized(approvalCommentValidationException.InnerException);
            }
            catch (ApprovalCommentValidationException approvalCommentValidationException)
            {
                return BadRequest(approvalCommentValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyValidationException approvalCommentDependencyValidationException)
                when (approvalCommentDependencyValidationException.InnerException
                    is AlreadyExistsApprovalCommentException)
            {
                return Conflict(approvalCommentDependencyValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyValidationException approvalCommentDependencyValidationException)
                when (approvalCommentDependencyValidationException.InnerException
                    is LockedApprovalCommentException)
            {
                return Locked(approvalCommentDependencyValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyValidationException approvalCommentDependencyValidationException)
            {
                return BadRequest(approvalCommentDependencyValidationException.InnerException);
            }
            catch (ApprovalCommentDependencyException approvalCommentDependencyException)
            {
                return FailedDependency(approvalCommentDependencyException.InnerException);
            }
            catch (ApprovalCommentServiceException approvalCommentServiceException)
            {
                return InternalServerError(approvalCommentServiceException);
            }
        }
    }
}
