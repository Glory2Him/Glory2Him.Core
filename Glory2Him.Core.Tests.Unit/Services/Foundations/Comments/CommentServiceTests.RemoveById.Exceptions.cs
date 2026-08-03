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
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Comments
{
    public partial class CommentServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRemoveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            Guid someCommentId = Guid.NewGuid();

            var expectedCommentDependencyException = new CommentDependencyException(
                message: "Comment dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    someCommentId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<Comment> removeCommentByIdTask =
                this.commentService.RemoveCommentByIdAsync(
                    someCommentId,
                    cancellationToken: TestContext.Current.CancellationToken);

            CommentDependencyException actualCommentDependencyException =
                await Assert.ThrowsAsync<CommentDependencyException>(
                    removeCommentByIdTask.AsTask);

            // then
            actualCommentDependencyException.Should().BeEquivalentTo(
                expectedCommentDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    someCommentId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRemoveByIdIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            Guid someCommentId = Guid.NewGuid();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutCommentException =
                new TimeoutCommentException(
                    message: "Failed comment timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedCommentDependencyException = new CommentDependencyException(
                message: "Comment dependency error occurred, contact support.",
                innerException: timeoutCommentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    someCommentId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<Comment> removeCommentByIdTask =
                this.commentService.RemoveCommentByIdAsync(
                    someCommentId,
                    cancellationToken: TestContext.Current.CancellationToken);

            CommentDependencyException actualCommentDependencyException =
                await Assert.ThrowsAsync<CommentDependencyException>(
                    removeCommentByIdTask.AsTask);

            // then
            actualCommentDependencyException.Should().BeEquivalentTo(
                expectedCommentDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    someCommentId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRemoveByIdIfCancellationRequestedAsync()
        {
            // given
            Guid someCommentId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<Comment> removeCommentByIdTask =
                this.commentService.RemoveCommentByIdAsync(
                    someCommentId,
                    cancellationToken: cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                removeCommentByIdTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnRemoveByIdIfSqlErrorOccursAndLogItAsync()
        {
            // given
            Guid someCommentId = Guid.NewGuid();
            SqlException sqlException = GetSqlException();

            var failedStorageCommentException = new FailedStorageCommentException(
                message: "Failed comment storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedCommentDependencyException = new CommentDependencyException(
                message: "Comment dependency error occurred, contact support.",
                innerException: failedStorageCommentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    someCommentId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<Comment> removeCommentByIdTask =
                this.commentService.RemoveCommentByIdAsync(
                    someCommentId,
                    cancellationToken: TestContext.Current.CancellationToken);

            CommentDependencyException actualCommentDependencyException =
                await Assert.ThrowsAsync<CommentDependencyException>(
                    removeCommentByIdTask.AsTask);

            // then
            actualCommentDependencyException.Should().BeEquivalentTo(
                expectedCommentDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    someCommentId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedCommentDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyValidationExceptionOnRemoveByIdIfDbUpdateConcurrencyExceptionOccursAndLogItAsync()
        {
            // given
            Guid someCommentId = Guid.NewGuid();
            Comment someComment = CreateRandomComment();
            someComment.IsDeleted = false;
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();

            var lockedCommentException = new LockedCommentException(
                message: "Locked comment record, please try again later.",
                innerException: dbUpdateConcurrencyException,
                data: dbUpdateConcurrencyException.Data);

            var expectedCommentDependencyValidationException = new CommentDependencyValidationException(
                message: "Comment dependency validation error occurred, fix the errors and try again.",
                innerException: lockedCommentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    someCommentId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(someComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(someComment.CreatedBy);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(someComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(someComment);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateCommentAsync(
                    someComment,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(dbUpdateConcurrencyException);

            // when
            ValueTask<Comment> removeCommentByIdTask =
                this.commentService.RemoveCommentByIdAsync(
                    someCommentId,
                    cancellationToken: TestContext.Current.CancellationToken);

            CommentDependencyValidationException actualCommentDependencyValidationException =
                await Assert.ThrowsAsync<CommentDependencyValidationException>(
                    removeCommentByIdTask.AsTask);

            // then
            actualCommentDependencyValidationException.Should().BeEquivalentTo(
                expectedCommentDependencyValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    someCommentId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(someComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateCommentAsync(
                    someComment,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRemoveByIdIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Guid someCommentId = Guid.NewGuid();
            var serviceException = new Exception();

            var failedCommentServiceException = new FailedCommentServiceException(
                message: "Failed comment service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedCommentServiceException = new CommentServiceException(
                message: "Comment service error occurred, contact support.",
                innerException: failedCommentServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    someCommentId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<Comment> removeCommentByIdTask =
                this.commentService.RemoveCommentByIdAsync(
                    someCommentId,
                    cancellationToken: TestContext.Current.CancellationToken);

            CommentServiceException actualCommentServiceException =
                await Assert.ThrowsAsync<CommentServiceException>(
                    removeCommentByIdTask.AsTask);

            // then
            actualCommentServiceException.Should().BeEquivalentTo(
                expectedCommentServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    someCommentId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
