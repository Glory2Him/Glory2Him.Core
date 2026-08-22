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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.ApprovalReviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RESTFulSense.Controllers;

namespace Glory2Him.WebApp.Controllers.ApprovalReviews
{
    /// <summary>
    /// The approval-review exposure point (design §12.6). Thin by construction: it authenticates
    /// through middleware, hands the request to <see cref="IApprovalReviewService"/>, and maps the
    /// service's typed exceptions onto HTTP status codes. It carries no business logic and builds
    /// no <c>SecurityContext</c>, <c>RequestContext</c> or <c>EventEnvelope&lt;T&gt;</c> — those are
    /// created only inside the service (design §10.12).
    ///
    /// <para><b>There is no dismissal endpoint, and there must not be one</b> (#295). Dismissal
    /// is the approval workflow's own act: a reviewer records <c>Approved</c> or <c>Rejected</c>,
    /// and <c>Dismissed</c> is what happens <i>to</i> a verdict when the content it judged
    /// changes. It runs in process on an internal seam that mints the system identity itself, so
    /// no route — and no event address — can ask for it.</para>
    ///
    /// <para>Only hard removal carries a role list. The rest, for the same reason:</para>
    ///
    /// <list type="bullet">
    /// <item>Add and both reads admit any review-role holder — global <c>Reviewer</c>,
    /// <c>Publisher</c>, <c>Admin</c>, or any <c>%EntityType%-Reviewer</c> /
    /// <c>%EntityType%-Publisher</c>. Suffix-matched, so not enumerable.</item>
    /// <item>Modify and soft removal are the <b>owner alone</b> — not <c>Publisher</c>, not
    /// <c>Admin</c>. A verdict belongs to the reviewer who recorded it; an Admin who needs past a
    /// standing rejection bypasses the block (§8.6.1) rather than editing the review out of the
    /// way, which keeps the record of what was actually said intact (§14.7 rule 4).</item>
    /// <item>Hard removal is the global <c>Admin</c> and nothing else — a closed, enumerable set
    /// with no owner branch, which is why it is the only one in the attribute.</item>
    /// </list>
    ///
    /// <para>This entity is §14.7 <b>posture D</b>: these records are never public, so the reads
    /// are gated rather than <c>[AllowAnonymous]</c>.</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ApprovalReviewsController : RESTFulController
    {
        private readonly IApprovalReviewService approvalReviewService;

        public ApprovalReviewsController(IApprovalReviewService approvalReviewService) =>
            this.approvalReviewService = approvalReviewService;

        /// <summary>
        /// Records a verdict. The 409 clause below is the correct mapping for the unique index
        /// <c>UX_ApprovalReviews_ApprovalId_CreatedBy</c>, but it is <b>unreachable over HTTP</b>:
        /// a second active review by the same author is refused by the access decision
        /// (<c>ActiveReviewAlreadyRecorded</c> → <c>UnauthorizedApprovalReviewException</c> → 401)
        /// before the insert runs, so only a genuine concurrent race reaches the index. It is kept
        /// because that race is real, and pinned in the unit suite rather than in acceptance.
        /// </summary>
        [HttpPost]
        [Authorize]
        public async ValueTask<ActionResult<ApprovalReview>> PostApprovalReviewAsync(
            [FromBody] ApprovalReview approvalReview,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalReview addedApprovalReview =
                    await this.approvalReviewService.AddApprovalReviewAsync(
                        approvalReview,
                        cancellationToken);

                return Created(addedApprovalReview);
            }
            catch (ApprovalReviewValidationException approvalReviewValidationException)
                when (approvalReviewValidationException.InnerException
                    is UnauthorizedApprovalReviewException)
            {
                return Unauthorized(approvalReviewValidationException.InnerException);
            }
            catch (ApprovalReviewValidationException approvalReviewValidationException)
            {
                return BadRequest(approvalReviewValidationException.InnerException);
            }
            catch (ApprovalReviewDependencyValidationException approvalReviewDependencyValidationException)
                when (approvalReviewDependencyValidationException.InnerException
                    is AlreadyExistsApprovalReviewException)
            {
                return Conflict(approvalReviewDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewDependencyValidationException approvalReviewDependencyValidationException)
                when (approvalReviewDependencyValidationException.InnerException
                    is LockedApprovalReviewException)
            {
                return Locked(approvalReviewDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewDependencyValidationException approvalReviewDependencyValidationException)
            {
                return BadRequest(approvalReviewDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewDependencyException approvalReviewDependencyException)
            {
                return FailedDependency(approvalReviewDependencyException.InnerException);
            }
            catch (ApprovalReviewServiceException approvalReviewServiceException)
            {
                return InternalServerError(approvalReviewServiceException);
            }
        }

        [HttpGet]
        [EnableQuery]
        [Authorize]
        public async ValueTask<ActionResult<IQueryable<ApprovalReview>>> Get(
            CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<ApprovalReview> retrievedApprovalReviews =
                    await this.approvalReviewService.RetrieveAllApprovalReviewsAsync(
                        cancellationToken);

                return Ok(retrievedApprovalReviews);
            }
            catch (ApprovalReviewDependencyException approvalReviewDependencyException)
            {
                return FailedDependency(approvalReviewDependencyException.InnerException);
            }
            catch (ApprovalReviewServiceException approvalReviewServiceException)
            {
                return InternalServerError(approvalReviewServiceException);
            }
        }

        /// <summary>
        /// No <c>Unauthorized</c> clause, and that is the posture rather than an omission. Every
        /// way this read can refuse — soft-deleted, unauthenticated, holding no review role —
        /// throws <c>NotFound</c> (§14.5 rule 1), precisely so the endpoint cannot be used to
        /// probe which reviews exist. Mapping an <c>Unauthorized</c> here would be dead code
        /// today and would leak existence the day something threw it.
        ///
        /// <para>The <c>Conflict</c> and <c>Locked</c> dependency-validation clauses are absent for
        /// the same reason, and both sibling exposers omit them here too: a read performs a SELECT,
        /// and every source of <c>ApprovalReviewDependencyValidationException</c> is a write-path
        /// fault — duplicate key, unique-index violation, foreign-key conflict, update
        /// concurrency. Carrying them would advertise a 409 and a 423 on a read that cannot
        /// produce either.</para>
        /// </summary>
        [HttpGet("{approvalReviewId}")]
        [Authorize]
        public async ValueTask<ActionResult<ApprovalReview>> GetApprovalReviewByIdAsync(
            Guid approvalReviewId,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalReview approvalReview =
                    await this.approvalReviewService.RetrieveApprovalReviewByIdAsync(
                        approvalReviewId,
                        cancellationToken);

                return Ok(approvalReview);
            }
            catch (ApprovalReviewValidationException approvalReviewValidationException)
                when (approvalReviewValidationException.InnerException
                    is NotFoundApprovalReviewException)
            {
                return NotFound(approvalReviewValidationException.InnerException);
            }
            catch (ApprovalReviewValidationException approvalReviewValidationException)
            {
                return BadRequest(approvalReviewValidationException.InnerException);
            }
            catch (ApprovalReviewDependencyValidationException approvalReviewDependencyValidationException)
            {
                return BadRequest(approvalReviewDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewDependencyException approvalReviewDependencyException)
            {
                return FailedDependency(approvalReviewDependencyException.InnerException);
            }
            catch (ApprovalReviewServiceException approvalReviewServiceException)
            {
                return InternalServerError(approvalReviewServiceException);
            }
        }

        [HttpPut]
        [Authorize]
        public async ValueTask<ActionResult<ApprovalReview>> PutApprovalReviewAsync(
            [FromBody] ApprovalReview approvalReview,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalReview modifiedApprovalReview =
                    await this.approvalReviewService.ModifyApprovalReviewAsync(
                        approvalReview,
                        cancellationToken);

                return Ok(modifiedApprovalReview);
            }
            catch (ApprovalReviewValidationException approvalReviewValidationException)
                when (approvalReviewValidationException.InnerException
                    is NotFoundApprovalReviewException)
            {
                return NotFound(approvalReviewValidationException.InnerException);
            }
            catch (ApprovalReviewValidationException approvalReviewValidationException)
                when (approvalReviewValidationException.InnerException
                    is UnauthorizedApprovalReviewException)
            {
                return Unauthorized(approvalReviewValidationException.InnerException);
            }
            catch (ApprovalReviewValidationException approvalReviewValidationException)
            {
                return BadRequest(approvalReviewValidationException.InnerException);
            }
            catch (ApprovalReviewDependencyValidationException approvalReviewDependencyValidationException)
                when (approvalReviewDependencyValidationException.InnerException
                    is AlreadyExistsApprovalReviewException)
            {
                return Conflict(approvalReviewDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewDependencyValidationException approvalReviewDependencyValidationException)
                when (approvalReviewDependencyValidationException.InnerException
                    is LockedApprovalReviewException)
            {
                return Locked(approvalReviewDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewDependencyValidationException approvalReviewDependencyValidationException)
            {
                return BadRequest(approvalReviewDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewDependencyException approvalReviewDependencyException)
            {
                return FailedDependency(approvalReviewDependencyException.InnerException);
            }
            catch (ApprovalReviewServiceException approvalReviewServiceException)
            {
                return InternalServerError(approvalReviewServiceException);
            }
        }

        /// <summary>
        /// Soft removal, owner-only at the foundation. The reason rides the query string for the
        /// same reason it does on the sibling exposers: it is a scalar the operation owns
        /// outright, not a body the caller composes.
        /// </summary>
        [HttpDelete("{approvalReviewId}")]
        [Authorize]
        public async ValueTask<ActionResult<ApprovalReview>> DeleteApprovalReviewByIdAsync(
            Guid approvalReviewId,
            [FromQuery] string? deletionReason,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalReview deletedApprovalReview =
                    await this.approvalReviewService.RemoveApprovalReviewByIdAsync(
                        approvalReviewId,
                        deletionReason,
                        cancellationToken);

                return Ok(deletedApprovalReview);
            }
            catch (ApprovalReviewValidationException approvalReviewValidationException)
                when (approvalReviewValidationException.InnerException
                    is NotFoundApprovalReviewException)
            {
                return NotFound(approvalReviewValidationException.InnerException);
            }
            catch (ApprovalReviewValidationException approvalReviewValidationException)
                when (approvalReviewValidationException.InnerException
                    is UnauthorizedApprovalReviewException)
            {
                return Unauthorized(approvalReviewValidationException.InnerException);
            }
            catch (ApprovalReviewValidationException approvalReviewValidationException)
            {
                return BadRequest(approvalReviewValidationException.InnerException);
            }
            catch (ApprovalReviewDependencyValidationException approvalReviewDependencyValidationException)
                when (approvalReviewDependencyValidationException.InnerException
                    is AlreadyExistsApprovalReviewException)
            {
                return Conflict(approvalReviewDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewDependencyValidationException approvalReviewDependencyValidationException)
                when (approvalReviewDependencyValidationException.InnerException
                    is LockedApprovalReviewException)
            {
                return Locked(approvalReviewDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewDependencyValidationException approvalReviewDependencyValidationException)
            {
                return BadRequest(approvalReviewDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewDependencyException approvalReviewDependencyException)
            {
                return FailedDependency(approvalReviewDependencyException.InnerException);
            }
            catch (ApprovalReviewServiceException approvalReviewServiceException)
            {
                return InternalServerError(approvalReviewServiceException);
            }
        }

        /// <summary>
        /// Permanent removal, and the one gate here with no owner branch: the foundation requires
        /// the global <c>Admin</c> role and admits neither the reviewer who recorded the verdict
        /// nor any suffix-matched publisher role, so the set is closed and enumerable and belongs
        /// in the attribute.
        /// </summary>
        [HttpDelete("{approvalReviewId}/Hard")]
        [Authorize(Roles = Roles.Admin)]
        public async ValueTask<ActionResult<ApprovalReview>> HardDeleteApprovalReviewByIdAsync(
            Guid approvalReviewId,
            CancellationToken cancellationToken)
        {
            try
            {
                ApprovalReview hardDeletedApprovalReview =
                    await this.approvalReviewService.HardRemoveApprovalReviewByIdAsync(
                        approvalReviewId,
                        cancellationToken);

                return Ok(hardDeletedApprovalReview);
            }
            catch (ApprovalReviewValidationException approvalReviewValidationException)
                when (approvalReviewValidationException.InnerException
                    is NotFoundApprovalReviewException)
            {
                return NotFound(approvalReviewValidationException.InnerException);
            }
            catch (ApprovalReviewValidationException approvalReviewValidationException)
                when (approvalReviewValidationException.InnerException
                    is UnauthorizedApprovalReviewException)
            {
                return Unauthorized(approvalReviewValidationException.InnerException);
            }
            catch (ApprovalReviewValidationException approvalReviewValidationException)
            {
                return BadRequest(approvalReviewValidationException.InnerException);
            }
            catch (ApprovalReviewDependencyValidationException approvalReviewDependencyValidationException)
                when (approvalReviewDependencyValidationException.InnerException
                    is AlreadyExistsApprovalReviewException)
            {
                return Conflict(approvalReviewDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewDependencyValidationException approvalReviewDependencyValidationException)
                when (approvalReviewDependencyValidationException.InnerException
                    is LockedApprovalReviewException)
            {
                return Locked(approvalReviewDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewDependencyValidationException approvalReviewDependencyValidationException)
            {
                return BadRequest(approvalReviewDependencyValidationException.InnerException);
            }
            catch (ApprovalReviewDependencyException approvalReviewDependencyException)
            {
                return FailedDependency(approvalReviewDependencyException.InnerException);
            }
            catch (ApprovalReviewServiceException approvalReviewServiceException)
            {
                return InternalServerError(approvalReviewServiceException);
            }
        }
    }
}
