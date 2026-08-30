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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.ContentItemSettings.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.ContentItemSettings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RESTFulSense.Controllers;

namespace Glory2Him.WebApp.Controllers.ContentItemSettings
{
    /// <summary>
    /// The contentItemSetting exposure point (design §12.6). Thin by construction: it authenticates through
    /// middleware, hands the request to <see cref="IContentItemSettingService"/>, and maps the service's typed
    /// exceptions onto HTTP status codes. It carries no business logic and builds no
    /// <c>SecurityContext</c>, <c>RequestContext</c> or <c>EventEnvelope&lt;T&gt;</c> — those are
    /// created only inside the service (design §10.12).
    ///
    /// <para><b>It binds to the foundation today, and will rebind.</b> #209 is open to build a
    /// <c>ContentItemSettingsProcessingService</c>, earning its layer by effective-setting
    /// resolution — merging the content type default with any item-level override (§6.10,
    /// §12.5.2 responsibility 5), which reads two rows of one entity and composes them into one
    /// answer. That service does not exist, and the six CRUD members below do not need it:
    /// resolution is an additional read, not a different owner for add, modify or remove. When
    /// #209 lands this controller rebinds and gains the resolution endpoint — an accepted cost,
    /// recorded here so the rebinding is expected rather than discovered.</para>
    ///
    /// <para><c>ContentItemSetting</c> is not versioned, so §10.17's fork argument does not apply
    /// and there is no approval-invalidation hazard in binding one layer down meanwhile.</para>
    ///
    /// <para><b>Posture C (§14.7) on the writes.</b> All writes including hard removal are
    /// <c>Administrators</c> only — there is no owner branch, because only administrators author
    /// configuration — so the role list is expressible on the attribute rather than deferred to
    /// the service. The service still re-decides it against the stored row (§14.6).</para>
    ///
    /// <para><b>The reads are <c>[AllowAnonymous]</c>, and this is where the posture splits.</b>
    /// <c>ApprovalSettingsController</c> shares posture C and gates its reads on
    /// <c>[Authorize]</c>; this one must not. Effective settings drive rendering for anonymous
    /// visitors — whether a page shows its tags, reactions or comments is decided by these rows
    /// — so <c>ContentItemSetting</c> is public-read under posture C rule 2 while
    /// <c>ApprovalSetting</c> is authenticated-read.
    ///
    /// Two entities, one posture, opposite read gates. Getting it backwards would either leak
    /// policy or render every anonymous page without its settings, and neither failure is loud,
    /// which is why the security suite asserts both directions rather than only the permissive
    /// one.</para>
    ///
    /// <para><b>No approval verbs.</b> §7.5 entry 9 lists <c>ContentItemSetting</c> as approvable
    /// <i>"if policy changes require approval"</i> — a conditional never taken up. The entity
    /// carries no <c>ApprovalStatus</c> or bypass pair, and the service has no submit and no
    /// approval transition, so this exposer has six endpoints rather than eight.</para>
    ///
    /// <para><b>Two unique indexes make 409 a live response.</b>
    /// <c>UX_ContentItemSettings_DefaultPerType</c> allows one default per content type
    /// (<c>ContentItemId IS NULL</c>) and <c>UX_ContentItemSettings_OverridePerEntity</c> allows
    /// one override per content item. §6.10's resolution depends on at most one row per scope,
    /// so these are the rule rather than a storage detail (§12.5.2 business rules 3 and 4).</para>
    ///
    /// <para><b>Both delete verbs refuse a default, and answer 400.</b> Every content type must
    /// always have a live default (§12.5.2 business rule 5), so the service refuses to remove a row
    /// whose <c>ContentItemId</c> is null — soft and hard alike, the invariant being about the row
    /// existing rather than about how it goes away. It surfaces as a validation error naming the
    /// rule rather than a 404: the row is there and every caller may read it. Overrides stay
    /// freely removable.</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ContentItemSettingsController : RESTFulController
    {
        private readonly IContentItemSettingService contentItemSettingService;

        public ContentItemSettingsController(IContentItemSettingService contentItemSettingService) =>
            this.contentItemSettingService = contentItemSettingService;

        [HttpPost]
        [Authorize(Roles = Roles.Administrators)]
        public async ValueTask<ActionResult<ContentItemSetting>> PostContentItemSettingAsync(
            [FromBody] ContentItemSetting contentItemSetting,
            CancellationToken cancellationToken)
        {
            try
            {
                ContentItemSetting addedContentItemSetting =
                    await this.contentItemSettingService.AddContentItemSettingAsync(contentItemSetting, cancellationToken);

                return Created(addedContentItemSetting);
            }
            catch (ContentItemSettingValidationException contentItemSettingValidationException)
                when (contentItemSettingValidationException.InnerException is UnauthorizedContentItemSettingException)
            {
                return Unauthorized(contentItemSettingValidationException.InnerException);
            }
            catch (ContentItemSettingValidationException contentItemSettingValidationException)
            {
                return BadRequest(contentItemSettingValidationException.InnerException);
            }
            catch (ContentItemSettingDependencyValidationException contentItemSettingDependencyValidationException)
                when (contentItemSettingDependencyValidationException.InnerException is AlreadyExistsContentItemSettingException)
            {
                return Conflict(contentItemSettingDependencyValidationException.InnerException);
            }
            catch (ContentItemSettingDependencyValidationException contentItemSettingDependencyValidationException)
            {
                return BadRequest(contentItemSettingDependencyValidationException.InnerException);
            }
            catch (ContentItemSettingDependencyException contentItemSettingDependencyException)
            {
                return FailedDependency(contentItemSettingDependencyException.InnerException);
            }
            catch (ContentItemSettingServiceException contentItemSettingServiceException)
            {
                return InternalServerError(contentItemSettingServiceException);
            }
        }

        [HttpGet]
        [EnableQuery]
        [AllowAnonymous]
        public async ValueTask<ActionResult<IQueryable<ContentItemSetting>>> Get(CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<ContentItemSetting> retrievedContentItemSettings =
                    await this.contentItemSettingService.RetrieveAllContentItemSettingsAsync(cancellationToken);

                return Ok(retrievedContentItemSettings);
            }
            catch (ContentItemSettingDependencyException contentItemSettingDependencyException)
            {
                return FailedDependency(contentItemSettingDependencyException.InnerException);
            }
            catch (ContentItemSettingServiceException contentItemSettingServiceException)
            {
                return InternalServerError(contentItemSettingServiceException);
            }
        }

        [HttpGet("{contentItemSettingId}")]
        [AllowAnonymous]
        public async ValueTask<ActionResult<ContentItemSetting>> GetContentItemSettingByIdAsync(
            Guid contentItemSettingId,
            CancellationToken cancellationToken)
        {
            try
            {
                ContentItemSetting contentItemSetting = await this.contentItemSettingService.RetrieveContentItemSettingByIdAsync(contentItemSettingId, cancellationToken);

                return Ok(contentItemSetting);
            }
            catch (ContentItemSettingValidationException contentItemSettingValidationException)
                when (contentItemSettingValidationException.InnerException is NotFoundContentItemSettingException)
            {
                return NotFound(contentItemSettingValidationException.InnerException);
            }
            catch (ContentItemSettingValidationException contentItemSettingValidationException)
            {
                return BadRequest(contentItemSettingValidationException.InnerException);
            }
            catch (ContentItemSettingDependencyValidationException contentItemSettingDependencyValidationException)
            {
                return BadRequest(contentItemSettingDependencyValidationException.InnerException);
            }
            catch (ContentItemSettingDependencyException contentItemSettingDependencyException)
            {
                return FailedDependency(contentItemSettingDependencyException.InnerException);
            }
            catch (ContentItemSettingServiceException contentItemSettingServiceException)
            {
                return InternalServerError(contentItemSettingServiceException);
            }
        }

        [HttpPut]
        [Authorize(Roles = Roles.Administrators)]
        public async ValueTask<ActionResult<ContentItemSetting>> PutContentItemSettingAsync(
            [FromBody] ContentItemSetting contentItemSetting,
            CancellationToken cancellationToken)
        {
            try
            {
                ContentItemSetting modifiedContentItemSetting =
                    await this.contentItemSettingService.ModifyContentItemSettingAsync(contentItemSetting, cancellationToken);

                return Ok(modifiedContentItemSetting);
            }
            catch (ContentItemSettingValidationException contentItemSettingValidationException)
                when (contentItemSettingValidationException.InnerException is NotFoundContentItemSettingException)
            {
                return NotFound(contentItemSettingValidationException.InnerException);
            }
            catch (ContentItemSettingValidationException contentItemSettingValidationException)
                when (contentItemSettingValidationException.InnerException is UnauthorizedContentItemSettingException)
            {
                return Unauthorized(contentItemSettingValidationException.InnerException);
            }
            catch (ContentItemSettingValidationException contentItemSettingValidationException)
            {
                return BadRequest(contentItemSettingValidationException.InnerException);
            }
            catch (ContentItemSettingDependencyValidationException contentItemSettingDependencyValidationException)
                when (contentItemSettingDependencyValidationException.InnerException is AlreadyExistsContentItemSettingException)
            {
                return Conflict(contentItemSettingDependencyValidationException.InnerException);
            }
            catch (ContentItemSettingDependencyValidationException contentItemSettingDependencyValidationException)
                when (contentItemSettingDependencyValidationException.InnerException is LockedContentItemSettingException)
            {
                return Locked(contentItemSettingDependencyValidationException.InnerException);
            }
            catch (ContentItemSettingDependencyValidationException contentItemSettingDependencyValidationException)
            {
                return BadRequest(contentItemSettingDependencyValidationException.InnerException);
            }
            catch (ContentItemSettingDependencyException contentItemSettingDependencyException)
            {
                return FailedDependency(contentItemSettingDependencyException.InnerException);
            }
            catch (ContentItemSettingServiceException contentItemSettingServiceException)
            {
                return InternalServerError(contentItemSettingServiceException);
            }
        }

        /// <summary>
        /// Soft removal (design §14.6): the row is marked deleted and keeps its audit trail.
        /// The optional reason is carried through to <c>DeletionReason</c>. A per-type default is
        /// refused with 400 — see the class remarks.
        /// </summary>
        [HttpDelete("{contentItemSettingId}")]
        [Authorize(Roles = Roles.Administrators)]
        public async ValueTask<ActionResult<ContentItemSetting>> DeleteContentItemSettingByIdAsync(
            Guid contentItemSettingId,
            [FromQuery] string? deletionReason,
            CancellationToken cancellationToken)
        {
            try
            {
                ContentItemSetting deletedContentItemSetting =
                    await this.contentItemSettingService.RemoveContentItemSettingByIdAsync(contentItemSettingId, deletionReason, cancellationToken);

                return Ok(deletedContentItemSetting);
            }
            catch (ContentItemSettingValidationException contentItemSettingValidationException)
                when (contentItemSettingValidationException.InnerException is NotFoundContentItemSettingException)
            {
                return NotFound(contentItemSettingValidationException.InnerException);
            }
            catch (ContentItemSettingValidationException contentItemSettingValidationException)
                when (contentItemSettingValidationException.InnerException is UnauthorizedContentItemSettingException)
            {
                return Unauthorized(contentItemSettingValidationException.InnerException);
            }
            catch (ContentItemSettingValidationException contentItemSettingValidationException)
            {
                return BadRequest(contentItemSettingValidationException.InnerException);
            }
            catch (ContentItemSettingDependencyValidationException contentItemSettingDependencyValidationException)
                when (contentItemSettingDependencyValidationException.InnerException is AlreadyExistsContentItemSettingException)
            {
                return Conflict(contentItemSettingDependencyValidationException.InnerException);
            }

            catch (ContentItemSettingDependencyValidationException contentItemSettingDependencyValidationException)
                when (contentItemSettingDependencyValidationException.InnerException is LockedContentItemSettingException)
            {
                return Locked(contentItemSettingDependencyValidationException.InnerException);
            }
            catch (ContentItemSettingDependencyValidationException contentItemSettingDependencyValidationException)
            {
                return BadRequest(contentItemSettingDependencyValidationException.InnerException);
            }
            catch (ContentItemSettingDependencyException contentItemSettingDependencyException)
            {
                return FailedDependency(contentItemSettingDependencyException.InnerException);
            }
            catch (ContentItemSettingServiceException contentItemSettingServiceException)
            {
                return InternalServerError(contentItemSettingServiceException);
            }
        }

        /// <summary>
        /// Permanent removal. Design §14.6 restricts hard removal to <c>Administrators</c>; the attribute
        /// below is the coarse half of that and the foundation re-decides it against the row. A
        /// per-type default is refused with 400 here too — hard delete is not an escape hatch from
        /// the must-always-exist rule.
        /// </summary>
        [HttpDelete("{contentItemSettingId}/Hard")]
        [Authorize(Roles = Roles.Administrators)]
        public async ValueTask<ActionResult<ContentItemSetting>> HardDeleteContentItemSettingByIdAsync(
            Guid contentItemSettingId,
            CancellationToken cancellationToken)
        {
            try
            {
                ContentItemSetting hardDeletedContentItemSetting =
                    await this.contentItemSettingService.HardRemoveContentItemSettingByIdAsync(contentItemSettingId, cancellationToken);

                return Ok(hardDeletedContentItemSetting);
            }
            catch (ContentItemSettingValidationException contentItemSettingValidationException)
                when (contentItemSettingValidationException.InnerException is NotFoundContentItemSettingException)
            {
                return NotFound(contentItemSettingValidationException.InnerException);
            }
            catch (ContentItemSettingValidationException contentItemSettingValidationException)
                when (contentItemSettingValidationException.InnerException is UnauthorizedContentItemSettingException)
            {
                return Unauthorized(contentItemSettingValidationException.InnerException);
            }
            catch (ContentItemSettingValidationException contentItemSettingValidationException)
            {
                return BadRequest(contentItemSettingValidationException.InnerException);
            }
            catch (ContentItemSettingDependencyValidationException contentItemSettingDependencyValidationException)
                when (contentItemSettingDependencyValidationException.InnerException is AlreadyExistsContentItemSettingException)
            {
                return Conflict(contentItemSettingDependencyValidationException.InnerException);
            }

            catch (ContentItemSettingDependencyValidationException contentItemSettingDependencyValidationException)
                when (contentItemSettingDependencyValidationException.InnerException is LockedContentItemSettingException)
            {
                return Locked(contentItemSettingDependencyValidationException.InnerException);
            }
            catch (ContentItemSettingDependencyValidationException contentItemSettingDependencyValidationException)
            {
                return BadRequest(contentItemSettingDependencyValidationException.InnerException);
            }
            catch (ContentItemSettingDependencyException contentItemSettingDependencyException)
            {
                return FailedDependency(contentItemSettingDependencyException.InnerException);
            }
            catch (ContentItemSettingServiceException contentItemSettingServiceException)
            {
                return InternalServerError(contentItemSettingServiceException);
            }
        }

        /// <summary>
        /// Draft → Submitted (design §9.7.1). The owner or the publisher tier may submit, and
        /// the service decides which against the stored row — the attribute only establishes
        /// that somebody is signed in.
        /// </summary>
    }
}
