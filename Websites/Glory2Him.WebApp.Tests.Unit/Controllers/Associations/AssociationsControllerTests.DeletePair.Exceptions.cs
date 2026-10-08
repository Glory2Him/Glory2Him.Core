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

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Associations
{
    public partial class AssociationsControllerTests
    {
        [Fact]
        public async Task ShouldReturnUnauthorizedOnDeletePairIfUnauthorizedErrorOccurredAsync()
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

            this.associationOrchestrationServiceMock.Setup(service =>
                service.RemoveAssociationByPairAsync(someAssociation, cancellationToken))
                    .ThrowsAsync(associationOrchestrationValidationException);

            // when
            ActionResult actualActionResult =
                await this.associationsController.DeleteAssociationPairAsync(someAssociation, cancellationToken);

            // then
            actualActionResult.Should().BeOfType<UnauthorizedObjectResult>()
                .Which.Should().BeEquivalentTo(expectedUnauthorizedObjectResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.RemoveAssociationByPairAsync(someAssociation, cancellationToken),
                    Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnDeletePairIfAnEndpointIsNotFoundAsync()
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

            this.associationOrchestrationServiceMock.Setup(service =>
                service.RemoveAssociationByPairAsync(someAssociation, cancellationToken))
                    .ThrowsAsync(associationOrchestrationValidationException);

            // when
            ActionResult actualActionResult =
                await this.associationsController.DeleteAssociationPairAsync(someAssociation, cancellationToken);

            // then
            actualActionResult.Should().BeOfType<NotFoundObjectResult>()
                .Which.Should().BeEquivalentTo(expectedNotFoundObjectResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.RemoveAssociationByPairAsync(someAssociation, cancellationToken),
                    Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnDeletePairIfValidationErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            Association someAssociation = CreateRandomAssociation();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.RemoveAssociationByPairAsync(someAssociation, cancellationToken))
                    .ThrowsAsync(validationException);

            // when
            ActionResult actualActionResult =
                await this.associationsController.DeleteAssociationPairAsync(someAssociation, cancellationToken);

            // then
            actualActionResult.Should().BeOfType<BadRequestObjectResult>()
                .Which.Should().BeEquivalentTo(expectedBadRequestObjectResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.RemoveAssociationByPairAsync(someAssociation, cancellationToken),
                    Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnFailedDependencyOnDeletePairIfDependencyErrorOccurredAsync()
        {
            // given
            Association someAssociation = CreateRandomAssociation();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            var someInnerException = new Xeption();

            var associationOrchestrationDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: GetRandomString(),
                    innerException: someInnerException);

            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(someInnerException);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.RemoveAssociationByPairAsync(someAssociation, cancellationToken))
                    .ThrowsAsync(associationOrchestrationDependencyException);

            // when
            ActionResult actualActionResult =
                await this.associationsController.DeleteAssociationPairAsync(someAssociation, cancellationToken);

            // then
            actualActionResult.Should().BeOfType<FailedDependencyObjectResult>()
                .Which.Should().BeEquivalentTo(expectedFailedDependencyObjectResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.RemoveAssociationByPairAsync(someAssociation, cancellationToken),
                    Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
