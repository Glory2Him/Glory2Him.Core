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
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.Tags;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RESTFulSense.Controllers;

namespace Glory2Him.WebApp.Controllers.Tags
{
    /// <summary>
    /// The tag exposure point (design §12.6). Thin by construction: it authenticates through
    /// middleware, hands the request to <see cref="ITagService"/>, and maps the service's typed
    /// exceptions onto HTTP status codes. It carries no business logic and builds no
    /// <c>SecurityContext</c>, <c>RequestContext</c> or <c>EventEnvelope&lt;T&gt;</c> — those are
    /// created only inside the service (design §10.12).
    ///
    /// <para>It binds to the foundation rather than an orchestration because <c>Tag</c> is
    /// approvable but Single-Row, so it needs nothing above its foundation service — the
    /// withdrawn <c>TagOrchestration</c> of the old §12.4.7 is not coming (design §12.1 rule 3,
    /// §12.3.1, §10.17 rule 3, which requires binding to the entity's <i>top-layer</i> service).</para>
    ///
    /// <para>The <c>[Authorize]</c> attributes are a <b>coarse</b> gate only (design §10.16
    /// rule 2): they establish that a caller is authenticated and, where the design names a fixed
    /// tier, that the caller holds a role in it. Every row-level rule — the contribution gate,
    /// owner-or-moderation write permission, read visibility, no self-approval — is decided by
    /// the foundation service against the stored row, which never assumes an upstream layer
    /// gated the caller (design §14.6).</para>
    ///
    /// <para>The two reads are <c>[AllowAnonymous]</c> on purpose. Tag is a §14.7 posture A
    /// entity, and rule 4 of that posture is "anonymous callers see public only" — the service
    /// implements it directly, returning a publicly visible row before it consults the security
    /// context at all and degrading the collection filter to the public predicate when the caller
    /// is unauthenticated. An <c>[Authorize]</c> here would answer 401 before the service was
    /// reached and make the entire public read surface unreachable over HTTP.</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : RESTFulController
    {
        private readonly ITagService tagService;

        public TagsController(ITagService tagService) =>
            this.tagService = tagService;

        [HttpPost]
        [Authorize]
        public async ValueTask<ActionResult<Tag>> PostTagAsync(
            [FromBody] Tag tag,
            CancellationToken cancellationToken)
        {
            try
            {
                Tag addedTag =
                    await this.tagService.AddTagAsync(tag, cancellationToken);

                return Created(addedTag);
            }
            catch (TagValidationException tagValidationException)
                when (tagValidationException.InnerException is UnauthorizedTagException)
            {
                return Unauthorized(tagValidationException.InnerException);
            }
            catch (TagValidationException tagValidationException)
            {
                return BadRequest(tagValidationException.InnerException);
            }
            catch (TagDependencyValidationException tagDependencyValidationException)
                when (tagDependencyValidationException.InnerException is AlreadyExistsTagException)
            {
                return Conflict(tagDependencyValidationException.InnerException);
            }
            catch (TagDependencyValidationException tagDependencyValidationException)
            {
                return BadRequest(tagDependencyValidationException.InnerException);
            }
            catch (TagDependencyException tagDependencyException)
            {
                return FailedDependency(tagDependencyException.InnerException);
            }
            catch (TagServiceException tagServiceException)
            {
                return InternalServerError(tagServiceException);
            }
        }

        [HttpGet]
#if !DEBUG
        [EnableQuery(PageSize = 50)]
#endif
#if DEBUG
        [EnableQuery(PageSize = 5000)]
#endif
        [AllowAnonymous]
        public async ValueTask<ActionResult<IQueryable<Tag>>> Get(CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<Tag> retrievedTags =
                    await this.tagService.RetrieveAllTagsAsync(cancellationToken);

                return Ok(retrievedTags);
            }
            catch (TagDependencyException tagDependencyException)
            {
                return FailedDependency(tagDependencyException.InnerException);
            }
            catch (TagServiceException tagServiceException)
            {
                return InternalServerError(tagServiceException);
            }
        }

        [HttpGet("{tagId}")]
        [AllowAnonymous]
        public async ValueTask<ActionResult<Tag>> GetTagByIdAsync(
            Guid tagId,
            CancellationToken cancellationToken)
        {
            try
            {
                Tag tag = await this.tagService.RetrieveTagByIdAsync(tagId, cancellationToken);

                return Ok(tag);
            }
            catch (TagValidationException tagValidationException)
                when (tagValidationException.InnerException is NotFoundTagException)
            {
                return NotFound(tagValidationException.InnerException);
            }
            catch (TagValidationException tagValidationException)
            {
                return BadRequest(tagValidationException.InnerException);
            }
            catch (TagDependencyValidationException tagDependencyValidationException)
            {
                return BadRequest(tagDependencyValidationException.InnerException);
            }
            catch (TagDependencyException tagDependencyException)
            {
                return FailedDependency(tagDependencyException.InnerException);
            }
            catch (TagServiceException tagServiceException)
            {
                return InternalServerError(tagServiceException);
            }
        }

        [HttpPut]
        [Authorize]
        public async ValueTask<ActionResult<Tag>> PutTagAsync(
            [FromBody] Tag tag,
            CancellationToken cancellationToken)
        {
            try
            {
                Tag modifiedTag =
                    await this.tagService.ModifyTagAsync(tag, cancellationToken);

                return Ok(modifiedTag);
            }
            catch (TagValidationException tagValidationException)
                when (tagValidationException.InnerException is NotFoundTagException)
            {
                return NotFound(tagValidationException.InnerException);
            }
            catch (TagValidationException tagValidationException)
                when (tagValidationException.InnerException is UnauthorizedTagException)
            {
                return Unauthorized(tagValidationException.InnerException);
            }
            catch (TagValidationException tagValidationException)
            {
                return BadRequest(tagValidationException.InnerException);
            }
            catch (TagDependencyValidationException tagDependencyValidationException)
                when (tagDependencyValidationException.InnerException is AlreadyExistsTagException)
            {
                return Conflict(tagDependencyValidationException.InnerException);
            }
            catch (TagDependencyValidationException tagDependencyValidationException)
                when (tagDependencyValidationException.InnerException is LockedTagException)
            {
                return Locked(tagDependencyValidationException.InnerException);
            }
            catch (TagDependencyValidationException tagDependencyValidationException)
            {
                return BadRequest(tagDependencyValidationException.InnerException);
            }
            catch (TagDependencyException tagDependencyException)
            {
                return FailedDependency(tagDependencyException.InnerException);
            }
            catch (TagServiceException tagServiceException)
            {
                return InternalServerError(tagServiceException);
            }
        }

        /// <summary>
        /// Soft removal (design §14.6): the row is marked deleted and keeps its audit trail.
        /// The optional reason is carried through to <c>DeletionReason</c>.
        /// </summary>
        [HttpDelete("{tagId}")]
        [Authorize]
        public async ValueTask<ActionResult<Tag>> DeleteTagByIdAsync(
            Guid tagId,
            [FromQuery] string? deletionReason,
            CancellationToken cancellationToken)
        {
            try
            {
                Tag deletedTag =
                    await this.tagService.RemoveTagByIdAsync(tagId, deletionReason, cancellationToken);

                return Ok(deletedTag);
            }
            catch (TagValidationException tagValidationException)
                when (tagValidationException.InnerException is NotFoundTagException)
            {
                return NotFound(tagValidationException.InnerException);
            }
            catch (TagValidationException tagValidationException)
                when (tagValidationException.InnerException is UnauthorizedTagException)
            {
                return Unauthorized(tagValidationException.InnerException);
            }
            catch (TagValidationException tagValidationException)
            {
                return BadRequest(tagValidationException.InnerException);
            }
            catch (TagDependencyValidationException tagDependencyValidationException)
                when (tagDependencyValidationException.InnerException is AlreadyExistsTagException)
            {
                return Conflict(tagDependencyValidationException.InnerException);
            }

            catch (TagDependencyValidationException tagDependencyValidationException)
                when (tagDependencyValidationException.InnerException is LockedTagException)
            {
                return Locked(tagDependencyValidationException.InnerException);
            }
            catch (TagDependencyValidationException tagDependencyValidationException)
            {
                return BadRequest(tagDependencyValidationException.InnerException);
            }
            catch (TagDependencyException tagDependencyException)
            {
                return FailedDependency(tagDependencyException.InnerException);
            }
            catch (TagServiceException tagServiceException)
            {
                return InternalServerError(tagServiceException);
            }
        }

        /// <summary>
        /// Permanent removal. Design §14.6 restricts hard removal to <c>Admin</c>; the attribute
        /// below is the coarse half of that and the foundation re-decides it against the row.
        /// </summary>
        [HttpDelete("{tagId}/Hard")]
        [Authorize(Roles = Roles.Admin)]
        public async ValueTask<ActionResult<Tag>> HardDeleteTagByIdAsync(
            Guid tagId,
            CancellationToken cancellationToken)
        {
            try
            {
                Tag hardDeletedTag =
                    await this.tagService.HardRemoveTagByIdAsync(tagId, cancellationToken);

                return Ok(hardDeletedTag);
            }
            catch (TagValidationException tagValidationException)
                when (tagValidationException.InnerException is NotFoundTagException)
            {
                return NotFound(tagValidationException.InnerException);
            }
            catch (TagValidationException tagValidationException)
                when (tagValidationException.InnerException is UnauthorizedTagException)
            {
                return Unauthorized(tagValidationException.InnerException);
            }
            catch (TagValidationException tagValidationException)
            {
                return BadRequest(tagValidationException.InnerException);
            }
            catch (TagDependencyValidationException tagDependencyValidationException)
                when (tagDependencyValidationException.InnerException is AlreadyExistsTagException)
            {
                return Conflict(tagDependencyValidationException.InnerException);
            }

            catch (TagDependencyValidationException tagDependencyValidationException)
                when (tagDependencyValidationException.InnerException is LockedTagException)
            {
                return Locked(tagDependencyValidationException.InnerException);
            }
            catch (TagDependencyValidationException tagDependencyValidationException)
            {
                return BadRequest(tagDependencyValidationException.InnerException);
            }
            catch (TagDependencyException tagDependencyException)
            {
                return FailedDependency(tagDependencyException.InnerException);
            }
            catch (TagServiceException tagServiceException)
            {
                return InternalServerError(tagServiceException);
            }
        }

        /// <summary>
        /// Draft → Submitted (design §9.7.1). The owner or the publisher tier may submit, and
        /// the service decides which against the stored row — the attribute only establishes
        /// that somebody is signed in.
        /// </summary>
        [HttpPost("{tagId}/Submit")]
        [Authorize]
        public async ValueTask<ActionResult<Tag>> SubmitTagByIdAsync(
            Guid tagId,
            CancellationToken cancellationToken)
        {
            try
            {
                Tag submittedTag =
                    await this.tagService.SubmitTagByIdAsync(tagId, cancellationToken);

                return Ok(submittedTag);
            }
            catch (TagValidationException tagValidationException)
                when (tagValidationException.InnerException is NotFoundTagException)
            {
                return NotFound(tagValidationException.InnerException);
            }
            catch (TagValidationException tagValidationException)
                when (tagValidationException.InnerException is UnauthorizedTagException)
            {
                return Unauthorized(tagValidationException.InnerException);
            }
            catch (TagValidationException tagValidationException)
            {
                return BadRequest(tagValidationException.InnerException);
            }
            catch (TagDependencyValidationException tagDependencyValidationException)
                when (tagDependencyValidationException.InnerException is AlreadyExistsTagException)
            {
                return Conflict(tagDependencyValidationException.InnerException);
            }

            catch (TagDependencyValidationException tagDependencyValidationException)
                when (tagDependencyValidationException.InnerException is LockedTagException)
            {
                return Locked(tagDependencyValidationException.InnerException);
            }
            catch (TagDependencyValidationException tagDependencyValidationException)
            {
                return BadRequest(tagDependencyValidationException.InnerException);
            }
            catch (TagDependencyException tagDependencyException)
            {
                return FailedDependency(tagDependencyException.InnerException);
            }
            catch (TagServiceException tagServiceException)
            {
                return InternalServerError(tagServiceException);
            }
        }

        /// <summary>
        /// Decides a submitted tag — Approved or Rejected (design §9.7.1, §8.6). The publisher
        /// tier is the coarse gate here because the design names it; the service still takes the
        /// real decision against the stored row, including the no-self-approval rule (HR-2).
        /// </summary>
        [HttpPost("Approve")]
        [Authorize(Roles = Roles.Admin + "," + Roles.Publisher + "," + Roles.TagPublisher)]
        public async ValueTask<ActionResult<Tag>> ApproveTagAsync(
            [FromBody] Tag tag,
            CancellationToken cancellationToken)
        {
            try
            {
                Tag approvedTag =
                    await this.tagService.ApproveTagAsync(tag, cancellationToken);

                return Ok(approvedTag);
            }
            catch (TagValidationException tagValidationException)
                when (tagValidationException.InnerException is NotFoundTagException)
            {
                return NotFound(tagValidationException.InnerException);
            }
            catch (TagValidationException tagValidationException)
                when (tagValidationException.InnerException is UnauthorizedTagException)
            {
                return Unauthorized(tagValidationException.InnerException);
            }
            catch (TagValidationException tagValidationException)
            {
                return BadRequest(tagValidationException.InnerException);
            }
            catch (TagDependencyValidationException tagDependencyValidationException)
                when (tagDependencyValidationException.InnerException is AlreadyExistsTagException)
            {
                return Conflict(tagDependencyValidationException.InnerException);
            }

            catch (TagDependencyValidationException tagDependencyValidationException)
                when (tagDependencyValidationException.InnerException is LockedTagException)
            {
                return Locked(tagDependencyValidationException.InnerException);
            }
            catch (TagDependencyValidationException tagDependencyValidationException)
            {
                return BadRequest(tagDependencyValidationException.InnerException);
            }
            catch (TagDependencyException tagDependencyException)
            {
                return FailedDependency(tagDependencyException.InnerException);
            }
            catch (TagServiceException tagServiceException)
            {
                return InternalServerError(tagServiceException);
            }
        }
    }
}
