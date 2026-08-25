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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Processings.ContentItems.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ContentItems
{
    public partial class ContentItemsControllerTests
    {
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnDeleteIfValidationErrorOccurredAsync(Xeption validationException)
        {
            // given
            Guid someId = Guid.NewGuid();

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<ContentItem>(expectedBadRequestObjectResult);

            this.contentItemProcessingServiceMock.Setup(service =>
                service.RemoveContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<ContentItem> actualActionResult =
                await this.contentItemsController.DeleteContentItemByIdAsync(someId, null, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemProcessingServiceMock.Verify(service =>
                service.RemoveContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnDeleteIfServerErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            Guid someId = Guid.NewGuid();

            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(validationException);

            var expectedActionResult =
                new ActionResult<ContentItem>(expectedInternalServerErrorObjectResult);

            this.contentItemProcessingServiceMock.Setup(service =>
                service.RemoveContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<ContentItem> actualActionResult =
                await this.contentItemsController.DeleteContentItemByIdAsync(someId, null, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemProcessingServiceMock.Verify(service =>
                service.RemoveContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnDeleteIfItemDoesNotExistAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
            string someMessage = GetRandomString();

            var notFoundContentItemProcessingException =
                new NotFoundContentItemProcessingException(
                    message: someMessage);

            var contentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: someMessage,
                    innerException: notFoundContentItemProcessingException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundContentItemProcessingException);

            var expectedActionResult =
                new ActionResult<ContentItem>(expectedNotFoundObjectResult);

            this.contentItemProcessingServiceMock.Setup(service =>
                service.RemoveContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(contentItemProcessingValidationException);

            // when
            ActionResult<ContentItem> actualActionResult =
                await this.contentItemsController.DeleteContentItemByIdAsync(someId, null, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemProcessingServiceMock.Verify(service =>
                service.RemoveContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnDeleteIfUnauthorizedErrorOccurredAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
            string someMessage = GetRandomString();

            var unauthorizedContentItemProcessingException =
                new UnauthorizedContentItemProcessingException(
                    message: someMessage);

            var contentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: someMessage,
                    innerException: unauthorizedContentItemProcessingException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedContentItemProcessingException);

            var expectedActionResult =
                new ActionResult<ContentItem>(expectedUnauthorizedObjectResult);

            this.contentItemProcessingServiceMock.Setup(service =>
                service.RemoveContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(contentItemProcessingValidationException);

            // when
            ActionResult<ContentItem> actualActionResult =
                await this.contentItemsController.DeleteContentItemByIdAsync(someId, null, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemProcessingServiceMock.Verify(service =>
                service.RemoveContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnDeleteIfDependencyErrorOccurredAsync(
            Xeption dependencyException)
        {
            // given
            Guid someId = Guid.NewGuid();

            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<ContentItem>(expectedFailedDependencyObjectResult);

            this.contentItemProcessingServiceMock.Setup(service =>
                service.RemoveContentItemByIdAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ActionResult<ContentItem> actualActionResult =
                await this.contentItemsController.DeleteContentItemByIdAsync(someId, null, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemProcessingServiceMock.Verify(service =>
                service.RemoveContentItemByIdAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }
        [Fact]
        public async Task ShouldReturnConflictOnDeleteIfAlreadyExistsContentItemErrorOccurredAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var alreadyExistsContentItemProcessingException =
                new AlreadyExistsContentItemProcessingException(
                    message: someMessage);

            var contentItemProcessingDependencyValidationException =
                new ContentItemProcessingDependencyValidationException(
                    message: someMessage,
                    innerException: alreadyExistsContentItemProcessingException);

            ConflictObjectResult expectedConflictObjectResult =
                Conflict(alreadyExistsContentItemProcessingException);

            var expectedActionResult =
                new ActionResult<ContentItem>(expectedConflictObjectResult);

            this.contentItemProcessingServiceMock.Setup(service =>
                service.RemoveContentItemByIdAsync(It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(contentItemProcessingDependencyValidationException);

            // when
            ActionResult<ContentItem> actualActionResult =
                await this.contentItemsController.DeleteContentItemByIdAsync(someId, null, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemProcessingServiceMock.Verify(service =>
                service.RemoveContentItemByIdAsync(It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

            this.contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }
    }
}
