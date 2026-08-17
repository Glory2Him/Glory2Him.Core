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
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Tags
{
    public partial class TagsControllerTests
    {
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnApproveIfValidationErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            Tag someTag = CreateRandomTag();

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<Tag>(expectedBadRequestObjectResult);

            this.tagServiceMock.Setup(service =>
                service.TransitionTagApprovalAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(validationException);

            // when
            ActionResult<Tag> actualActionResult =
                await this.tagsController.TransitionTagApprovalAsync(someTag, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.tagServiceMock.Verify(service =>
                service.TransitionTagApprovalAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.tagServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnApproveIfServerErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            Tag someTag = CreateRandomTag();

            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(validationException);

            var expectedActionResult =
                new ActionResult<Tag>(expectedInternalServerErrorObjectResult);

            this.tagServiceMock.Setup(service =>
                service.TransitionTagApprovalAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(validationException);

            // when
            ActionResult<Tag> actualActionResult =
                await this.tagsController.TransitionTagApprovalAsync(someTag, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.tagServiceMock.Verify(service =>
                service.TransitionTagApprovalAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.tagServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnApproveIfItemDoesNotExistAsync()
        {
            // given
            Tag someTag = CreateRandomTag();
            string someMessage = GetRandomString();

            var notFoundTagException =
                new NotFoundTagException(
                    message: someMessage);

            var tagValidationException =
                new TagValidationException(
                    message: someMessage,
                    innerException: notFoundTagException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundTagException);

            var expectedActionResult =
                new ActionResult<Tag>(expectedNotFoundObjectResult);

            this.tagServiceMock.Setup(service =>
                service.TransitionTagApprovalAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(tagValidationException);

            // when
            ActionResult<Tag> actualActionResult =
                await this.tagsController.TransitionTagApprovalAsync(someTag, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.tagServiceMock.Verify(service =>
                service.TransitionTagApprovalAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.tagServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnApproveIfUnauthorizedErrorOccurredAsync()
        {
            // given
            Tag someTag = CreateRandomTag();
            string someMessage = GetRandomString();

            var unauthorizedTagException =
                new UnauthorizedTagException(
                    message: someMessage);

            var tagValidationException =
                new TagValidationException(
                    message: someMessage,
                    innerException: unauthorizedTagException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedTagException);

            var expectedActionResult =
                new ActionResult<Tag>(expectedUnauthorizedObjectResult);

            this.tagServiceMock.Setup(service =>
                service.TransitionTagApprovalAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(tagValidationException);

            // when
            ActionResult<Tag> actualActionResult =
                await this.tagsController.TransitionTagApprovalAsync(someTag, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.tagServiceMock.Verify(service =>
                service.TransitionTagApprovalAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.tagServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnLockedOnApproveIfRecordIsLockedAsync()
        {
            // given
            Tag someTag = CreateRandomTag();
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var lockedTagException =
                new LockedTagException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var tagDependencyValidationException =
                new TagDependencyValidationException(
                    message: someMessage,
                    innerException: lockedTagException);

            LockedObjectResult expectedConflictObjectResult =
                Locked(lockedTagException);

            var expectedActionResult =
                new ActionResult<Tag>(expectedConflictObjectResult);

            this.tagServiceMock.Setup(service =>
                service.TransitionTagApprovalAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(tagDependencyValidationException);

            // when
            ActionResult<Tag> actualActionResult =
                await this.tagsController.TransitionTagApprovalAsync(someTag, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.tagServiceMock.Verify(service =>
                service.TransitionTagApprovalAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.tagServiceMock.VerifyNoOtherCalls();
        }
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnApproveIfDependencyErrorOccurredAsync(
            Xeption dependencyException)
        {
            // given
            Tag someTag = CreateRandomTag();

            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<Tag>(expectedFailedDependencyObjectResult);

            this.tagServiceMock.Setup(service =>
                service.TransitionTagApprovalAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ActionResult<Tag> actualActionResult =
                await this.tagsController.TransitionTagApprovalAsync(someTag, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.tagServiceMock.Verify(service =>
                service.TransitionTagApprovalAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.tagServiceMock.VerifyNoOtherCalls();
        }
        [Fact]
        public async Task ShouldReturnConflictOnApproveIfAlreadyExistsTagErrorOccurredAsync()
        {
            // given
            Tag someTag = CreateRandomTag();
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var alreadyExistsTagException =
                new AlreadyExistsTagException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var tagDependencyValidationException =
                new TagDependencyValidationException(
                    message: someMessage,
                    innerException: alreadyExistsTagException);

            ConflictObjectResult expectedConflictObjectResult =
                Conflict(alreadyExistsTagException);

            var expectedActionResult =
                new ActionResult<Tag>(expectedConflictObjectResult);

            this.tagServiceMock.Setup(service =>
                service.TransitionTagApprovalAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(tagDependencyValidationException);

            // when
            ActionResult<Tag> actualActionResult =
                await this.tagsController.TransitionTagApprovalAsync(someTag, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.tagServiceMock.Verify(service =>
                service.TransitionTagApprovalAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.tagServiceMock.VerifyNoOtherCalls();
        }
    }
}
