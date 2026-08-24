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
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Reactions
{
    public partial class ReactionsControllerTests
    {
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnGetByIdIfValidationErrorOccurredAsync(Xeption validationException)
        {
            // given
            Guid someId = Guid.NewGuid();

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<Reaction>(expectedBadRequestObjectResult);

            this.reactionServiceMock.Setup(service =>
                service.RetrieveReactionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(validationException);

            // when
            ActionResult<Reaction> actualActionResult =
                await this.reactionsController.GetReactionByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.reactionServiceMock.Verify(service =>
                service.RetrieveReactionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.reactionServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnGetByIdIfServerErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            Guid someId = Guid.NewGuid();

            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(validationException);

            var expectedActionResult =
                new ActionResult<Reaction>(expectedInternalServerErrorObjectResult);

            this.reactionServiceMock.Setup(service =>
                service.RetrieveReactionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(validationException);

            // when
            ActionResult<Reaction> actualActionResult =
                await this.reactionsController.GetReactionByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.reactionServiceMock.Verify(service =>
                service.RetrieveReactionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.reactionServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnGetByIdIfItemDoesNotExistAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
            string someMessage = GetRandomString();

            var notFoundReactionException =
                new NotFoundReactionException(
                    message: someMessage);

            var reactionValidationException =
                new ReactionValidationException(
                    message: someMessage,
                    innerException: notFoundReactionException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundReactionException);

            var expectedActionResult =
                new ActionResult<Reaction>(expectedNotFoundObjectResult);

            this.reactionServiceMock.Setup(service =>
                service.RetrieveReactionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(reactionValidationException);

            // when
            ActionResult<Reaction> actualActionResult =
                await this.reactionsController.GetReactionByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.reactionServiceMock.Verify(service =>
                service.RetrieveReactionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.reactionServiceMock.VerifyNoOtherCalls();
        }
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnGetByIdIfDependencyErrorOccurredAsync(
            Xeption dependencyException)
        {
            // given
            Guid someId = Guid.NewGuid();

            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<Reaction>(expectedFailedDependencyObjectResult);

            this.reactionServiceMock.Setup(service =>
                service.RetrieveReactionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ActionResult<Reaction> actualActionResult =
                await this.reactionsController.GetReactionByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.reactionServiceMock.Verify(service =>
                service.RetrieveReactionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.reactionServiceMock.VerifyNoOtherCalls();
        }
    }
}
