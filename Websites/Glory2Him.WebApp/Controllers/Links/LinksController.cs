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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Glory2Him.Core.Models.Processings.Links.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Processings.Links;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RESTFulSense.Controllers;

namespace Glory2Him.WebApp.Controllers.Links
{
    /// <summary>
    /// The link exposure point (design §12.6). Thin by construction: it authenticates through
    /// middleware, hands the request to <see cref="ILinkProcessingService"/>, and maps the service's typed
    /// exceptions onto HTTP status codes. It carries no business logic and builds no
    /// <c>SecurityContext</c>, <c>RequestContext</c> or <c>EventEnvelope&lt;T&gt;</c> — those are
    /// created only inside the service (design §10.12).
    ///
    /// <para><b>It binds to the PROCESSING service, and that is mandatory rather than a
    /// preference.</b> <c>Link</c> is Versioned (§7.5.1), and §10.17 rule 1 makes a service
    /// above the foundation a hard prerequisite for a Versioned approvable entity — a fork must
    /// emit exactly one fact per completed amend, which a foundation cannot promise. §10.17 rule 3
    /// states the consequence for exposers directly: a write made against a foundation service
    /// bypasses approval invalidation, so an approvable entity must be exposed through its
    /// top-layer service. <c>ILinkService</c> has more members and is the wrong
    /// dependency; binding to it would let an HTTP caller amend an item without the approval
    /// workflow ever hearing about it.</para>
    ///
    /// <para><b>All six reads are <c>[AllowAnonymous]</c>, each for its own documented reason</b>
    /// — the service interface states the posture per member and this controller does not restate
    /// it. What matters here is that two of them are NOT interchangeable: <c>Get</c> widens with
    /// the caller (owner sees their own drafts, a review role sees everything) while
    /// <c>GetPublicLinks</c> consults no security context at all. The first is a moderation
    /// surface, the second is the public one.</para>
    ///
    /// <para><b>Submit and hard removal are absent, and it is a gap rather than a design.</b>
    /// <c>ILinkProcessingService</c> has neither; both exist only on
    /// <c>ILinkService</c>. The consequence is worth stating plainly: <b>a link cannot be
    /// submitted for approval over HTTP.</b> No other route reaches it either —
    /// <c>ModifyLinkAsync</c> treats <c>ApprovalStatus</c> as a control field (§12.4.2 business
    /// rule 6) so the <c>Draft</c> ↔ <c>Submitted</c> carve-out is unavailable on this path, and
    /// <c>ApprovalOrchestrationService.ProcessEntityModifiedAsync</c> explicitly refuses to move
    /// the status because "submitting is somebody's decision to offer the content rather than a
    /// side effect of editing it". A draft created here stays a draft. Lifting
    /// <c>SubmitLinkByIdAsync</c> onto the processing service is the fix when the approval
    /// round needs to be enterable through the API (#317).</para>
    ///
    /// <para><b>Approve is absent for a different reason, and that one IS a design.</b> §12.4.1
    /// rule 10 governs both Versioned types and addresses the approval command for <c>Link</c>
    /// to this service — but as an
    /// <i>event</i>, <c>OnApprovingLinkAsync</c>, because the publication swap must demote
    /// the incumbent before promoting the new row and the filtered unique index refuses the other
    /// order. Two rows in a guaranteed order is a call stack, not a delivery. The HTTP route in
    /// already exists: <c>POST api/Approvals/{entityType}/{entityId}/Decision</c> reaches
    /// <c>ApprovalOrchestrationService.DecideApprovalAsync</c>, which publishes the command. An
    /// <c>Approve</c> endpoint here would be a second, unordered path to the same write.</para>
    ///
    /// <para><b>A Link is not a ContentItem, and three of that entity's rules are absent
    /// here</b> (§12.4.2). There is <b>no duplicate-content rule</b>: §3.4.2 is keyed on
    /// (<c>ContentType</c>, <c>ContentHash</c>) and a link carries neither, because two links to
    /// the same URL are a legitimate pair — the same article cited from two stories, under two
    /// names. There is <b>no content-type role tier</b>: §18.6 rule 5 gives the narrow tier only
    /// to <c>ContentItem</c>, so a <c>Link-Reviewers</c> covers every link there is and no per-row
    /// role question is asked. And there is <b>no <c>ContentType</c> immutability rule</b>,
    /// because there is nothing to reclassify.</para>
    ///
    /// <para>The visible consequence for this exposer is the catch ladder: there is no
    /// <c>AlreadyExistsLinkProcessingException</c>, so the 409 arm the <c>ContentItem</c> ladder
    /// carries has nothing to catch and is not written. The foundation's
    /// <c>AlreadyExistsLinkException</c> does exist and still arrives through the
    /// dependency-validation wrapper, so that arm stays — the two are not the same type and
    /// dropping both would be wrong.</para>
    ///
    /// <para><b>Routes follow <c>[Route("api/[controller]")]</c>.</b> §17.1 tables endpoints for
    /// <c>ContentItem</c> only, and §12.4.2 responsibility 7 says a link serves "the group reads
    /// whose endpoint shape §17.1 tables for <c>ContentItem</c>" — so the shape is borrowed and
    /// the naming is the skill's. The
    /// exposer skill's <c>contracts.json</c> requires that token and every built controller uses
    /// it; §17.1 predates the skill. <c>PUT</c> takes the model in the body with no <c>{id}</c>
    /// segment, matching its siblings. <c>api/Links/Public</c> resolves to the literal
    /// route rather than <c>{linkId}</c> because attribute routing ranks a literal segment
    /// above a parameter — the acceptance suite exercises it rather than trusting it.</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LinksController : RESTFulController
    {
        private readonly ILinkProcessingService linkProcessingService;

        public LinksController(ILinkProcessingService linkProcessingService) =>
            this.linkProcessingService = linkProcessingService;

        [HttpPost]
        [Authorize]
        public async ValueTask<ActionResult<Link>> PostLinkAsync(
            [FromBody] Link link,
            CancellationToken cancellationToken)
        {
            try
            {
                Link addedLink =
                    await this.linkProcessingService.AddLinkAsync(link, cancellationToken);

                return Created(addedLink);
            }
            catch (LinkProcessingValidationException linkProcessingValidationException)
                when (linkProcessingValidationException.InnerException is UnauthorizedLinkProcessingException)
            {
                return Unauthorized(linkProcessingValidationException.InnerException);
            }
            catch (LinkProcessingValidationException linkProcessingValidationException)
            {
                return BadRequest(linkProcessingValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is NotFoundLinkException)
            {
                return NotFound(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is UnauthorizedLinkException)
            {
                return Unauthorized(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is AlreadyExistsLinkException)
            {
                return Conflict(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is LockedLinkException)
            {
                return Locked(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
            {
                return BadRequest(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyException linkProcessingDependencyException)
            {
                return FailedDependency(linkProcessingDependencyException.InnerException);
            }
            catch (LinkProcessingServiceException linkProcessingServiceException)
            {
                return InternalServerError(linkProcessingServiceException);
            }
        }

        [HttpGet]
        [EnableQuery]
        [AllowAnonymous]
        public async ValueTask<ActionResult<IQueryable<Link>>> Get(CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<Link> retrievedLinks =
                    await this.linkProcessingService.RetrieveAllLinksAsync(cancellationToken);

                return Ok(retrievedLinks);
            }
            catch (LinkProcessingDependencyException linkProcessingDependencyException)
            {
                return FailedDependency(linkProcessingDependencyException.InnerException);
            }
            catch (LinkProcessingServiceException linkProcessingServiceException)
            {
                return InternalServerError(linkProcessingServiceException);
            }
        }

        [HttpGet("{linkId}")]
        [AllowAnonymous]
        public async ValueTask<ActionResult<Link>> GetLinkByIdAsync(
            Guid linkId,
            CancellationToken cancellationToken)
        {
            try
            {
                Link link = await this.linkProcessingService.RetrieveLinkByIdAsync(linkId, cancellationToken);

                return Ok(link);
            }
            catch (LinkProcessingValidationException linkProcessingValidationException)
                when (linkProcessingValidationException.InnerException is NotFoundLinkProcessingException)
            {
                return NotFound(linkProcessingValidationException.InnerException);
            }
            catch (LinkProcessingValidationException linkProcessingValidationException)
            {
                return BadRequest(linkProcessingValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is NotFoundLinkException)
            {
                return NotFound(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is UnauthorizedLinkException)
            {
                return Unauthorized(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is AlreadyExistsLinkException)
            {
                return Conflict(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is LockedLinkException)
            {
                return Locked(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
            {
                return BadRequest(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyException linkProcessingDependencyException)
            {
                return FailedDependency(linkProcessingDependencyException.InnerException);
            }
            catch (LinkProcessingServiceException linkProcessingServiceException)
            {
                return InternalServerError(linkProcessingServiceException);
            }
        }

        [HttpPut]
        [Authorize]
        public async ValueTask<ActionResult<Link>> PutLinkAsync(
            [FromBody] Link link,
            CancellationToken cancellationToken)
        {
            try
            {
                Link modifiedLink =
                    await this.linkProcessingService.ModifyLinkAsync(link, cancellationToken);

                return Ok(modifiedLink);
            }
            catch (LinkProcessingValidationException linkProcessingValidationException)
                when (linkProcessingValidationException.InnerException is NotFoundLinkProcessingException)
            {
                return NotFound(linkProcessingValidationException.InnerException);
            }
            catch (LinkProcessingValidationException linkProcessingValidationException)
                when (linkProcessingValidationException.InnerException is UnauthorizedLinkProcessingException)
            {
                return Unauthorized(linkProcessingValidationException.InnerException);
            }
            catch (LinkProcessingValidationException linkProcessingValidationException)
            {
                return BadRequest(linkProcessingValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is NotFoundLinkException)
            {
                return NotFound(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is UnauthorizedLinkException)
            {
                return Unauthorized(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is AlreadyExistsLinkException)
            {
                return Conflict(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is LockedLinkException)
            {
                return Locked(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
            {
                return BadRequest(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyException linkProcessingDependencyException)
            {
                return FailedDependency(linkProcessingDependencyException.InnerException);
            }
            catch (LinkProcessingServiceException linkProcessingServiceException)
            {
                return InternalServerError(linkProcessingServiceException);
            }
        }

        /// <summary>
        /// Soft removal (design §14.6): the row is marked deleted and keeps its audit trail.
        /// The optional reason is carried through to <c>DeletionReason</c>.
        /// </summary>
        [HttpDelete("{linkId}")]
        [Authorize]
        public async ValueTask<ActionResult<Link>> DeleteLinkByIdAsync(
            Guid linkId,
            [FromQuery] string? deletionReason,
            CancellationToken cancellationToken)
        {
            try
            {
                Link deletedLink =
                    await this.linkProcessingService.RemoveLinkByIdAsync(linkId, deletionReason, cancellationToken);

                return Ok(deletedLink);
            }
            catch (LinkProcessingValidationException linkProcessingValidationException)
                when (linkProcessingValidationException.InnerException is NotFoundLinkProcessingException)
            {
                return NotFound(linkProcessingValidationException.InnerException);
            }
            catch (LinkProcessingValidationException linkProcessingValidationException)
                when (linkProcessingValidationException.InnerException is UnauthorizedLinkProcessingException)
            {
                return Unauthorized(linkProcessingValidationException.InnerException);
            }
            catch (LinkProcessingValidationException linkProcessingValidationException)
            {
                return BadRequest(linkProcessingValidationException.InnerException);
            }

            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is NotFoundLinkException)
            {
                return NotFound(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is UnauthorizedLinkException)
            {
                return Unauthorized(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is AlreadyExistsLinkException)
            {
                return Conflict(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is LockedLinkException)
            {
                return Locked(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
            {
                return BadRequest(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyException linkProcessingDependencyException)
            {
                return FailedDependency(linkProcessingDependencyException.InnerException);
            }
            catch (LinkProcessingServiceException linkProcessingServiceException)
            {
                return InternalServerError(linkProcessingServiceException);
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
        public async ValueTask<ActionResult<IQueryable<Link>>> GetPublicLinks(
            CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<Link> retrievedLinks =
                    await this.linkProcessingService
                        .RetrieveAllPublicLinksAsync(cancellationToken);

                return Ok(retrievedLinks);
            }
            catch (LinkProcessingDependencyException linkProcessingDependencyException)
            {
                return FailedDependency(linkProcessingDependencyException.InnerException);
            }
            catch (LinkProcessingServiceException linkProcessingServiceException)
            {
                return InternalServerError(linkProcessingServiceException);
            }
        }

        /// <summary>
        /// Every version of one group (§17.1 <c>/groups/{groupId}</c>), under the same per-caller
        /// filter as <see cref="Get"/>.
        /// </summary>
        [HttpGet("Groups/{groupId}")]
        [EnableQuery]
        [AllowAnonymous]
        public async ValueTask<ActionResult<IQueryable<Link>>> GetLinksByGroupId(
            Guid groupId,
            CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<Link> retrievedLinks =
                    await this.linkProcessingService
                        .RetrieveLinksByGroupIdAsync(groupId, cancellationToken);

                return Ok(retrievedLinks);
            }
            catch (LinkProcessingDependencyException linkProcessingDependencyException)
            {
                return FailedDependency(linkProcessingDependencyException.InnerException);
            }
            catch (LinkProcessingServiceException linkProcessingServiceException)
            {
                return InternalServerError(linkProcessingServiceException);
            }
        }

        /// <summary>
        /// The group's edit tip — the highest non-deleted <c>Version</c>, which may still be an
        /// unapproved draft. Answers not-found rather than unauthorized so an unprivileged probe
        /// cannot tell a non-public tip from a missing group (§14.5).
        /// </summary>
        [HttpGet("Groups/{groupId}/Latest")]
        [AllowAnonymous]
        public async ValueTask<ActionResult<Link>> GetLatestLinkByGroupIdAsync(
            Guid groupId,
            CancellationToken cancellationToken)
        {
            try
            {
                Link link = await this.linkProcessingService
                    .RetrieveLatestLinkByGroupIdAsync(groupId, cancellationToken);

                return Ok(link);
            }
            catch (LinkProcessingValidationException linkProcessingValidationException)
                when (linkProcessingValidationException.InnerException
                    is NotFoundLinkProcessingException)
            {
                return NotFound(linkProcessingValidationException.InnerException);
            }
            catch (LinkProcessingValidationException linkProcessingValidationException)
            {
                return BadRequest(linkProcessingValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is NotFoundLinkException)
            {
                return NotFound(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is UnauthorizedLinkException)
            {
                return Unauthorized(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is AlreadyExistsLinkException)
            {
                return Conflict(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is LockedLinkException)
            {
                return Locked(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
            {
                return BadRequest(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyException linkProcessingDependencyException)
            {
                return FailedDependency(linkProcessingDependencyException.InnerException);
            }
            catch (LinkProcessingServiceException linkProcessingServiceException)
            {
                return InternalServerError(linkProcessingServiceException);
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
        public async ValueTask<ActionResult<Link>> GetPublishedLinkByGroupIdAsync(
            Guid groupId,
            CancellationToken cancellationToken)
        {
            try
            {
                Link link = await this.linkProcessingService
                    .RetrievePublishedLinkByGroupIdAsync(groupId, cancellationToken);

                return Ok(link);
            }
            catch (LinkProcessingValidationException linkProcessingValidationException)
                when (linkProcessingValidationException.InnerException
                    is NotFoundLinkProcessingException)
            {
                return NotFound(linkProcessingValidationException.InnerException);
            }
            catch (LinkProcessingValidationException linkProcessingValidationException)
            {
                return BadRequest(linkProcessingValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is NotFoundLinkException)
            {
                return NotFound(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is UnauthorizedLinkException)
            {
                return Unauthorized(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is AlreadyExistsLinkException)
            {
                return Conflict(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
                when (linkProcessingDependencyValidationException.InnerException
                    is LockedLinkException)
            {
                return Locked(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyValidationException linkProcessingDependencyValidationException)
            {
                return BadRequest(linkProcessingDependencyValidationException.InnerException);
            }
            catch (LinkProcessingDependencyException linkProcessingDependencyException)
            {
                return FailedDependency(linkProcessingDependencyException.InnerException);
            }
            catch (LinkProcessingServiceException linkProcessingServiceException)
            {
                return InternalServerError(linkProcessingServiceException);
            }
        }
    }
}
