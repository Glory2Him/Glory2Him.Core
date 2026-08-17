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
        public async Task ShouldReturnBadRequestOnHardDeleteIfValidationErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            Guid someId = Guid.NewGuid();

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<ApprovalReview>(expectedBadRequestObjectResult);

            this.approvalReviewServiceMock.Setup(service =>
                service.HardRemoveApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<ApprovalReview> actualActionResult =
                await this.approvalReviewsController.HardDeleteApprovalReviewByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewServiceMock.Verify(service =>
                service.HardRemoveApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnHardDeleteIfServerErrorOccurredAsync(
            Xeption serverException)
        {
            // given
            Guid someId = Guid.NewGuid();

            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(serverException);

            var expectedActionResult =
                new ActionResult<ApprovalReview>(expectedInternalServerErrorObjectResult);

            this.approvalReviewServiceMock.Setup(service =>
                service.HardRemoveApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serverException);

            // when
            ActionResult<ApprovalReview> actualActionResult =
                await this.approvalReviewsController.HardDeleteApprovalReviewByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewServiceMock.Verify(service =>
                service.HardRemoveApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnHardDeleteIfItemDoesNotExistAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
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
                service.HardRemoveApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalReviewValidationException);

            // when
            ActionResult<ApprovalReview> actualActionResult =
                await this.approvalReviewsController.HardDeleteApprovalReviewByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewServiceMock.Verify(service =>
                service.HardRemoveApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnHardDeleteIfUnauthorizedErrorOccurredAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
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
                service.HardRemoveApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalReviewValidationException);

            // when
            ActionResult<ApprovalReview> actualActionResult =
                await this.approvalReviewsController.HardDeleteApprovalReviewByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewServiceMock.Verify(service =>
                service.HardRemoveApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnConflictOnHardDeleteIfAlreadyExistsApprovalReviewErrorOccurredAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
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
                service.HardRemoveApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalReviewDependencyValidationException);

            // when
            ActionResult<ApprovalReview> actualActionResult =
                await this.approvalReviewsController.HardDeleteApprovalReviewByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewServiceMock.Verify(service =>
                service.HardRemoveApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnLockedOnHardDeleteIfRecordIsLockedAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
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
                service.HardRemoveApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalReviewDependencyValidationException);

            // when
            ActionResult<ApprovalReview> actualActionResult =
                await this.approvalReviewsController.HardDeleteApprovalReviewByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewServiceMock.Verify(service =>
                service.HardRemoveApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnHardDeleteIfDependencyErrorOccurredAsync(
            Xeption dependencyException)
        {
            // given
            Guid someId = Guid.NewGuid();

            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<ApprovalReview>(expectedFailedDependencyObjectResult);

            this.approvalReviewServiceMock.Setup(service =>
                service.HardRemoveApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ActionResult<ApprovalReview> actualActionResult =
                await this.approvalReviewsController.HardDeleteApprovalReviewByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalReviewServiceMock.Verify(service =>
                service.HardRemoveApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalReviewServiceMock.VerifyNoOtherCalls();
        }
    }
}
