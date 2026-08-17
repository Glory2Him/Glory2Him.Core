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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ApprovalReviews
{
    public partial class ApprovalReviewsControllerTests
    {
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnPutIfValidationErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            ApprovalReview someApprovalReview = CreateRandomApprovalReview();

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<ApprovalReview>(expectedBadRequestObjectResult);

            this.approvalReviewServiceMock.Setup(service =>
                service.ModifyApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<ApprovalReview> actualActionResult =
                await this.approvalReviewsController.PutApprovalReviewAsync(someApprovalReview, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewServiceMock.Verify(service =>
                service.ModifyApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnPutIfServerErrorOccurredAsync(
            Xeption serverException)
        {
            // given
            ApprovalReview someApprovalReview = CreateRandomApprovalReview();

            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(serverException);

            var expectedActionResult =
                new ActionResult<ApprovalReview>(expectedInternalServerErrorObjectResult);

            this.approvalReviewServiceMock.Setup(service =>
                service.ModifyApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serverException);

            // when
            ActionResult<ApprovalReview> actualActionResult =
                await this.approvalReviewsController.PutApprovalReviewAsync(someApprovalReview, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewServiceMock.Verify(service =>
                service.ModifyApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnPutIfItemDoesNotExistAsync()
        {
            // given
            ApprovalReview someApprovalReview = CreateRandomApprovalReview();
            string someMessage = GetRandomString();

            var notFoundApprovalReviewException =
                new NotFoundApprovalReviewException(
                    message: someMessage);

            var approvalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: someMessage,
                    innerException: notFoundApprovalReviewException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundApprovalReviewException);

            var expectedActionResult =
                new ActionResult<ApprovalReview>(expectedNotFoundObjectResult);

            this.approvalReviewServiceMock.Setup(service =>
                service.ModifyApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalReviewValidationException);

            // when
            ActionResult<ApprovalReview> actualActionResult =
                await this.approvalReviewsController.PutApprovalReviewAsync(someApprovalReview, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewServiceMock.Verify(service =>
                service.ModifyApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnPutIfUnauthorizedErrorOccurredAsync()
        {
            // given
            ApprovalReview someApprovalReview = CreateRandomApprovalReview();
            string someMessage = GetRandomString();

            var unauthorizedApprovalReviewException =
                new UnauthorizedApprovalReviewException(
                    message: someMessage);

            var approvalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: someMessage,
                    innerException: unauthorizedApprovalReviewException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedApprovalReviewException);

            var expectedActionResult =
                new ActionResult<ApprovalReview>(expectedUnauthorizedObjectResult);

            this.approvalReviewServiceMock.Setup(service =>
                service.ModifyApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalReviewValidationException);

            // when
            ActionResult<ApprovalReview> actualActionResult =
                await this.approvalReviewsController.PutApprovalReviewAsync(someApprovalReview, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewServiceMock.Verify(service =>
                service.ModifyApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnConflictOnPutIfAlreadyExistsApprovalReviewErrorOccurredAsync()
        {
            // given
            ApprovalReview someApprovalReview = CreateRandomApprovalReview();
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var alreadyExistsApprovalReviewException =
                new AlreadyExistsApprovalReviewException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var approvalReviewDependencyValidationException =
                new ApprovalReviewDependencyValidationException(
                    message: someMessage,
                    innerException: alreadyExistsApprovalReviewException);

            ConflictObjectResult expectedConflictObjectResult =
                Conflict(alreadyExistsApprovalReviewException);

            var expectedActionResult =
                new ActionResult<ApprovalReview>(expectedConflictObjectResult);

            this.approvalReviewServiceMock.Setup(service =>
                service.ModifyApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalReviewDependencyValidationException);

            // when
            ActionResult<ApprovalReview> actualActionResult =
                await this.approvalReviewsController.PutApprovalReviewAsync(someApprovalReview, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewServiceMock.Verify(service =>
                service.ModifyApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnLockedOnPutIfRecordIsLockedAsync()
        {
            // given
            ApprovalReview someApprovalReview = CreateRandomApprovalReview();
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var lockedApprovalReviewException =
                new LockedApprovalReviewException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var approvalReviewDependencyValidationException =
                new ApprovalReviewDependencyValidationException(
                    message: someMessage,
                    innerException: lockedApprovalReviewException);

            LockedObjectResult expectedLockedObjectResult =
                Locked(lockedApprovalReviewException);

            var expectedActionResult =
                new ActionResult<ApprovalReview>(expectedLockedObjectResult);

            this.approvalReviewServiceMock.Setup(service =>
                service.ModifyApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalReviewDependencyValidationException);

            // when
            ActionResult<ApprovalReview> actualActionResult =
                await this.approvalReviewsController.PutApprovalReviewAsync(someApprovalReview, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewServiceMock.Verify(service =>
                service.ModifyApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnPutIfDependencyErrorOccurredAsync(
            Xeption dependencyException)
        {
            // given
            ApprovalReview someApprovalReview = CreateRandomApprovalReview();

            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<ApprovalReview>(expectedFailedDependencyObjectResult);

            this.approvalReviewServiceMock.Setup(service =>
                service.ModifyApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ActionResult<ApprovalReview> actualActionResult =
                await this.approvalReviewsController.PutApprovalReviewAsync(someApprovalReview, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewServiceMock.Verify(service =>
                service.ModifyApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewServiceMock.VerifyNoOtherCalls();
        }
    }
}
