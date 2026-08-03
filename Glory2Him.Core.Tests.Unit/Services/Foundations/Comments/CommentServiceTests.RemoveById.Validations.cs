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
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.Comments.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Comments
{
    public partial class CommentServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidCommentId = Guid.Empty;

            var invalidCommentException = new InvalidCommentException(
                message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.UpsertDataList(
                key: nameof(Comment.Id),
                value: "Id is required");

            var expectedCommentValidationException = new CommentValidationException(
                message: "Comment validation error occurred, fix the errors and try again.",
                innerException: invalidCommentException);

            // when
            ValueTask<Comment> removeCommentByIdTask =
                this.commentService.RemoveCommentByIdAsync(
                    invalidCommentId,
                    cancellationToken: TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    removeCommentByIdTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfCommentNotFoundAndLogItAsync()
        {
            // given
            Guid someCommentId = Guid.NewGuid();
            Comment noComment = null;

            var notFoundCommentException = new NotFoundCommentException(
                message: $"Comment not found with id: {someCommentId}.");

            var expectedCommentValidationException = new CommentValidationException(
                message: "Comment validation error occurred, fix the errors and try again.",
                innerException: notFoundCommentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    someCommentId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noComment);

            // when
            ValueTask<Comment> removeCommentByIdTask =
                this.commentService.RemoveCommentByIdAsync(
                    someCommentId,
                    cancellationToken: TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    removeCommentByIdTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    someCommentId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            Guid someCommentId = Guid.NewGuid();

            var unauthorizedCommentException = new UnauthorizedCommentException(
                message: "The current user is not authenticated.");

            var expectedCommentValidationException = new CommentValidationException(
                message: "Comment validation error occurred, fix the errors and try again.",
                innerException: unauthorizedCommentException);

            // when
            ValueTask<Comment> removeCommentByIdTask =
                this.commentService.RemoveCommentByIdAsync(
                    someCommentId,
                    cancellationToken: TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    removeCommentByIdTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.CommentReadOnly)]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserIsBlockedFromContributingAndLogItAsync(
            string blockedRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(blockedRole);
            Guid someCommentId = Guid.NewGuid();

            var unauthorizedCommentException = new UnauthorizedCommentException(
                message: "The current user is blocked from contributing comments.");

            var expectedCommentValidationException = new CommentValidationException(
                message: "Comment validation error occurred, fix the errors and try again.",
                innerException: unauthorizedCommentException);

            // when
            ValueTask<Comment> removeCommentByIdTask =
                this.commentService.RemoveCommentByIdAsync(
                    someCommentId,
                    cancellationToken: TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    removeCommentByIdTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserIsNotOwnerAndNotAdminAndLogItAsync()
        {
            // given
            string randomActorUserId = GetRandomString();
            Comment storageComment = CreateRandomComment();
            Guid someCommentId = storageComment.Id;

            var unauthorizedCommentException = new UnauthorizedCommentException(
                message: "The current user is not allowed to remove this comment.");

            var expectedCommentValidationException = new CommentValidationException(
                message: "Comment validation error occurred, fix the errors and try again.",
                innerException: unauthorizedCommentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    someCommentId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ValueTask<Comment> removeCommentByIdTask =
                this.commentService.RemoveCommentByIdAsync(
                    someCommentId,
                    cancellationToken: TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    removeCommentByIdTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    someCommentId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateCommentAsync(
                    It.IsAny<Comment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
