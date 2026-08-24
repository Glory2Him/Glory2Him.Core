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
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.Comments.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Comments
{
    public partial class CommentsControllerTests
    {
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnApproveIfValidationErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            Comment someComment = CreateRandomComment();

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<Comment>(expectedBadRequestObjectResult);

            this.commentServiceMock.Setup(service =>
                service.TransitionCommentApprovalAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(validationException);

            // when
            ActionResult<Comment> actualActionResult =
                await this.commentsController.TransitionCommentApprovalAsync(someComment, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.commentServiceMock.Verify(service =>
                service.TransitionCommentApprovalAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.commentServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnApproveIfServerErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            Comment someComment = CreateRandomComment();

            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(validationException);

            var expectedActionResult =
                new ActionResult<Comment>(expectedInternalServerErrorObjectResult);

            this.commentServiceMock.Setup(service =>
                service.TransitionCommentApprovalAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(validationException);

            // when
            ActionResult<Comment> actualActionResult =
                await this.commentsController.TransitionCommentApprovalAsync(someComment, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.commentServiceMock.Verify(service =>
                service.TransitionCommentApprovalAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.commentServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnApproveIfItemDoesNotExistAsync()
        {
            // given
            Comment someComment = CreateRandomComment();
            string someMessage = GetRandomString();

            var notFoundCommentException =
                new NotFoundCommentException(
                    message: someMessage);

            var commentValidationException =
                new CommentValidationException(
                    message: someMessage,
                    innerException: notFoundCommentException);

            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(notFoundCommentException);

            var expectedActionResult =
                new ActionResult<Comment>(expectedNotFoundObjectResult);

            this.commentServiceMock.Setup(service =>
                service.TransitionCommentApprovalAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(commentValidationException);

            // when
            ActionResult<Comment> actualActionResult =
                await this.commentsController.TransitionCommentApprovalAsync(someComment, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.commentServiceMock.Verify(service =>
                service.TransitionCommentApprovalAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.commentServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnApproveIfUnauthorizedErrorOccurredAsync()
        {
            // given
            Comment someComment = CreateRandomComment();
            string someMessage = GetRandomString();

            var unauthorizedCommentException =
                new UnauthorizedCommentException(
                    message: someMessage);

            var commentValidationException =
                new CommentValidationException(
                    message: someMessage,
                    innerException: unauthorizedCommentException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedCommentException);

            var expectedActionResult =
                new ActionResult<Comment>(expectedUnauthorizedObjectResult);

            this.commentServiceMock.Setup(service =>
                service.TransitionCommentApprovalAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(commentValidationException);

            // when
            ActionResult<Comment> actualActionResult =
                await this.commentsController.TransitionCommentApprovalAsync(someComment, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.commentServiceMock.Verify(service =>
                service.TransitionCommentApprovalAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.commentServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnLockedOnApproveIfRecordIsLockedAsync()
        {
            // given
            Comment someComment = CreateRandomComment();
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var lockedCommentException =
                new LockedCommentException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var commentDependencyValidationException =
                new CommentDependencyValidationException(
                    message: someMessage,
                    innerException: lockedCommentException);

            LockedObjectResult expectedConflictObjectResult =
                Locked(lockedCommentException);

            var expectedActionResult =
                new ActionResult<Comment>(expectedConflictObjectResult);

            this.commentServiceMock.Setup(service =>
                service.TransitionCommentApprovalAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(commentDependencyValidationException);

            // when
            ActionResult<Comment> actualActionResult =
                await this.commentsController.TransitionCommentApprovalAsync(someComment, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.commentServiceMock.Verify(service =>
                service.TransitionCommentApprovalAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.commentServiceMock.VerifyNoOtherCalls();
        }
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnApproveIfDependencyErrorOccurredAsync(
            Xeption dependencyException)
        {
            // given
            Comment someComment = CreateRandomComment();

            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<Comment>(expectedFailedDependencyObjectResult);

            this.commentServiceMock.Setup(service =>
                service.TransitionCommentApprovalAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ActionResult<Comment> actualActionResult =
                await this.commentsController.TransitionCommentApprovalAsync(someComment, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.commentServiceMock.Verify(service =>
                service.TransitionCommentApprovalAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.commentServiceMock.VerifyNoOtherCalls();
        }
        [Fact]
        public async Task ShouldReturnConflictOnApproveIfAlreadyExistsCommentErrorOccurredAsync()
        {
            // given
            Comment someComment = CreateRandomComment();
            var someInnerException = new Exception();
            string someMessage = GetRandomString();

            var alreadyExistsCommentException =
                new AlreadyExistsCommentException(
                    message: someMessage,
                    innerException: someInnerException,
                    data: someInnerException.Data);

            var commentDependencyValidationException =
                new CommentDependencyValidationException(
                    message: someMessage,
                    innerException: alreadyExistsCommentException);

            ConflictObjectResult expectedConflictObjectResult =
                Conflict(alreadyExistsCommentException);

            var expectedActionResult =
                new ActionResult<Comment>(expectedConflictObjectResult);

            this.commentServiceMock.Setup(service =>
                service.TransitionCommentApprovalAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(commentDependencyValidationException);

            // when
            ActionResult<Comment> actualActionResult =
                await this.commentsController.TransitionCommentApprovalAsync(someComment, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.commentServiceMock.Verify(service =>
                service.TransitionCommentApprovalAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.commentServiceMock.VerifyNoOtherCalls();
        }
    }
}
