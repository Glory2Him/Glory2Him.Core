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
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Associations
{
    public partial class AssociationsControllerTests
    {
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnReactionSummariesIfValidationErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            Guid[] someContentItemIds = CreateRandomContentItemIds();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ContentItemReactionSummary>>(
                    expectedBadRequestObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.RetrieveContentItemReactionSummariesAsync(
                    someContentItemIds,
                    cancellationToken))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<IReadOnlyList<ContentItemReactionSummary>> actualActionResult =
                await this.associationsController.GetReactionSummariesAsync(
                    someContentItemIds,
                    cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.RetrieveContentItemReactionSummariesAsync(
                    someContentItemIds,
                    cancellationToken),
                        Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnFailedDependencyOnReactionSummariesIfDependencyErrorOccurredAsync()
        {
            // given
            Guid[] someContentItemIds = CreateRandomContentItemIds();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            var someInnerException = new Xeption();

            var associationOrchestrationDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: GetRandomString(),
                    innerException: someInnerException);

            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(someInnerException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ContentItemReactionSummary>>(
                    expectedFailedDependencyObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.RetrieveContentItemReactionSummariesAsync(
                    someContentItemIds,
                    cancellationToken))
                        .ThrowsAsync(associationOrchestrationDependencyException);

            // when
            ActionResult<IReadOnlyList<ContentItemReactionSummary>> actualActionResult =
                await this.associationsController.GetReactionSummariesAsync(
                    someContentItemIds,
                    cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.RetrieveContentItemReactionSummariesAsync(
                    someContentItemIds,
                    cancellationToken),
                        Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnInternalServerErrorOnReactionSummariesIfServerErrorOccurredAsync()
        {
            // given
            Guid[] someContentItemIds = CreateRandomContentItemIds();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            var someInnerException = new Xeption();

            var associationOrchestrationServiceException =
                new AssociationOrchestrationServiceException(
                    message: GetRandomString(),
                    innerException: someInnerException);

            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(associationOrchestrationServiceException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ContentItemReactionSummary>>(
                    expectedInternalServerErrorObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.RetrieveContentItemReactionSummariesAsync(
                    someContentItemIds,
                    cancellationToken))
                        .ThrowsAsync(associationOrchestrationServiceException);

            // when
            ActionResult<IReadOnlyList<ContentItemReactionSummary>> actualActionResult =
                await this.associationsController.GetReactionSummariesAsync(
                    someContentItemIds,
                    cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.RetrieveContentItemReactionSummariesAsync(
                    someContentItemIds,
                    cancellationToken),
                        Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
