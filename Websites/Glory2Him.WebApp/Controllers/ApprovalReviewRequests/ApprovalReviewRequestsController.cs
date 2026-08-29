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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Services.Orchestrations.Approvals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RESTFulSense.Controllers;

namespace Glory2Him.WebApp.Controllers.ApprovalReviewRequests
{
    /// <summary>
    /// The withdraw half of the review-invitation surface (design §7.9 rule 5, §17.5). Thin by
    /// construction: it authenticates through middleware, hands the request to
    /// <see cref="IApprovalOrchestrationService"/>, and maps typed exceptions onto status codes.
    ///
    /// <para><b>Why this lives apart from <c>ApprovalsController</c>.</b> Withdrawal is keyed on
    /// the REQUEST row rather than on the entity behind it, so it has no
    /// <c>{entityType}/{entityId}</c> to hang off — §17.5 gives it its own route for that reason.
    /// Issuing an invitation stays on the approvals route, where the entity key is what the
    /// caller holds.</para>
    ///
    /// <para><b>No role list, deliberately.</b> §7.9 rule 5 opens withdrawal to the whole review
    /// tier, matched by SUFFIX across every entity type and content type (§18.6), so a fixed
    /// <c>Roles = ...</c> list could not express it without locking out the scoped tiers and every
    /// entity type added later. The orchestration and the foundation each decide it against the
    /// stored row, which is §14.6 rule 2 rather than duplication.</para>
    ///
    /// <para><b>Retirement has no endpoint here, and must not get one.</b> §7.9 rule 6 retires an
    /// answered invitation under the system identity; exposing that verb would hand every caller a
    /// way to clear the panel while claiming nobody did it.</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ApprovalReviewRequestsController : RESTFulController
    {
        private readonly IApprovalOrchestrationService approvalOrchestrationService;

        public ApprovalReviewRequestsController(
            IApprovalOrchestrationService approvalOrchestrationService) =>
            this.approvalOrchestrationService = approvalOrchestrationService;

        /// <summary>
        /// Withdraws a pending invitation — the undo for one sent to the wrong person.
        ///
        /// <para>Withdrawing an already-withdrawn request is a 204, not a 404: the caller asked
        /// for a state the row is already in, and the foundation returns it unchanged without
        /// publishing a second removal fact.</para>
        ///
        /// <para><c>deletionReason</c> rides the query string for the same reason the sibling
        /// exposers' scalars do — it is a value the operation owns, not a body the caller
        /// composes.</para>
        /// </summary>
        [HttpDelete("{approvalReviewRequestId}")]
        [Authorize]
        public async ValueTask<ActionResult<ApprovalReviewRequest>>
            DeleteApprovalReviewRequestByIdAsync(
                Guid approvalReviewRequestId,
                [FromQuery] string? deletionReason,
                CancellationToken cancellationToken)
        {
            try
            {
                await this.approvalOrchestrationService.WithdrawApprovalReviewRequestAsync(
                    approvalReviewRequestId: approvalReviewRequestId,

                    // Null is the ordinary "no reason supplied" case the orchestration passes
                    // through, so the absent query value is not coerced to an empty string a
                    // length check would then have to treat as blank all over again.
                    deletionReason: deletionReason!,
                    cancellationToken: cancellationToken);

                // The withdrawn row is of no use to a caller - it is the record of something
                // that is now gone, and the panel refreshes from the round rather than from
                // this. Withdrawing one already withdrawn answers the same way, which is what
                // keeps a double click harmless.
                return NoContent();
            }
            catch (ApprovalOrchestrationValidationException approvalOrchestrationValidationException)
                when (approvalOrchestrationValidationException.InnerException
                    is NotFoundApprovalOrchestrationException)
            {
                return NotFound(approvalOrchestrationValidationException.InnerException);
            }
            catch (ApprovalOrchestrationValidationException approvalOrchestrationValidationException)
                when (approvalOrchestrationValidationException.InnerException
                    is UnauthorizedApprovalOrchestrationException)
            {
                return Unauthorized(approvalOrchestrationValidationException.InnerException);
            }
            catch (ApprovalOrchestrationValidationException approvalOrchestrationValidationException)
            {
                return BadRequest(approvalOrchestrationValidationException.InnerException);
            }
            catch (ApprovalOrchestrationDependencyValidationException
                approvalOrchestrationDependencyValidationException)
            {
                return BadRequest(approvalOrchestrationDependencyValidationException.InnerException);
            }
            catch (ApprovalOrchestrationDependencyException approvalOrchestrationDependencyException)
            {
                return FailedDependency(approvalOrchestrationDependencyException.InnerException);
            }
            catch (ApprovalOrchestrationServiceException approvalOrchestrationServiceException)
            {
                return InternalServerError(approvalOrchestrationServiceException);
            }
        }
    }
}
