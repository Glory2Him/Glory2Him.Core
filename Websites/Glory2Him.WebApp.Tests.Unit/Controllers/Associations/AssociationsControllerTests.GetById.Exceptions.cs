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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Associations
{
    public partial class AssociationsControllerTests
    {
        /// <summary>
        /// Criterion 8: the arm the shared mapping does not have. No row, a row taken down and a
        /// row the caller's posture hides all leave the orchestration as this family
        /// (<c>AssociationsController.AssociationReads.md</c> §2 rule 2).
        /// </summary>
        [Fact]
        public async Task ShouldReturnNotFoundOnGetByIdIfAssociationIsNotFoundAsync()
        {
            // given
            Guid someAssociationId = Guid.NewGuid();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            var notFoundAssociationException =
                new NotFoundAssociationException(
                    message: $"Content item association not found with id: {someAssociationId}.");

            var associationOrchestrationDependencyValidationException =
                new AssociationOrchestrationDependencyValidationException(
                    message: GetRandomString(),
                    innerException: notFoundAssociationException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundAssociationException);

            var expectedActionResult =
                new ActionResult<Association>(expectedNotFoundObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.RetrieveAssociationByIdAsync(
                    someAssociationId,
                    cancellationToken))
                        .ThrowsAsync(associationOrchestrationDependencyValidationException);

            // when
            ActionResult<Association> actualActionResult =
                await this.associationsController.GetAssociationByIdAsync(
                    someAssociationId,
                    cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.RetrieveAssociationByIdAsync(
                    someAssociationId,
                    cancellationToken),
                        Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnGetByIdIfAnEndpointIsNotVisibleAsync()
        {
            // given
            Guid someAssociationId = Guid.NewGuid();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            var notFoundAssociationOrchestrationException =
                new NotFoundAssociationOrchestrationException(
                    message: $"Content item association not found with id: {someAssociationId}.");

            var associationOrchestrationValidationException =
                new AssociationOrchestrationValidationException(
                    message: GetRandomString(),
                    innerException: notFoundAssociationOrchestrationException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundAssociationOrchestrationException);

            var expectedActionResult =
                new ActionResult<Association>(expectedNotFoundObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.RetrieveAssociationByIdAsync(
                    someAssociationId,
                    cancellationToken))
                        .ThrowsAsync(associationOrchestrationValidationException);

            // when
            ActionResult<Association> actualActionResult =
                await this.associationsController.GetAssociationByIdAsync(
                    someAssociationId,
                    cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.RetrieveAssociationByIdAsync(
                    someAssociationId,
                    cancellationToken),
                        Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnGetByIdIfValidationErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            Guid someAssociationId = Guid.NewGuid();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<Association>(expectedBadRequestObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.RetrieveAssociationByIdAsync(
                    someAssociationId,
                    cancellationToken))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<Association> actualActionResult =
                await this.associationsController.GetAssociationByIdAsync(
                    someAssociationId,
                    cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.RetrieveAssociationByIdAsync(
                    someAssociationId,
                    cancellationToken),
                        Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
