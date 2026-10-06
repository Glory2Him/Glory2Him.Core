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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Glory2Him.Core.Services.Orchestrations.Associations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RESTFulSense.Controllers;

namespace Glory2Him.WebApp.Controllers.Associations
{
    /// <summary>
    /// The association exposure point (§ARC12.6 row 14). It binds
    /// <see cref="IAssociationOrchestrationService"/> alone (§EVN13 rule 3), holds no logic, and
    /// maps the orchestration's exception families onto status codes. Its routes are added by the
    /// work that needs them rather than all at once: today it serves only the upsert.
    ///
    /// <para><b>The upsert answers <c>201</c> only when a row was created, and <c>200</c>
    /// otherwise</b>, with the result rather than the row as the body. Both depart from
    /// <c>the-standard-exposers</c> ts-exposers-003, and
    /// <c>Documentation/DesignFeatures/Backend/Controllers/AssociationsController.md</c>,
    /// <i>Deviations</i>, records why: five of the six outcomes create nothing, and the row
    /// would leak its author (§ARC16.8.1).</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AssociationsController : RESTFulController
    {
        private readonly IAssociationOrchestrationService associationOrchestrationService;

        public AssociationsController(IAssociationOrchestrationService associationOrchestrationService) =>
            this.associationOrchestrationService = associationOrchestrationService;

        [HttpPost]
        [Authorize]
        public async ValueTask<ActionResult<AssociationSuggestionResult>> PostAssociationAsync(
            [FromBody] Association association,
            CancellationToken cancellationToken)
        {
            try
            {
                AssociationSuggestionResult associationSuggestionResult =
                    await this.associationOrchestrationService.UpsertAssociationAsync(
                        association,
                        cancellationToken);

                return associationSuggestionResult.Status is AssociationSuggestionStatus.Created
                    ? Created(associationSuggestionResult)
                    : Ok(associationSuggestionResult);
            }
            catch (AssociationOrchestrationValidationException
                associationOrchestrationValidationException)
                when (associationOrchestrationValidationException.InnerException
                    is UnauthorizedAssociationOrchestrationException)
            {
                return Unauthorized(associationOrchestrationValidationException.InnerException);
            }
            catch (AssociationOrchestrationValidationException
                associationOrchestrationValidationException)
                when (associationOrchestrationValidationException.InnerException
                    is NotFoundAssociationOrchestrationException)
            {
                return NotFound(associationOrchestrationValidationException.InnerException);
            }
            catch (AssociationOrchestrationValidationException
                associationOrchestrationValidationException)
            {
                return BadRequest(associationOrchestrationValidationException.InnerException);
            }
            catch (AssociationOrchestrationDependencyValidationException
                associationOrchestrationDependencyValidationException)
                when (associationOrchestrationDependencyValidationException.InnerException
                    is AlreadyExistsAssociationException)
            {
                return Conflict(associationOrchestrationDependencyValidationException.InnerException);
            }
            catch (AssociationOrchestrationDependencyValidationException
                associationOrchestrationDependencyValidationException)
            {
                return BadRequest(associationOrchestrationDependencyValidationException.InnerException);
            }
            catch (AssociationOrchestrationDependencyException
                associationOrchestrationDependencyException)
            {
                return FailedDependency(associationOrchestrationDependencyException.InnerException);
            }
            catch (AssociationOrchestrationServiceException
                associationOrchestrationServiceException)
            {
                return InternalServerError(associationOrchestrationServiceException);
            }
        }

        [HttpGet("ReactionSummaries")]
        [AllowAnonymous]
        public async ValueTask<ActionResult<IReadOnlyList<ContentItemReactionSummary>>> GetReactionSummariesAsync(
            [FromQuery] Guid[] contentItemIds,
            CancellationToken cancellationToken)
        {
            try
            {
                IReadOnlyList<ContentItemReactionSummary> contentItemReactionSummaries =
                    await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                        contentItemIds,
                        cancellationToken);

                return Ok(contentItemReactionSummaries);
            }
            catch (AssociationOrchestrationValidationException
                associationOrchestrationValidationException)
            {
                return BadRequest(associationOrchestrationValidationException.InnerException);
            }
        }
    }
}
