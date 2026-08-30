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
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.Comments.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.Comments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RESTFulSense.Controllers;

namespace Glory2Him.WebApp.Controllers.Comments
{
    /// <summary>
    /// The comment exposure point (design §12.6). Thin by construction: it authenticates through
    /// middleware, hands the request to <see cref="ICommentService"/>, and maps the service's typed
    /// exceptions onto HTTP status codes. It carries no business logic and builds no
    /// <c>SecurityContext</c>, <c>RequestContext</c> or <c>EventEnvelope&lt;T&gt;</c> — those are
    /// created only inside the service (design §10.12).
    ///
    /// <para>It binds to the foundation rather than an orchestration because <c>Comment</c> is
    /// approvable but Single-Row, so it needs nothing above its foundation service — the
    /// withdrawn <c>CommentOrchestration</c> of the old §12.5 entry 5 is not coming (design
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
    /// <para>The two reads are <c>[AllowAnonymous]</c> on purpose. Comment is a §14.7 posture A
    /// entity, and rule 4 of that posture is "anonymous callers see public only" — the service
    /// implements it directly, returning a publicly visible row before it consults the security
    /// context at all and degrading the collection filter to the public predicate when the caller
    /// is unauthenticated. An <c>[Authorize]</c> here would answer 401 before the service was
    /// reached and make the entire public read surface unreachable over HTTP.</para>
    ///
    /// <para><b>Not to be confused with <c>ApprovalComment</c>.</b>
    /// <c>ApprovalCommentsController</c> exposes a different entity — a note on an approval
    /// record, §14.7 posture D, never public. This one is §5.3 discussion attached to content
    /// through associations, posture A, publicly readable once approved. The event broker
    /// composes fact names as subject plus operation, so the two publish <c>Comment-*</c> and
    /// <c>ApprovalComment-*</c> respectively and must not be conflated.</para>
    ///
    /// <para><b>This entity has no natural key</b>, which is why the 409 arm below is reachable
    /// in principle and unprovokable in practice. <c>Tag</c>, <c>Reaction</c> and
    /// <c>BibleReference</c> each carry a unique index that a duplicate violates;
    /// <c>StorageBroker.Comment.Configurations</c> declares no index at all, and <c>Content</c>
    /// is required but uncapped. The arm stays because <c>AlreadyExistsCommentException</c>
    /// exists and the service is entitled to throw it — an exposer that dropped the catch would
    /// turn a typed dependency-validation error into a 500 the day something did. The unit suite
    /// covers it by mocking; the acceptance suite deliberately does not, because the real stack
    /// has no rule to break.</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : RESTFulController
    {
        private readonly ICommentService commentService;

        public CommentsController(ICommentService commentService) =>
            this.commentService = commentService;

        [HttpPost]
        [Authorize]
        public async ValueTask<ActionResult<Comment>> PostCommentAsync(
            [FromBody] Comment comment,
            CancellationToken cancellationToken)
        {
            try
            {
                Comment addedComment =
                    await this.commentService.AddCommentAsync(comment, cancellationToken);

                return Created(addedComment);
            }
            catch (CommentValidationException commentValidationException)
                when (commentValidationException.InnerException is UnauthorizedCommentException)
            {
                return Unauthorized(commentValidationException.InnerException);
            }
            catch (CommentValidationException commentValidationException)
            {
                return BadRequest(commentValidationException.InnerException);
            }
            catch (CommentDependencyValidationException commentDependencyValidationException)
                when (commentDependencyValidationException.InnerException is AlreadyExistsCommentException)
            {
                return Conflict(commentDependencyValidationException.InnerException);
            }
            catch (CommentDependencyValidationException commentDependencyValidationException)
            {
                return BadRequest(commentDependencyValidationException.InnerException);
            }
            catch (CommentDependencyException commentDependencyException)
            {
                return FailedDependency(commentDependencyException.InnerException);
            }
            catch (CommentServiceException commentServiceException)
            {
                return InternalServerError(commentServiceException);
            }
        }

        [HttpGet]
        [EnableQuery]
        [AllowAnonymous]
        public async ValueTask<ActionResult<IQueryable<Comment>>> Get(CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<Comment> retrievedComments =
                    await this.commentService.RetrieveAllCommentsAsync(cancellationToken);

                return Ok(retrievedComments);
            }
            catch (CommentDependencyException commentDependencyException)
            {
                return FailedDependency(commentDependencyException.InnerException);
            }
            catch (CommentServiceException commentServiceException)
            {
                return InternalServerError(commentServiceException);
            }
        }

        [HttpGet("{commentId}")]
        [AllowAnonymous]
        public async ValueTask<ActionResult<Comment>> GetCommentByIdAsync(
            Guid commentId,
            CancellationToken cancellationToken)
        {
            try
            {
                Comment comment = await this.commentService.RetrieveCommentByIdAsync(commentId, cancellationToken);

                return Ok(comment);
            }
            catch (CommentValidationException commentValidationException)
                when (commentValidationException.InnerException is NotFoundCommentException)
            {
                return NotFound(commentValidationException.InnerException);
            }
            catch (CommentValidationException commentValidationException)
            {
                return BadRequest(commentValidationException.InnerException);
            }
            catch (CommentDependencyValidationException commentDependencyValidationException)
            {
                return BadRequest(commentDependencyValidationException.InnerException);
            }
            catch (CommentDependencyException commentDependencyException)
            {
                return FailedDependency(commentDependencyException.InnerException);
            }
            catch (CommentServiceException commentServiceException)
            {
                return InternalServerError(commentServiceException);
            }
        }

        [HttpPut]
        [Authorize]
        public async ValueTask<ActionResult<Comment>> PutCommentAsync(
            [FromBody] Comment comment,
            CancellationToken cancellationToken)
        {
            try
            {
                Comment modifiedComment =
                    await this.commentService.ModifyCommentAsync(comment, cancellationToken);

                return Ok(modifiedComment);
            }
            catch (CommentValidationException commentValidationException)
                when (commentValidationException.InnerException is NotFoundCommentException)
            {
                return NotFound(commentValidationException.InnerException);
            }
            catch (CommentValidationException commentValidationException)
                when (commentValidationException.InnerException is UnauthorizedCommentException)
            {
                return Unauthorized(commentValidationException.InnerException);
            }
            catch (CommentValidationException commentValidationException)
            {
                return BadRequest(commentValidationException.InnerException);
            }
            catch (CommentDependencyValidationException commentDependencyValidationException)
                when (commentDependencyValidationException.InnerException is AlreadyExistsCommentException)
            {
                return Conflict(commentDependencyValidationException.InnerException);
            }
            catch (CommentDependencyValidationException commentDependencyValidationException)
                when (commentDependencyValidationException.InnerException is LockedCommentException)
            {
                return Locked(commentDependencyValidationException.InnerException);
            }
            catch (CommentDependencyValidationException commentDependencyValidationException)
            {
                return BadRequest(commentDependencyValidationException.InnerException);
            }
            catch (CommentDependencyException commentDependencyException)
            {
                return FailedDependency(commentDependencyException.InnerException);
            }
            catch (CommentServiceException commentServiceException)
            {
                return InternalServerError(commentServiceException);
            }
        }

        /// <summary>
        /// Soft removal (design §14.6): the row is marked deleted and keeps its audit trail.
        /// The optional reason is carried through to <c>DeletionReason</c>.
        /// </summary>
        [HttpDelete("{commentId}")]
        [Authorize]
        public async ValueTask<ActionResult<Comment>> DeleteCommentByIdAsync(
            Guid commentId,
            [FromQuery] string? deletionReason,
            CancellationToken cancellationToken)
        {
            try
            {
                Comment deletedComment =
                    await this.commentService.RemoveCommentByIdAsync(commentId, deletionReason, cancellationToken);

                return Ok(deletedComment);
            }
            catch (CommentValidationException commentValidationException)
                when (commentValidationException.InnerException is NotFoundCommentException)
            {
                return NotFound(commentValidationException.InnerException);
            }
            catch (CommentValidationException commentValidationException)
                when (commentValidationException.InnerException is UnauthorizedCommentException)
            {
                return Unauthorized(commentValidationException.InnerException);
            }
            catch (CommentValidationException commentValidationException)
            {
                return BadRequest(commentValidationException.InnerException);
            }
            catch (CommentDependencyValidationException commentDependencyValidationException)
                when (commentDependencyValidationException.InnerException is AlreadyExistsCommentException)
            {
                return Conflict(commentDependencyValidationException.InnerException);
            }

            catch (CommentDependencyValidationException commentDependencyValidationException)
                when (commentDependencyValidationException.InnerException is LockedCommentException)
            {
                return Locked(commentDependencyValidationException.InnerException);
            }
            catch (CommentDependencyValidationException commentDependencyValidationException)
            {
                return BadRequest(commentDependencyValidationException.InnerException);
            }
            catch (CommentDependencyException commentDependencyException)
            {
                return FailedDependency(commentDependencyException.InnerException);
            }
            catch (CommentServiceException commentServiceException)
            {
                return InternalServerError(commentServiceException);
            }
        }

        /// <summary>
        /// Permanent removal. Design §14.6 restricts hard removal to <c>Administrators</c>; the attribute
        /// below is the coarse half of that and the foundation re-decides it against the row.
        /// </summary>
        [HttpDelete("{commentId}/Hard")]
        [Authorize(Roles = Roles.Administrators)]
        public async ValueTask<ActionResult<Comment>> HardDeleteCommentByIdAsync(
            Guid commentId,
            CancellationToken cancellationToken)
        {
            try
            {
                Comment hardDeletedComment =
                    await this.commentService.HardRemoveCommentByIdAsync(commentId, cancellationToken);

                return Ok(hardDeletedComment);
            }
            catch (CommentValidationException commentValidationException)
                when (commentValidationException.InnerException is NotFoundCommentException)
            {
                return NotFound(commentValidationException.InnerException);
            }
            catch (CommentValidationException commentValidationException)
                when (commentValidationException.InnerException is UnauthorizedCommentException)
            {
                return Unauthorized(commentValidationException.InnerException);
            }
            catch (CommentValidationException commentValidationException)
            {
                return BadRequest(commentValidationException.InnerException);
            }
            catch (CommentDependencyValidationException commentDependencyValidationException)
                when (commentDependencyValidationException.InnerException is AlreadyExistsCommentException)
            {
                return Conflict(commentDependencyValidationException.InnerException);
            }

            catch (CommentDependencyValidationException commentDependencyValidationException)
                when (commentDependencyValidationException.InnerException is LockedCommentException)
            {
                return Locked(commentDependencyValidationException.InnerException);
            }
            catch (CommentDependencyValidationException commentDependencyValidationException)
            {
                return BadRequest(commentDependencyValidationException.InnerException);
            }
            catch (CommentDependencyException commentDependencyException)
            {
                return FailedDependency(commentDependencyException.InnerException);
            }
            catch (CommentServiceException commentServiceException)
            {
                return InternalServerError(commentServiceException);
            }
        }

        /// <summary>
        /// Draft → Submitted (design §9.7.1). The owner or the publisher tier may submit, and
        /// the service decides which against the stored row — the attribute only establishes
        /// that somebody is signed in.
        /// </summary>
        [HttpPost("{commentId}/Submit")]
        [Authorize]
        public async ValueTask<ActionResult<Comment>> SubmitCommentByIdAsync(
            Guid commentId,
            CancellationToken cancellationToken)
        {
            try
            {
                Comment submittedComment =
                    await this.commentService.SubmitCommentByIdAsync(commentId, cancellationToken);

                return Ok(submittedComment);
            }
            catch (CommentValidationException commentValidationException)
                when (commentValidationException.InnerException is NotFoundCommentException)
            {
                return NotFound(commentValidationException.InnerException);
            }
            catch (CommentValidationException commentValidationException)
                when (commentValidationException.InnerException is UnauthorizedCommentException)
            {
                return Unauthorized(commentValidationException.InnerException);
            }
            catch (CommentValidationException commentValidationException)
            {
                return BadRequest(commentValidationException.InnerException);
            }
            catch (CommentDependencyValidationException commentDependencyValidationException)
                when (commentDependencyValidationException.InnerException is AlreadyExistsCommentException)
            {
                return Conflict(commentDependencyValidationException.InnerException);
            }

            catch (CommentDependencyValidationException commentDependencyValidationException)
                when (commentDependencyValidationException.InnerException is LockedCommentException)
            {
                return Locked(commentDependencyValidationException.InnerException);
            }
            catch (CommentDependencyValidationException commentDependencyValidationException)
            {
                return BadRequest(commentDependencyValidationException.InnerException);
            }
            catch (CommentDependencyException commentDependencyException)
            {
                return FailedDependency(commentDependencyException.InnerException);
            }
            catch (CommentServiceException commentServiceException)
            {
                return InternalServerError(commentServiceException);
            }
        }

        /// <summary>
        /// Moves a comment's approval state — Approved, Rejected, or back to Submitted (design
        /// §9.7.1, §8.6). The publisher tier is the coarse gate here because the design names it;
        /// the service still takes the real decision against the stored row, including the
        /// no-self-approval rule (HR-2) and the <c>Administrators</c>-only override that re-opens a
        /// terminal row (HR-4). The route keeps its name: the ordinary decision is what nearly
        /// every caller reaches it for.
        /// </summary>
        [HttpPost("Approve")]
        [Authorize(Roles = Roles.Administrators + "," + Roles.Publishers + "," + Roles.CommentPublishers)]
        public async ValueTask<ActionResult<Comment>> TransitionCommentApprovalAsync(
            [FromBody] Comment comment,
            CancellationToken cancellationToken)
        {
            try
            {
                Comment approvedComment =
                    await this.commentService.TransitionCommentApprovalAsync(comment, cancellationToken);

                return Ok(approvedComment);
            }
            catch (CommentValidationException commentValidationException)
                when (commentValidationException.InnerException is NotFoundCommentException)
            {
                return NotFound(commentValidationException.InnerException);
            }
            catch (CommentValidationException commentValidationException)
                when (commentValidationException.InnerException is UnauthorizedCommentException)
            {
                return Unauthorized(commentValidationException.InnerException);
            }
            catch (CommentValidationException commentValidationException)
            {
                return BadRequest(commentValidationException.InnerException);
            }
            catch (CommentDependencyValidationException commentDependencyValidationException)
                when (commentDependencyValidationException.InnerException is AlreadyExistsCommentException)
            {
                return Conflict(commentDependencyValidationException.InnerException);
            }

            catch (CommentDependencyValidationException commentDependencyValidationException)
                when (commentDependencyValidationException.InnerException is LockedCommentException)
            {
                return Locked(commentDependencyValidationException.InnerException);
            }
            catch (CommentDependencyValidationException commentDependencyValidationException)
            {
                return BadRequest(commentDependencyValidationException.InnerException);
            }
            catch (CommentDependencyException commentDependencyException)
            {
                return FailedDependency(commentDependencyException.InnerException);
            }
            catch (CommentServiceException commentServiceException)
            {
                return InternalServerError(commentServiceException);
            }
        }
    }
}
