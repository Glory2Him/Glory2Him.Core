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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Processings.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Processings.ContentItems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RESTFulSense.Controllers;

namespace Glory2Him.WebApp.Controllers.ContentItems
{
    /// <summary>
    /// The contentItem exposure point (design §12.6). Thin by construction: it authenticates through
    /// middleware, hands the request to <see cref="IContentItemProcessingService"/>, and maps the service's typed
    /// exceptions onto HTTP status codes. It carries no business logic and builds no
    /// <c>SecurityContext</c>, <c>RequestContext</c> or <c>EventEnvelope&lt;T&gt;</c> — those are
    /// created only inside the service (design §10.12).
    ///
    /// <para><b>It binds to the PROCESSING service, and that is mandatory rather than a
    /// preference.</b> <c>ContentItem</c> is Versioned (§7.5.1), and §10.17 rule 1 makes a service
    /// above the foundation a hard prerequisite for a Versioned approvable entity — a fork must
    /// emit exactly one fact per completed amend, which a foundation cannot promise. §10.17 rule 3
    /// states the consequence for exposers directly: a write made against a foundation service
    /// bypasses approval invalidation, so an approvable entity must be exposed through its
    /// top-layer service. <c>IContentItemService</c> has more members and is the wrong
    /// dependency; binding to it would let an HTTP caller amend an item without the approval
    /// workflow ever hearing about it.</para>
    ///
    /// <para><b>All six reads are <c>[AllowAnonymous]</c>, each for its own documented reason</b>
    /// — the service interface states the posture per member and this controller does not restate
    /// it. What matters here is that two of them are NOT interchangeable: <c>Get</c> widens with
    /// the caller (owner sees their own drafts, a review role sees everything) while
    /// <c>GetPublicContentItems</c> consults no security context at all. The first is a moderation
    /// surface, the second is the public one.</para>
    ///
    /// <para><b>Submit and hard removal are absent, and it is a gap rather than a design.</b>
    /// <c>IContentItemProcessingService</c> has neither; both exist only on
    /// <c>IContentItemService</c>. The consequence is worth stating plainly: <b>a content item
    /// cannot be submitted for approval over HTTP.</b> No other route reaches it either —
    /// <c>ModifyContentItemAsync</c> treats <c>ApprovalStatus</c> as a control field (§12.4.1 rule
    /// 6) so the <c>Draft</c> ↔ <c>Submitted</c> carve-out is unavailable on this path, and
    /// <c>ApprovalOrchestrationService.ProcessEntityModifiedAsync</c> explicitly refuses to move
    /// the status because "submitting is somebody's decision to offer the content rather than a
    /// side effect of editing it". A draft created here stays a draft. Lifting
    /// <c>SubmitContentItemByIdAsync</c> onto the processing service is the fix when the approval
    /// round needs to be enterable through the API (#316).</para>
    ///
    /// <para><b>Approve is absent for a different reason, and that one IS a design.</b> §12.4.1
    /// rule 10 addresses the approval command for <c>ContentItem</c> to this service — but as an
    /// <i>event</i>, <c>OnApprovingContentItemAsync</c>, because the publication swap must demote
    /// the incumbent before promoting the new row and the filtered unique index refuses the other
    /// order. Two rows in a guaranteed order is a call stack, not a delivery. The HTTP route in
    /// already exists: <c>POST api/Approvals/{entityType}/{entityId}/Decision</c> reaches
    /// <c>ApprovalOrchestrationService.DecideApprovalAsync</c>, which publishes the command. An
    /// <c>Approve</c> endpoint here would be a second, unordered path to the same write.</para>
    ///
    /// <para><b>Routes follow <c>[Route("api/[controller]")]</c>, not §17.1's kebab-case.</b> The
    /// exposer skill's <c>contracts.json</c> requires that token and every built controller uses
    /// it; §17.1 predates the skill. <c>PUT</c> takes the model in the body with no <c>{id}</c>
    /// segment, matching its siblings. <c>api/ContentItems/Public</c> resolves to the literal
    /// route rather than <c>{contentItemId}</c> because attribute routing ranks a literal segment
    /// above a parameter — the acceptance suite exercises it rather than trusting it.</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ContentItemsController : RESTFulController
    {
        private readonly IContentItemProcessingService contentItemProcessingService;

        public ContentItemsController(IContentItemProcessingService contentItemProcessingService) =>
            this.contentItemProcessingService = contentItemProcessingService;

        [HttpPost]
        [Authorize]
        public async ValueTask<ActionResult<ContentItem>> PostContentItemAsync(
            [FromBody] ContentItem contentItem,
            CancellationToken cancellationToken)
        {
            try
            {
                ContentItem addedContentItem =
                    await this.contentItemProcessingService.AddContentItemAsync(contentItem, cancellationToken);

                return Created(addedContentItem);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
                when (contentItemProcessingValidationException.InnerException is UnauthorizedContentItemProcessingException)
            {
                return Unauthorized(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
            {
                return BadRequest(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException is AlreadyExistsContentItemProcessingException)
            {
                return Conflict(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is NotFoundContentItemException)
            {
                return NotFound(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is UnauthorizedContentItemException)
            {
                return Unauthorized(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is AlreadyExistsContentItemException)
            {
                return Conflict(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is LockedContentItemException)
            {
                return Locked(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
            {
                return BadRequest(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }

        [HttpGet]
        [EnableQuery]
        [AllowAnonymous]
        public async ValueTask<ActionResult<IQueryable<ContentItem>>> Get(CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<ContentItem> retrievedContentItems =
                    await this.contentItemProcessingService.RetrieveAllContentItemsAsync(cancellationToken);

                return Ok(retrievedContentItems);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }

        [HttpGet("{contentItemId}")]
        [AllowAnonymous]
        public async ValueTask<ActionResult<ContentItem>> GetContentItemByIdAsync(
            Guid contentItemId,
            CancellationToken cancellationToken)
        {
            try
            {
                ContentItem contentItem = await this.contentItemProcessingService.RetrieveContentItemByIdAsync(contentItemId, cancellationToken);

                return Ok(contentItem);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
                when (contentItemProcessingValidationException.InnerException is NotFoundContentItemProcessingException)
            {
                return NotFound(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
            {
                return BadRequest(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is NotFoundContentItemException)
            {
                return NotFound(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is UnauthorizedContentItemException)
            {
                return Unauthorized(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is AlreadyExistsContentItemException)
            {
                return Conflict(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is LockedContentItemException)
            {
                return Locked(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
            {
                return BadRequest(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }

        [HttpPut]
        [Authorize]
        public async ValueTask<ActionResult<ContentItem>> PutContentItemAsync(
            [FromBody] ContentItem contentItem,
            CancellationToken cancellationToken)
        {
            try
            {
                ContentItem modifiedContentItem =
                    await this.contentItemProcessingService.ModifyContentItemAsync(contentItem, cancellationToken);

                return Ok(modifiedContentItem);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
                when (contentItemProcessingValidationException.InnerException is NotFoundContentItemProcessingException)
            {
                return NotFound(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
                when (contentItemProcessingValidationException.InnerException is UnauthorizedContentItemProcessingException)
            {
                return Unauthorized(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
            {
                return BadRequest(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException is AlreadyExistsContentItemProcessingException)
            {
                return Conflict(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is NotFoundContentItemException)
            {
                return NotFound(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is UnauthorizedContentItemException)
            {
                return Unauthorized(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is AlreadyExistsContentItemException)
            {
                return Conflict(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is LockedContentItemException)
            {
                return Locked(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
            {
                return BadRequest(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }

        /// <summary>
        /// Soft removal (design §14.6): the row is marked deleted and keeps its audit trail.
        /// The optional reason is carried through to <c>DeletionReason</c>.
        /// </summary>
        [HttpDelete("{contentItemId}")]
        [Authorize]
        public async ValueTask<ActionResult<ContentItem>> DeleteContentItemByIdAsync(
            Guid contentItemId,
            [FromQuery] string? deletionReason,
            CancellationToken cancellationToken)
        {
            try
            {
                ContentItem deletedContentItem =
                    await this.contentItemProcessingService.RemoveContentItemByIdAsync(contentItemId, deletionReason, cancellationToken);

                return Ok(deletedContentItem);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
                when (contentItemProcessingValidationException.InnerException is NotFoundContentItemProcessingException)
            {
                return NotFound(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
                when (contentItemProcessingValidationException.InnerException is UnauthorizedContentItemProcessingException)
            {
                return Unauthorized(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
            {
                return BadRequest(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException is AlreadyExistsContentItemProcessingException)
            {
                return Conflict(contentItemProcessingDependencyValidationException.InnerException);
            }

            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is NotFoundContentItemException)
            {
                return NotFound(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is UnauthorizedContentItemException)
            {
                return Unauthorized(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is AlreadyExistsContentItemException)
            {
                return Conflict(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is LockedContentItemException)
            {
                return Locked(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
            {
                return BadRequest(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }

        /// <summary>
        /// Permanent removal. Design §14.6 restricts hard removal to <c>Admin</c>; the attribute
        /// below is the coarse half of that and the foundation re-decides it against the row.
        /// </summary>

        /// <summary>
        /// Exactly the canonically visible versions (§14.1: not deleted, <c>Approved</c>,
        /// <c>IsPublished</c>, and <c>PublishDate</c> null or past).
        ///
        /// <para><b>Caller-INDEPENDENT, and that is the whole reason it exists beside
        /// <see cref="Get"/>.</b> No security context is consulted, so a privileged caller
        /// receives exactly what an anonymous visitor would. <see cref="Get"/> widens with the
        /// caller — an owner also sees their own drafts, a review role sees everything — which is
        /// correct for a moderation surface and wrong for a public one. Wiring this route to that
        /// member would leak drafts to anonymous visitors and no attribute test would catch it,
        /// which is why the unit suite asserts the two call different members.</para>
        /// </summary>
        [HttpGet("Public")]
        [EnableQuery]
        [AllowAnonymous]
        public async ValueTask<ActionResult<IQueryable<ContentItem>>> GetPublicContentItems(
            CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<ContentItem> retrievedContentItems =
                    await this.contentItemProcessingService
                        .RetrieveAllPublicContentItemsAsync(cancellationToken);

                return Ok(retrievedContentItems);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }

        /// <summary>
        /// Every version of one group (§17.1 <c>/groups/{groupId}</c>), under the same per-caller
        /// filter as <see cref="Get"/>.
        /// </summary>
        [HttpGet("Groups/{groupId}")]
        [EnableQuery]
        [AllowAnonymous]
        public async ValueTask<ActionResult<IQueryable<ContentItem>>> GetContentItemsByGroupId(
            Guid groupId,
            CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<ContentItem> retrievedContentItems =
                    await this.contentItemProcessingService
                        .RetrieveContentItemsByGroupIdAsync(groupId, cancellationToken);

                return Ok(retrievedContentItems);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }

        /// <summary>
        /// The group's edit tip — the highest non-deleted <c>Version</c>, which may still be an
        /// unapproved draft. Answers not-found rather than unauthorized so an unprivileged probe
        /// cannot tell a non-public tip from a missing group (§14.5).
        /// </summary>
        [HttpGet("Groups/{groupId}/Latest")]
        [AllowAnonymous]
        public async ValueTask<ActionResult<ContentItem>> GetLatestContentItemByGroupIdAsync(
            Guid groupId,
            CancellationToken cancellationToken)
        {
            try
            {
                ContentItem contentItem = await this.contentItemProcessingService
                    .RetrieveLatestContentItemByGroupIdAsync(groupId, cancellationToken);

                return Ok(contentItem);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
                when (contentItemProcessingValidationException.InnerException
                    is NotFoundContentItemProcessingException)
            {
                return NotFound(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
            {
                return BadRequest(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is NotFoundContentItemException)
            {
                return NotFound(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is UnauthorizedContentItemException)
            {
                return Unauthorized(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is AlreadyExistsContentItemException)
            {
                return Conflict(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is LockedContentItemException)
            {
                return Locked(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
            {
                return BadRequest(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }

        /// <summary>
        /// The row the public currently reads, which stays published while a newer draft is in
        /// review (§3.4.1). A published row scheduled in the future is visible only to its owner
        /// or a review role; everyone else gets not-found, as does every caller when the group has
        /// no published row.
        /// </summary>
        [HttpGet("Groups/{groupId}/Published")]
        [AllowAnonymous]
        public async ValueTask<ActionResult<ContentItem>> GetPublishedContentItemByGroupIdAsync(
            Guid groupId,
            CancellationToken cancellationToken)
        {
            try
            {
                ContentItem contentItem = await this.contentItemProcessingService
                    .RetrievePublishedContentItemByGroupIdAsync(groupId, cancellationToken);

                return Ok(contentItem);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
                when (contentItemProcessingValidationException.InnerException
                    is NotFoundContentItemProcessingException)
            {
                return NotFound(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
            {
                return BadRequest(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is NotFoundContentItemException)
            {
                return NotFound(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is UnauthorizedContentItemException)
            {
                return Unauthorized(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is AlreadyExistsContentItemException)
            {
                return Conflict(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is LockedContentItemException)
            {
                return Locked(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
            {
                return BadRequest(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }
    }
}
