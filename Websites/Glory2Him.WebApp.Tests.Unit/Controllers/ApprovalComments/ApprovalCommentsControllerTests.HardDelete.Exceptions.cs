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
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ApprovalComments
{
    public partial class ApprovalCommentsControllerTests
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
                new ActionResult<ApprovalComment>(expectedBadRequestObjectResult);

            this.approvalCommentServiceMock.Setup(service =>
                service.HardRemoveApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<ApprovalComment> actualActionResult =
                await this.approvalCommentsController.HardDeleteApprovalCommentByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalCommentServiceMock.Verify(service =>
                service.HardRemoveApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalCommentServiceMock.VerifyNoOtherCalls();
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
                new ActionResult<ApprovalComment>(expectedInternalServerErrorObjectResult);

            this.approvalCommentServiceMock.Setup(service =>
                service.HardRemoveApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serverException);

            // when
            ActionResult<ApprovalComment> actualActionResult =
                await this.approvalCommentsController.HardDeleteApprovalCommentByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalCommentServiceMock.Verify(service =>
                service.HardRemoveApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalCommentServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnHardDeleteIfItemDoesNotExistAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
            string someMessage = GetRandomString();

            var notFoundApprovalCommentException =
                new NotFoundApprovalCommentException(
                    message: someMessage);

            var approvalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: someMessage,
                    innerException: notFoundApprovalCommentException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundApprovalCommentException);

            var expectedActionResult =
                new ActionResult<ApprovalComment>(expectedNotFoundObjectResult);

            this.approvalCommentServiceMock.Setup(service =>
                service.HardRemoveApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalCommentValidationException);

            // when
            ActionResult<ApprovalComment> actualActionResult =
                await this.approvalCommentsController.HardDeleteApprovalCommentByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalCommentServiceMock.Verify(service =>
                service.HardRemoveApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalCommentServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnHardDeleteIfUnauthorizedErrorOccurredAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
            string someMessage = GetRandomString();

            var unauthorizedApprovalCommentException =
                new UnauthorizedApprovalCommentException(
                    message: someMessage);

            var approvalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: someMessage,
                    innerException: unauthorizedApprovalCommentException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedApprovalCommentException);

            var expectedActionResult =
                new ActionResult<ApprovalComment>(expectedUnauthorizedObjectResult);

            this.approvalCommentServiceMock.Setup(service =>
                service.HardRemoveApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalCommentValidationException);

            // when
            ActionResult<ApprovalComment> actualActionResult =
                await this.approvalCommentsController.HardDeleteApprovalCommentByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalCommentServiceMock.Verify(service =>
                service.HardRemoveApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalCommentServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnConflictOnHardDeleteIfAlreadyExistsApprovalCommentErrorOccurredAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var alreadyExistsApprovalCommentException =
                new AlreadyExistsApprovalCommentException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var approvalCommentDependencyValidationException =
                new ApprovalCommentDependencyValidationException(
                    message: someMessage,
                    innerException: alreadyExistsApprovalCommentException);

            ConflictObjectResult expectedConflictObjectResult =
                Conflict(alreadyExistsApprovalCommentException);

            var expectedActionResult =
                new ActionResult<ApprovalComment>(expectedConflictObjectResult);

            this.approvalCommentServiceMock.Setup(service =>
                service.HardRemoveApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalCommentDependencyValidationException);

            // when
            ActionResult<ApprovalComment> actualActionResult =
                await this.approvalCommentsController.HardDeleteApprovalCommentByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalCommentServiceMock.Verify(service =>
                service.HardRemoveApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalCommentServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnLockedOnHardDeleteIfRecordIsLockedAsync()
        {
            // given
            Guid someId = Guid.NewGuid();
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var lockedApprovalCommentException =
                new LockedApprovalCommentException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var approvalCommentDependencyValidationException =
                new ApprovalCommentDependencyValidationException(
                    message: someMessage,
                    innerException: lockedApprovalCommentException);

            LockedObjectResult expectedLockedObjectResult =
                Locked(lockedApprovalCommentException);

            var expectedActionResult =
                new ActionResult<ApprovalComment>(expectedLockedObjectResult);

            this.approvalCommentServiceMock.Setup(service =>
                service.HardRemoveApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalCommentDependencyValidationException);

            // when
            ActionResult<ApprovalComment> actualActionResult =
                await this.approvalCommentsController.HardDeleteApprovalCommentByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalCommentServiceMock.Verify(service =>
                service.HardRemoveApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalCommentServiceMock.VerifyNoOtherCalls();
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
                new ActionResult<ApprovalComment>(expectedFailedDependencyObjectResult);

            this.approvalCommentServiceMock.Setup(service =>
                service.HardRemoveApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ActionResult<ApprovalComment> actualActionResult =
                await this.approvalCommentsController.HardDeleteApprovalCommentByIdAsync(someId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalCommentServiceMock.Verify(service =>
                service.HardRemoveApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalCommentServiceMock.VerifyNoOtherCalls();
        }
    }
}
