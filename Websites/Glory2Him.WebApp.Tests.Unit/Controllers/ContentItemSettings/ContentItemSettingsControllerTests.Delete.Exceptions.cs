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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.ContentItemSettings.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ContentItemSettings
{
    public partial class ContentItemSettingsControllerTests
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
                new ActionResult<ContentItemSetting>(expectedBadRequestObjectResult);

            this.contentItemSettingServiceMock.Setup(service =>
                service.RemoveContentItemSettingByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<ContentItemSetting> actualActionResult =
                await this.contentItemSettingsController.DeleteContentItemSettingByIdAsync(someId, null, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemSettingServiceMock.Verify(service =>
                service.RemoveContentItemSettingByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
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
                new ActionResult<ContentItemSetting>(expectedInternalServerErrorObjectResult);

            this.contentItemSettingServiceMock.Setup(service =>
                service.RemoveContentItemSettingByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<ContentItemSetting> actualActionResult =
                await this.contentItemSettingsController.DeleteContentItemSettingByIdAsync(someId, null, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemSettingServiceMock.Verify(service =>
                service.RemoveContentItemSettingByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnDeleteIfItemDoesNotExistAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
            string someMessage = GetRandomString();

            var notFoundContentItemSettingException =
                new NotFoundContentItemSettingException(
                    message: someMessage);

            var contentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: someMessage,
                    innerException: notFoundContentItemSettingException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundContentItemSettingException);

            var expectedActionResult =
                new ActionResult<ContentItemSetting>(expectedNotFoundObjectResult);

            this.contentItemSettingServiceMock.Setup(service =>
                service.RemoveContentItemSettingByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(contentItemSettingValidationException);

            // when
            ActionResult<ContentItemSetting> actualActionResult =
                await this.contentItemSettingsController.DeleteContentItemSettingByIdAsync(someId, null, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemSettingServiceMock.Verify(service =>
                service.RemoveContentItemSettingByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnDeleteIfUnauthorizedErrorOccurredAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
            string someMessage = GetRandomString();

            var unauthorizedContentItemSettingException =
                new UnauthorizedContentItemSettingException(
                    message: someMessage);

            var contentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: someMessage,
                    innerException: unauthorizedContentItemSettingException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedContentItemSettingException);

            var expectedActionResult =
                new ActionResult<ContentItemSetting>(expectedUnauthorizedObjectResult);

            this.contentItemSettingServiceMock.Setup(service =>
                service.RemoveContentItemSettingByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(contentItemSettingValidationException);

            // when
            ActionResult<ContentItemSetting> actualActionResult =
                await this.contentItemSettingsController.DeleteContentItemSettingByIdAsync(someId, null, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemSettingServiceMock.Verify(service =>
                service.RemoveContentItemSettingByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnLockedOnDeleteIfRecordIsLockedAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var lockedContentItemSettingException =
                new LockedContentItemSettingException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var contentItemSettingDependencyValidationException =
                new ContentItemSettingDependencyValidationException(
                    message: someMessage,
                    innerException: lockedContentItemSettingException);

            LockedObjectResult expectedConflictObjectResult =
                Locked(lockedContentItemSettingException);

            var expectedActionResult =
                new ActionResult<ContentItemSetting>(expectedConflictObjectResult);

            this.contentItemSettingServiceMock.Setup(service =>
                service.RemoveContentItemSettingByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(contentItemSettingDependencyValidationException);

            // when
            ActionResult<ContentItemSetting> actualActionResult =
                await this.contentItemSettingsController.DeleteContentItemSettingByIdAsync(someId, null, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemSettingServiceMock.Verify(service =>
                service.RemoveContentItemSettingByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
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
                new ActionResult<ContentItemSetting>(expectedFailedDependencyObjectResult);

            this.contentItemSettingServiceMock.Setup(service =>
                service.RemoveContentItemSettingByIdAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ActionResult<ContentItemSetting> actualActionResult =
                await this.contentItemSettingsController.DeleteContentItemSettingByIdAsync(someId, null, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemSettingServiceMock.Verify(service =>
                service.RemoveContentItemSettingByIdAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
        }
        [Fact]
        public async Task ShouldReturnConflictOnDeleteIfAlreadyExistsContentItemSettingErrorOccurredAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var alreadyExistsContentItemSettingException =
                new AlreadyExistsContentItemSettingException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var contentItemSettingDependencyValidationException =
                new ContentItemSettingDependencyValidationException(
                    message: someMessage,
                    innerException: alreadyExistsContentItemSettingException);

            ConflictObjectResult expectedConflictObjectResult =
                Conflict(alreadyExistsContentItemSettingException);

            var expectedActionResult =
                new ActionResult<ContentItemSetting>(expectedConflictObjectResult);

            this.contentItemSettingServiceMock.Setup(service =>
                service.RemoveContentItemSettingByIdAsync(It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(contentItemSettingDependencyValidationException);

            // when
            ActionResult<ContentItemSetting> actualActionResult =
                await this.contentItemSettingsController.DeleteContentItemSettingByIdAsync(someId, null, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemSettingServiceMock.Verify(service =>
                service.RemoveContentItemSettingByIdAsync(It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
        }
    }
}
