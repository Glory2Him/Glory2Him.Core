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
        public async Task ShouldReturnBadRequestOnPutIfValidationErrorOccurredAsync(Xeption validationException)
        {
            // given
            Reaction someReaction = CreateRandomReaction();

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<Reaction>(expectedBadRequestObjectResult);

            this.reactionServiceMock.Setup(service =>
                service.ModifyReactionAsync(It.IsAny<Reaction>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(validationException);

            // when
            ActionResult<Reaction> actualActionResult =
                await this.reactionsController.PutReactionAsync(someReaction, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.reactionServiceMock.Verify(service =>
                service.ModifyReactionAsync(It.IsAny<Reaction>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.reactionServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnPutIfServerErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            Reaction someReaction = CreateRandomReaction();

            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(validationException);

            var expectedActionResult =
                new ActionResult<Reaction>(expectedInternalServerErrorObjectResult);

            this.reactionServiceMock.Setup(service =>
                service.ModifyReactionAsync(It.IsAny<Reaction>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(validationException);

            // when
            ActionResult<Reaction> actualActionResult =
                await this.reactionsController.PutReactionAsync(someReaction, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.reactionServiceMock.Verify(service =>
                service.ModifyReactionAsync(It.IsAny<Reaction>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.reactionServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnPutIfItemDoesNotExistAsync()
        {
            // given
            Reaction someReaction = CreateRandomReaction();
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
                service.ModifyReactionAsync(It.IsAny<Reaction>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(reactionValidationException);

            // when
            ActionResult<Reaction> actualActionResult =
                await this.reactionsController.PutReactionAsync(someReaction, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.reactionServiceMock.Verify(service =>
                service.ModifyReactionAsync(It.IsAny<Reaction>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.reactionServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnPutIfUnauthorizedErrorOccurredAsync()
        {
            // given
            Reaction someReaction = CreateRandomReaction();
            string someMessage = GetRandomString();

            var unauthorizedReactionException =
                new UnauthorizedReactionException(
                    message: someMessage);

            var reactionValidationException =
                new ReactionValidationException(
                    message: someMessage,
                    innerException: unauthorizedReactionException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedReactionException);

            var expectedActionResult =
                new ActionResult<Reaction>(expectedUnauthorizedObjectResult);

            this.reactionServiceMock.Setup(service =>
                service.ModifyReactionAsync(It.IsAny<Reaction>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(reactionValidationException);

            // when
            ActionResult<Reaction> actualActionResult =
                await this.reactionsController.PutReactionAsync(someReaction, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.reactionServiceMock.Verify(service =>
                service.ModifyReactionAsync(It.IsAny<Reaction>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.reactionServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnConflictOnPutIfAlreadyExistsReactionErrorOccurredAsync()
        {
            // given
            Reaction someReaction = CreateRandomReaction();
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var alreadyExistsReactionException =
                new AlreadyExistsReactionException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var reactionDependencyValidationException =
                new ReactionDependencyValidationException(
                    message: someMessage,
                    innerException: alreadyExistsReactionException);

            ConflictObjectResult expectedConflictObjectResult =
                Conflict(alreadyExistsReactionException);

            var expectedActionResult =
                new ActionResult<Reaction>(expectedConflictObjectResult);

            this.reactionServiceMock.Setup(service =>
                service.ModifyReactionAsync(It.IsAny<Reaction>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(reactionDependencyValidationException);

            // when
            ActionResult<Reaction> actualActionResult =
                await this.reactionsController.PutReactionAsync(someReaction, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.reactionServiceMock.Verify(service =>
                service.ModifyReactionAsync(It.IsAny<Reaction>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.reactionServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnLockedOnPutIfRecordIsLockedAsync()
        {
            // given
            Reaction someReaction = CreateRandomReaction();
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var lockedReactionException =
                new LockedReactionException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var reactionDependencyValidationException =
                new ReactionDependencyValidationException(
                    message: someMessage,
                    innerException: lockedReactionException);

            LockedObjectResult expectedConflictObjectResult =
                Locked(lockedReactionException);

            var expectedActionResult =
                new ActionResult<Reaction>(expectedConflictObjectResult);

            this.reactionServiceMock.Setup(service =>
                service.ModifyReactionAsync(It.IsAny<Reaction>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(reactionDependencyValidationException);

            // when
            ActionResult<Reaction> actualActionResult =
                await this.reactionsController.PutReactionAsync(someReaction, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.reactionServiceMock.Verify(service =>
                service.ModifyReactionAsync(It.IsAny<Reaction>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.reactionServiceMock.VerifyNoOtherCalls();
        }
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnPutIfDependencyErrorOccurredAsync(
            Xeption dependencyException)
        {
            // given
            Reaction someReaction = CreateRandomReaction();

            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<Reaction>(expectedFailedDependencyObjectResult);

            this.reactionServiceMock.Setup(service =>
                service.ModifyReactionAsync(It.IsAny<Reaction>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ActionResult<Reaction> actualActionResult =
                await this.reactionsController.PutReactionAsync(someReaction, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.reactionServiceMock.Verify(service =>
                service.ModifyReactionAsync(It.IsAny<Reaction>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.reactionServiceMock.VerifyNoOtherCalls();
        }
    }
}
