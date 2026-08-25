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
        public async Task ShouldReturnBadRequestOnPutIfValidationErrorOccurredAsync(Xeption validationException)
        {
            // given
            ContentItem someContentItem = CreateRandomContentItem();

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<ContentItem>(expectedBadRequestObjectResult);

            this.contentItemProcessingServiceMock.Setup(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(validationException);

            // when
            ActionResult<ContentItem> actualActionResult =
                await this.contentItemsController.PutContentItemAsync(someContentItem, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemProcessingServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnPutIfServerErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            ContentItem someContentItem = CreateRandomContentItem();

            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(validationException);

            var expectedActionResult =
                new ActionResult<ContentItem>(expectedInternalServerErrorObjectResult);

            this.contentItemProcessingServiceMock.Setup(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(validationException);

            // when
            ActionResult<ContentItem> actualActionResult =
                await this.contentItemsController.PutContentItemAsync(someContentItem, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemProcessingServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnPutIfItemDoesNotExistAsync()
        {
            // given
            ContentItem someContentItem = CreateRandomContentItem();
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
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(contentItemProcessingValidationException);

            // when
            ActionResult<ContentItem> actualActionResult =
                await this.contentItemsController.PutContentItemAsync(someContentItem, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemProcessingServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnPutIfUnauthorizedErrorOccurredAsync()
        {
            // given
            ContentItem someContentItem = CreateRandomContentItem();
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
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(contentItemProcessingValidationException);

            // when
            ActionResult<ContentItem> actualActionResult =
                await this.contentItemsController.PutContentItemAsync(someContentItem, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemProcessingServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnConflictOnPutIfAlreadyExistsContentItemErrorOccurredAsync()
        {
            // given
            ContentItem someContentItem = CreateRandomContentItem();
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
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(contentItemProcessingDependencyValidationException);

            // when
            ActionResult<ContentItem> actualActionResult =
                await this.contentItemsController.PutContentItemAsync(someContentItem, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemProcessingServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnPutIfDependencyErrorOccurredAsync(
            Xeption dependencyException)
        {
            // given
            ContentItem someContentItem = CreateRandomContentItem();

            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<ContentItem>(expectedFailedDependencyObjectResult);

            this.contentItemProcessingServiceMock.Setup(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ActionResult<ContentItem> actualActionResult =
                await this.contentItemsController.PutContentItemAsync(someContentItem, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemProcessingServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }
    }
}
