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
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Associations
{
    public partial class AssociationsControllerTests
    {
        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfUnauthorizedErrorOccurredAsync()
        {
            // given
            Association someAssociation = CreateRandomAssociation();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            string someMessage = GetRandomString();

            var unauthorizedAssociationOrchestrationException =
                new UnauthorizedAssociationOrchestrationException(
                    message: someMessage);

            var associationOrchestrationValidationException =
                new AssociationOrchestrationValidationException(
                    message: someMessage,
                    innerException: unauthorizedAssociationOrchestrationException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedAssociationOrchestrationException);

            var expectedActionResult =
                new ActionResult<AssociationSuggestionResult>(expectedUnauthorizedObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.UpsertAssociationAsync(someAssociation, cancellationToken))
                    .ThrowsAsync(associationOrchestrationValidationException);

            // when
            ActionResult<AssociationSuggestionResult> actualActionResult =
                await this.associationsController.PostAssociationAsync(someAssociation, cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.UpsertAssociationAsync(someAssociation, cancellationToken),
                    Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnPostIfAnEndpointIsNotFoundAsync()
        {
            // given
            Association someAssociation = CreateRandomAssociation();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            string someMessage = GetRandomString();

            var notFoundAssociationOrchestrationException =
                new NotFoundAssociationOrchestrationException(
                    message: someMessage);

            var associationOrchestrationValidationException =
                new AssociationOrchestrationValidationException(
                    message: someMessage,
                    innerException: notFoundAssociationOrchestrationException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundAssociationOrchestrationException);

            var expectedActionResult =
                new ActionResult<AssociationSuggestionResult>(expectedNotFoundObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.UpsertAssociationAsync(someAssociation, cancellationToken))
                    .ThrowsAsync(associationOrchestrationValidationException);

            // when
            ActionResult<AssociationSuggestionResult> actualActionResult =
                await this.associationsController.PostAssociationAsync(someAssociation, cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.UpsertAssociationAsync(someAssociation, cancellationToken),
                    Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnBadRequestOnPostIfValidationErrorOccurredAsync()
        {
            // given
            Association someAssociation = CreateRandomAssociation();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            var someInnerException = new Xeption();

            var associationOrchestrationValidationException =
                new AssociationOrchestrationValidationException(
                    message: GetRandomString(),
                    innerException: someInnerException);

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(someInnerException);

            var expectedActionResult =
                new ActionResult<AssociationSuggestionResult>(expectedBadRequestObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.UpsertAssociationAsync(someAssociation, cancellationToken))
                    .ThrowsAsync(associationOrchestrationValidationException);

            // when
            ActionResult<AssociationSuggestionResult> actualActionResult =
                await this.associationsController.PostAssociationAsync(someAssociation, cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.UpsertAssociationAsync(someAssociation, cancellationToken),
                    Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnConflictOnPostIfAlreadyExistsErrorOccurredAsync()
        {
            // given
            Association someAssociation = CreateRandomAssociation();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var alreadyExistsAssociationException =
                new AlreadyExistsAssociationException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var associationOrchestrationDependencyValidationException =
                new AssociationOrchestrationDependencyValidationException(
                    message: someMessage,
                    innerException: alreadyExistsAssociationException);

            ConflictObjectResult expectedConflictObjectResult =
                Conflict(alreadyExistsAssociationException);

            var expectedActionResult =
                new ActionResult<AssociationSuggestionResult>(expectedConflictObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.UpsertAssociationAsync(someAssociation, cancellationToken))
                    .ThrowsAsync(associationOrchestrationDependencyValidationException);

            // when
            ActionResult<AssociationSuggestionResult> actualActionResult =
                await this.associationsController.PostAssociationAsync(someAssociation, cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.UpsertAssociationAsync(someAssociation, cancellationToken),
                    Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnBadRequestOnPostIfDependencyValidationErrorOccurredAsync()
        {
            // given
            Association someAssociation = CreateRandomAssociation();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            var someInnerException = new Xeption();

            var associationOrchestrationDependencyValidationException =
                new AssociationOrchestrationDependencyValidationException(
                    message: GetRandomString(),
                    innerException: someInnerException);

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(someInnerException);

            var expectedActionResult =
                new ActionResult<AssociationSuggestionResult>(expectedBadRequestObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.UpsertAssociationAsync(someAssociation, cancellationToken))
                    .ThrowsAsync(associationOrchestrationDependencyValidationException);

            // when
            ActionResult<AssociationSuggestionResult> actualActionResult =
                await this.associationsController.PostAssociationAsync(someAssociation, cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.UpsertAssociationAsync(someAssociation, cancellationToken),
                    Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
