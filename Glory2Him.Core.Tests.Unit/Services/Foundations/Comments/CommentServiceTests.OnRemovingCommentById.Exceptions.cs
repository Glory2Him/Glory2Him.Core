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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.Comments.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Comments
{
    public partial class CommentServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRemovingCommentByIdEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<Comment> requestEnvelope = CreateRandomCommentRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<Comment>?> onRemovingTask =
                this.commentService.OnRemovingCommentByIdAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onRemovingTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRemovingCommentByIdEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<Comment> requestEnvelope = CreateRandomCommentRequestEnvelope();

            var expectedCommentDependencyException = new CommentDependencyException(
                message: "Comment dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.CommentOnRemovingCommentByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<Comment>?> onRemovingTask =
                this.commentService.OnRemovingCommentByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            CommentDependencyException actualCommentDependencyException =
                await Assert.ThrowsAsync<CommentDependencyException>(
                    onRemovingTask.AsTask);

            // then
            actualCommentDependencyException.Should().BeEquivalentTo(
                expectedCommentDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRemovingCommentByIdEventIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Comment> requestEnvelope = CreateRandomCommentRequestEnvelope();
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
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.CommentOnRemovingCommentByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<Comment>?> onRemovingTask =
                this.commentService.OnRemovingCommentByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            CommentDependencyException actualCommentDependencyException =
                await Assert.ThrowsAsync<CommentDependencyException>(
                    onRemovingTask.AsTask);

            // then
            actualCommentDependencyException.Should().BeEquivalentTo(
                expectedCommentDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.CommentOnRemovingCommentByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnRemovingCommentByIdEventIfSqlErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Comment> requestEnvelope = CreateRandomCommentRequestEnvelope();
            SqlException sqlException = GetSqlException();

            var failedStorageCommentException = new FailedStorageCommentException(
                message: "Failed comment storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedCommentDependencyException = new CommentDependencyException(
                message: "Comment dependency error occurred, contact support.",
                innerException: failedStorageCommentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.CommentOnRemovingCommentByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<EventEnvelope<Comment>?> onRemovingTask =
                this.commentService.OnRemovingCommentByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            CommentDependencyException actualCommentDependencyException =
                await Assert.ThrowsAsync<CommentDependencyException>(
                    onRemovingTask.AsTask);

            // then
            actualCommentDependencyException.Should().BeEquivalentTo(
                expectedCommentDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedCommentDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ModifyDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnRemovingCommentByIdEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<Comment> requestEnvelope = CreateRandomCommentRequestEnvelope();

            var expectedCommentDependencyValidationException = new CommentDependencyValidationException(
                message: "Comment dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.CommentOnRemovingCommentByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<Comment>?> onRemovingTask =
                this.commentService.OnRemovingCommentByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            CommentDependencyValidationException actualCommentDependencyValidationException =
                await Assert.ThrowsAsync<CommentDependencyValidationException>(
                    onRemovingTask.AsTask);

            // then
            actualCommentDependencyValidationException.Should().BeEquivalentTo(
                expectedCommentDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRemovingCommentByIdEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Comment> requestEnvelope = CreateRandomCommentRequestEnvelope();
            var serviceException = new Exception();

            var failedCommentServiceException = new FailedCommentServiceException(
                message: "Failed comment service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedCommentServiceException = new CommentServiceException(
                message: "Comment service error occurred, contact support.",
                innerException: failedCommentServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.CommentOnRemovingCommentByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<Comment>?> onRemovingTask =
                this.commentService.OnRemovingCommentByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            CommentServiceException actualCommentServiceException =
                await Assert.ThrowsAsync<CommentServiceException>(
                    onRemovingTask.AsTask);

            // then
            actualCommentServiceException.Should().BeEquivalentTo(
                expectedCommentServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
