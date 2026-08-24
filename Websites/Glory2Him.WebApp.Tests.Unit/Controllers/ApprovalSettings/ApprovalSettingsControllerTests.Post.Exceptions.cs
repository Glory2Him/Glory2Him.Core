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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ApprovalSettings
{
    public partial class ApprovalSettingsControllerTests
    {
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnPostIfValidationErrorOccurredAsync(Xeption validationException)
        {
            // given
            ApprovalSetting someApprovalSetting = CreateRandomApprovalSetting();

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<ApprovalSetting>(expectedBadRequestObjectResult);

            this.approvalSettingServiceMock.Setup(service =>
                service.AddApprovalSettingAsync(It.IsAny<ApprovalSetting>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(validationException);

            // when
            ActionResult<ApprovalSetting> actualActionResult =
                await this.approvalSettingsController.PostApprovalSettingAsync(someApprovalSetting, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalSettingServiceMock.Verify(service =>
                service.AddApprovalSettingAsync(It.IsAny<ApprovalSetting>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.approvalSettingServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnPostIfServerErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            ApprovalSetting someApprovalSetting = CreateRandomApprovalSetting();

            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(validationException);

            var expectedActionResult =
                new ActionResult<ApprovalSetting>(expectedInternalServerErrorObjectResult);

            this.approvalSettingServiceMock.Setup(service =>
                service.AddApprovalSettingAsync(It.IsAny<ApprovalSetting>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(validationException);

            // when
            ActionResult<ApprovalSetting> actualActionResult =
                await this.approvalSettingsController.PostApprovalSettingAsync(someApprovalSetting, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalSettingServiceMock.Verify(service =>
                service.AddApprovalSettingAsync(It.IsAny<ApprovalSetting>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.approvalSettingServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfUnauthorizedErrorOccurredAsync()
        {
            // given
            ApprovalSetting someApprovalSetting = CreateRandomApprovalSetting();
            string someMessage = GetRandomString();

            var unauthorizedApprovalSettingException =
                new UnauthorizedApprovalSettingException(
                    message: someMessage);

            var approvalSettingValidationException =
                new ApprovalSettingValidationException(
                    message: someMessage,
                    innerException: unauthorizedApprovalSettingException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedApprovalSettingException);

            var expectedActionResult =
                new ActionResult<ApprovalSetting>(expectedUnauthorizedObjectResult);

            this.approvalSettingServiceMock.Setup(service =>
                service.AddApprovalSettingAsync(It.IsAny<ApprovalSetting>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(approvalSettingValidationException);

            // when
            ActionResult<ApprovalSetting> actualActionResult =
                await this.approvalSettingsController.PostApprovalSettingAsync(someApprovalSetting, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalSettingServiceMock.Verify(service =>
                service.AddApprovalSettingAsync(It.IsAny<ApprovalSetting>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.approvalSettingServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnConflictOnPostIfAlreadyExistsApprovalSettingErrorOccurredAsync()
        {
            // given
            ApprovalSetting someApprovalSetting = CreateRandomApprovalSetting();
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var alreadyExistsApprovalSettingException =
                new AlreadyExistsApprovalSettingException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var approvalSettingDependencyValidationException =
                new ApprovalSettingDependencyValidationException(
                    message: someMessage,
                    innerException: alreadyExistsApprovalSettingException);

            ConflictObjectResult expectedConflictObjectResult =
                Conflict(alreadyExistsApprovalSettingException);

            var expectedActionResult =
                new ActionResult<ApprovalSetting>(expectedConflictObjectResult);

            this.approvalSettingServiceMock.Setup(service =>
                service.AddApprovalSettingAsync(It.IsAny<ApprovalSetting>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(approvalSettingDependencyValidationException);

            // when
            ActionResult<ApprovalSetting> actualActionResult =
                await this.approvalSettingsController.PostApprovalSettingAsync(someApprovalSetting, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalSettingServiceMock.Verify(service =>
                service.AddApprovalSettingAsync(It.IsAny<ApprovalSetting>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.approvalSettingServiceMock.VerifyNoOtherCalls();
        }
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnPostIfDependencyErrorOccurredAsync(
            Xeption dependencyException)
        {
            // given
            ApprovalSetting someApprovalSetting = CreateRandomApprovalSetting();

            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<ApprovalSetting>(expectedFailedDependencyObjectResult);

            this.approvalSettingServiceMock.Setup(service =>
                service.AddApprovalSettingAsync(It.IsAny<ApprovalSetting>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ActionResult<ApprovalSetting> actualActionResult =
                await this.approvalSettingsController.PostApprovalSettingAsync(someApprovalSetting, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalSettingServiceMock.Verify(service =>
                service.AddApprovalSettingAsync(It.IsAny<ApprovalSetting>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.approvalSettingServiceMock.VerifyNoOtherCalls();
        }
    }
}
