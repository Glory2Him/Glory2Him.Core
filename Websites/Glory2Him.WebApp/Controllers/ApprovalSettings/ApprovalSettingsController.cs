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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.ApprovalSettings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RESTFulSense.Controllers;

namespace Glory2Him.WebApp.Controllers.ApprovalSettings
{
    /// <summary>
    /// The approvalSetting exposure point (design §12.6). Thin by construction: it authenticates through
    /// middleware, hands the request to <see cref="IApprovalSettingService"/>, and maps the service's typed
    /// exceptions onto HTTP status codes. It carries no business logic and builds no
    /// <c>SecurityContext</c>, <c>RequestContext</c> or <c>EventEnvelope&lt;T&gt;</c> — those are
    /// created only inside the service (design §10.12).
    ///
    /// <para>It binds to the foundation, and nothing above it is planned. §12.4's intended
    /// processings table has three rows and this is not one of them; §12.5's intended
    /// orchestrations table has three and this is not one of those either. The §8.4 policy
    /// resolution that <i>reads</i> these rows lives in <c>IAccessClient</c>'s
    /// <c>EvaluateConditions</c>, reached through <c>IAccessBroker</c> — not in a layer above
    /// this service (§8.5, §8.6.1, and §12.5.3 R5, which corrects the earlier claim that the
    /// orchestration owned the threshold). So the foundation is the top-layer service §10.17
    /// rule 3 requires an exposer to bind to.</para>
    ///
    /// <para><b>Posture C (§14.7), and it differs from posture A in both directions.</b> All
    /// writes including hard removal are <c>Admin</c> only — there is no owner branch, because
    /// only administrators author configuration — so unlike the content exposers the role list
    /// is expressible on every write and the attribute names it rather than deferring to the
    /// service. The service still re-decides it against the stored row (§14.6): the attribute is
    /// the coarse half either way.</para>
    ///
    /// <para><b>The reads are <c>[Authorize]</c>, not <c>[AllowAnonymous]</c>, and that is the
    /// opposite of every content exposer.</b> Posture C rule 2 gives read access to any
    /// authenticated caller — anyone signed in may see the rules their submissions run under —
    /// and gives anonymous callers not-found or an empty set. There is no §14.1
    /// approval-visibility concept here at all and only non-deleted rows are visible, so there
    /// is no public predicate for the service to degrade to. Answering 401 at the attribute is
    /// therefore correct, where on a posture A entity it would make the public read surface
    /// unreachable. <c>ContentItemSettingsController</c> shares this posture and takes the
    /// opposite read gate, because effective settings drive rendering for anonymous
    /// visitors — the two are worth reading together.</para>
    ///
    /// <para><b>No approval verbs.</b> §7.5 entry 9 lists <c>ApprovalSetting</c> as approvable
    /// <i>"if policy changes require approval"</i> — a conditional never taken up. The entity
    /// carries no <c>ApprovalStatus</c>, <c>IsPublished</c> or bypass pair at all, and
    /// <c>IApprovalSettingService</c> has no submit and no approval transition. So this exposer
    /// has six endpoints rather than eight. If the condition is ever taken up, the verbs land on
    /// the service first and this follows.</para>
    ///
    /// <para><b>Two unique indexes make 409 a live response.</b>
    /// <c>UX_ApprovalSettings_EntityTypeDefault</c> allows one default per entity type
    /// (<c>ContentType IS NULL</c>) and <c>UX_ApprovalSettings_EntityTypeContentType</c> allows
    /// one override per pair. §8.4 policy resolution depends on at most one setting per scope,
    /// so these are the rule rather than a storage detail.</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ApprovalSettingsController : RESTFulController
    {
        private readonly IApprovalSettingService approvalSettingService;

        public ApprovalSettingsController(IApprovalSettingService approvalSettingService) =>
            this.approvalSettingService = approvalSettingService;

        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public async ValueTask<ActionResult<ApprovalSetting>> PostApprovalSettingAsync(
            [FromBody] ApprovalSetting approvalSetting,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalSetting addedApprovalSetting =
                    await this.approvalSettingService.AddApprovalSettingAsync(approvalSetting, cancellationToken);

                return Created(addedApprovalSetting);
            }
            catch (ApprovalSettingValidationException approvalSettingValidationException)
                when (approvalSettingValidationException.InnerException is UnauthorizedApprovalSettingException)
            {
                return Unauthorized(approvalSettingValidationException.InnerException);
            }
            catch (ApprovalSettingValidationException approvalSettingValidationException)
            {
                return BadRequest(approvalSettingValidationException.InnerException);
            }
            catch (ApprovalSettingDependencyValidationException approvalSettingDependencyValidationException)
                when (approvalSettingDependencyValidationException.InnerException is AlreadyExistsApprovalSettingException)
            {
                return Conflict(approvalSettingDependencyValidationException.InnerException);
            }
            catch (ApprovalSettingDependencyValidationException approvalSettingDependencyValidationException)
            {
                return BadRequest(approvalSettingDependencyValidationException.InnerException);
            }
            catch (ApprovalSettingDependencyException approvalSettingDependencyException)
            {
                return FailedDependency(approvalSettingDependencyException.InnerException);
            }
            catch (ApprovalSettingServiceException approvalSettingServiceException)
            {
                return InternalServerError(approvalSettingServiceException);
            }
        }

        [HttpGet]
        [EnableQuery]
        [Authorize]
        public async ValueTask<ActionResult<IQueryable<ApprovalSetting>>> Get(CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<ApprovalSetting> retrievedApprovalSettings =
                    await this.approvalSettingService.RetrieveAllApprovalSettingsAsync(cancellationToken);

                return Ok(retrievedApprovalSettings);
            }
            catch (ApprovalSettingDependencyException approvalSettingDependencyException)
            {
                return FailedDependency(approvalSettingDependencyException.InnerException);
            }
            catch (ApprovalSettingServiceException approvalSettingServiceException)
            {
                return InternalServerError(approvalSettingServiceException);
            }
        }

        [HttpGet("{approvalSettingId}")]
        [Authorize]
        public async ValueTask<ActionResult<ApprovalSetting>> GetApprovalSettingByIdAsync(
            Guid approvalSettingId,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalSetting approvalSetting = await this.approvalSettingService.RetrieveApprovalSettingByIdAsync(approvalSettingId, cancellationToken);

                return Ok(approvalSetting);
            }
            catch (ApprovalSettingValidationException approvalSettingValidationException)
                when (approvalSettingValidationException.InnerException is NotFoundApprovalSettingException)
            {
                return NotFound(approvalSettingValidationException.InnerException);
            }
            catch (ApprovalSettingValidationException approvalSettingValidationException)
            {
                return BadRequest(approvalSettingValidationException.InnerException);
            }
            catch (ApprovalSettingDependencyValidationException approvalSettingDependencyValidationException)
            {
                return BadRequest(approvalSettingDependencyValidationException.InnerException);
            }
            catch (ApprovalSettingDependencyException approvalSettingDependencyException)
            {
                return FailedDependency(approvalSettingDependencyException.InnerException);
            }
            catch (ApprovalSettingServiceException approvalSettingServiceException)
            {
                return InternalServerError(approvalSettingServiceException);
            }
        }

        [HttpPut]
        [Authorize(Roles = Roles.Admin)]
        public async ValueTask<ActionResult<ApprovalSetting>> PutApprovalSettingAsync(
            [FromBody] ApprovalSetting approvalSetting,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalSetting modifiedApprovalSetting =
                    await this.approvalSettingService.ModifyApprovalSettingAsync(approvalSetting, cancellationToken);

                return Ok(modifiedApprovalSetting);
            }
            catch (ApprovalSettingValidationException approvalSettingValidationException)
                when (approvalSettingValidationException.InnerException is NotFoundApprovalSettingException)
            {
                return NotFound(approvalSettingValidationException.InnerException);
            }
            catch (ApprovalSettingValidationException approvalSettingValidationException)
                when (approvalSettingValidationException.InnerException is UnauthorizedApprovalSettingException)
            {
                return Unauthorized(approvalSettingValidationException.InnerException);
            }
            catch (ApprovalSettingValidationException approvalSettingValidationException)
            {
                return BadRequest(approvalSettingValidationException.InnerException);
            }
            catch (ApprovalSettingDependencyValidationException approvalSettingDependencyValidationException)
                when (approvalSettingDependencyValidationException.InnerException is AlreadyExistsApprovalSettingException)
            {
                return Conflict(approvalSettingDependencyValidationException.InnerException);
            }
            catch (ApprovalSettingDependencyValidationException approvalSettingDependencyValidationException)
                when (approvalSettingDependencyValidationException.InnerException is LockedApprovalSettingException)
            {
                return Locked(approvalSettingDependencyValidationException.InnerException);
            }
            catch (ApprovalSettingDependencyValidationException approvalSettingDependencyValidationException)
            {
                return BadRequest(approvalSettingDependencyValidationException.InnerException);
            }
            catch (ApprovalSettingDependencyException approvalSettingDependencyException)
            {
                return FailedDependency(approvalSettingDependencyException.InnerException);
            }
            catch (ApprovalSettingServiceException approvalSettingServiceException)
            {
                return InternalServerError(approvalSettingServiceException);
            }
        }

        /// <summary>
        /// Soft removal (design §14.6): the row is marked deleted and keeps its audit trail.
        /// The optional reason is carried through to <c>DeletionReason</c>.
        /// </summary>
        [HttpDelete("{approvalSettingId}")]
        [Authorize(Roles = Roles.Admin)]
        public async ValueTask<ActionResult<ApprovalSetting>> DeleteApprovalSettingByIdAsync(
            Guid approvalSettingId,
            [FromQuery] string? deletionReason,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalSetting deletedApprovalSetting =
                    await this.approvalSettingService.RemoveApprovalSettingByIdAsync(approvalSettingId, deletionReason, cancellationToken);

                return Ok(deletedApprovalSetting);
            }
            catch (ApprovalSettingValidationException approvalSettingValidationException)
                when (approvalSettingValidationException.InnerException is NotFoundApprovalSettingException)
            {
                return NotFound(approvalSettingValidationException.InnerException);
            }
            catch (ApprovalSettingValidationException approvalSettingValidationException)
                when (approvalSettingValidationException.InnerException is UnauthorizedApprovalSettingException)
            {
                return Unauthorized(approvalSettingValidationException.InnerException);
            }
            catch (ApprovalSettingValidationException approvalSettingValidationException)
            {
                return BadRequest(approvalSettingValidationException.InnerException);
            }
            catch (ApprovalSettingDependencyValidationException approvalSettingDependencyValidationException)
                when (approvalSettingDependencyValidationException.InnerException is AlreadyExistsApprovalSettingException)
            {
                return Conflict(approvalSettingDependencyValidationException.InnerException);
            }

            catch (ApprovalSettingDependencyValidationException approvalSettingDependencyValidationException)
                when (approvalSettingDependencyValidationException.InnerException is LockedApprovalSettingException)
            {
                return Locked(approvalSettingDependencyValidationException.InnerException);
            }
            catch (ApprovalSettingDependencyValidationException approvalSettingDependencyValidationException)
            {
                return BadRequest(approvalSettingDependencyValidationException.InnerException);
            }
            catch (ApprovalSettingDependencyException approvalSettingDependencyException)
            {
                return FailedDependency(approvalSettingDependencyException.InnerException);
            }
            catch (ApprovalSettingServiceException approvalSettingServiceException)
            {
                return InternalServerError(approvalSettingServiceException);
            }
        }

        /// <summary>
        /// Permanent removal. Design §14.6 restricts hard removal to <c>Admin</c>; the attribute
        /// below is the coarse half of that and the foundation re-decides it against the row.
        /// </summary>
        [HttpDelete("{approvalSettingId}/Hard")]
        [Authorize(Roles = Roles.Admin)]
        public async ValueTask<ActionResult<ApprovalSetting>> HardDeleteApprovalSettingByIdAsync(
            Guid approvalSettingId,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalSetting hardDeletedApprovalSetting =
                    await this.approvalSettingService.HardRemoveApprovalSettingByIdAsync(approvalSettingId, cancellationToken);

                return Ok(hardDeletedApprovalSetting);
            }
            catch (ApprovalSettingValidationException approvalSettingValidationException)
                when (approvalSettingValidationException.InnerException is NotFoundApprovalSettingException)
            {
                return NotFound(approvalSettingValidationException.InnerException);
            }
            catch (ApprovalSettingValidationException approvalSettingValidationException)
                when (approvalSettingValidationException.InnerException is UnauthorizedApprovalSettingException)
            {
                return Unauthorized(approvalSettingValidationException.InnerException);
            }
            catch (ApprovalSettingValidationException approvalSettingValidationException)
            {
                return BadRequest(approvalSettingValidationException.InnerException);
            }
            catch (ApprovalSettingDependencyValidationException approvalSettingDependencyValidationException)
                when (approvalSettingDependencyValidationException.InnerException is AlreadyExistsApprovalSettingException)
            {
                return Conflict(approvalSettingDependencyValidationException.InnerException);
            }

            catch (ApprovalSettingDependencyValidationException approvalSettingDependencyValidationException)
                when (approvalSettingDependencyValidationException.InnerException is LockedApprovalSettingException)
            {
                return Locked(approvalSettingDependencyValidationException.InnerException);
            }
            catch (ApprovalSettingDependencyValidationException approvalSettingDependencyValidationException)
            {
                return BadRequest(approvalSettingDependencyValidationException.InnerException);
            }
            catch (ApprovalSettingDependencyException approvalSettingDependencyException)
            {
                return FailedDependency(approvalSettingDependencyException.InnerException);
            }
            catch (ApprovalSettingServiceException approvalSettingServiceException)
            {
                return InternalServerError(approvalSettingServiceException);
            }
        }

        /// <summary>
        /// Draft → Submitted (design §9.7.1). The owner or the publisher tier may submit, and
        /// the service decides which against the stored row — the attribute only establishes
        /// that somebody is signed in.
        /// </summary>
    }
}
