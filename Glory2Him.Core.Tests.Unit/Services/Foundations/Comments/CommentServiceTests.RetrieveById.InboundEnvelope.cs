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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.Comments.Exceptions;
using Glory2Him.Core.Models.Securities;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Comments
{
    public partial class CommentServiceTests
    {
        // THE ENDPOINT READ ON THE ASSOCIATION-ADDING EVENT PATH (#631). The ambient-context
        // overload mints its own envelope, so on a substrate delivery it would evaluate this read
        // as nobody, or as whoever published — never necessarily the subject the envelope was
        // signed for. This overload is handed the envelope and must decide as ITS caller.
        [Fact]
        public async Task ShouldRetrieveNonPublicCommentByIdAsTheInboundEnvelopesCallerAsync()
        {
            // given
            Comment randomComment = CreateRandomComment();
            Comment storageComment = randomComment;
            storageComment.IsDeleted = false;
            storageComment.ApprovalStatus = ApprovalStatus.Draft;
            storageComment.IsPublished = false;
            Comment expectedComment = storageComment;

            // the row's owner, carried on the envelope rather than found in the ambient context
            SecurityContext ownerSecurityContext = CreateAuthenticatedSecurityContext();

            // An Association-sourced envelope, because that is what the only production caller
            // holds: the association orchestration passes the ADD REQUEST it is handling.
            var inboundEnvelope = new EventEnvelope<Association>
            {
                Content = new Association { Id = Guid.NewGuid() },
                SecurityContext = ownerSecurityContext,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<Comment>()))
                        .Returns((EventEnvelope<Association> source, Comment content) =>
                            new ValueTask<EventEnvelope<Comment>>(
                                new EventEnvelope<Comment>
                                {
                                    Content = content,
                                    SecurityContext = source.SecurityContext,
                                    Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                                }));

            // THE AMBIENT CALLER IS NOBODY. Had the overload minted its own context, the gate
            // would refuse before the owner test was reached.
            this.ambientSecurityContext = new SecurityContext();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    randomComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageComment);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(ownerSecurityContext))
                    .ReturnsAsync(storageComment.CreatedBy);

            // when
            Comment actualComment =
                await this.commentService.RetrieveCommentByIdAsync(
                    randomComment.Id,
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualComment.Should().BeEquivalentTo(expectedComment);

            // the gate was asked about the ENVELOPE's subject, never the ambient one
            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(ownerSecurityContext),
                Times.Once);

            // chained rather than minted, so causation stays linked and the context is copied
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(
                    inboundEnvelope,
                    It.Is<Comment>(comment => comment.Id == randomComment.Id)),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Comment>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    randomComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A MISS ON THIS OVERLOAD MUST READ AS A MISS. The association orchestration recognises an
        // unresolvable endpoint by its *ValidationException shape; a raw NotFoundCommentException
        // escaping here would reach its dependency clause instead and report "this endpoint does
        // not exist" as a failed dependency.
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdAsTheInboundEnvelopesCallerIfCommentNotFoundAndLogItAsync()
        {
            // given
            Guid someCommentId = Guid.NewGuid();
            Comment nullComment = null;

            var inboundEnvelope = new EventEnvelope<Association>
            {
                Content = new Association { Id = Guid.NewGuid() },
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<Comment>()))
                        .Returns((EventEnvelope<Association> source, Comment content) =>
                            new ValueTask<EventEnvelope<Comment>>(
                                new EventEnvelope<Comment>
                                {
                                    Content = content,
                                    SecurityContext = source.SecurityContext,
                                    Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                                }));

            var notFoundCommentException =
                new NotFoundCommentException(
                    message: $"Comment not found with id: {someCommentId}.");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: notFoundCommentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    someCommentId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(nullComment);

            // when
            ValueTask<Comment> retrieveCommentByIdTask =
                this.commentService.RetrieveCommentByIdAsync(
                    someCommentId,
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    retrieveCommentByIdTask.AsTask);

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

        // CANCELLED BEFORE ANY WORK (tsc-csharp-cp-005). The ambient overload checks the token
        // first; so must this one, ahead of chaining the read envelope.
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRetrieveByIdAsTheInboundEnvelopesCallerIfCancellationRequestedAsync()
        {
            // given
            Guid someCommentId = Guid.NewGuid();
            EventEnvelope<Association> inboundEnvelope = CreateInboundAssociationEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<Comment> retrieveCommentByIdTask =
                this.commentService.RetrieveCommentByIdAsync(
                    someCommentId,
                    inboundEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                retrieveCommentByIdTask.AsTask);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        private static EventEnvelope<Association> CreateInboundAssociationEnvelope() =>
            new EventEnvelope<Association>
            {
                Content = new Association { Id = Guid.NewGuid() },
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveByIdAsTheInboundEnvelopesCallerIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            Guid someCommentId = Guid.NewGuid();
            EventEnvelope<Association> inboundEnvelope = CreateInboundAssociationEnvelope();
            SetupReadEnvelopeChainedFromTheInboundEnvelope();
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
            ValueTask<Comment> retrieveCommentByIdTask =
                this.commentService.RetrieveCommentByIdAsync(
                    someCommentId,
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            CommentDependencyException actualCommentDependencyException =
                await Assert.ThrowsAsync<CommentDependencyException>(
                    retrieveCommentByIdTask.AsTask);

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
        public async Task ShouldThrowCriticalDependencyExceptionOnRetrieveByIdAsTheInboundEnvelopesCallerIfSqlErrorOccursAndLogItAsync()
        {
            // given
            Guid someCommentId = Guid.NewGuid();
            EventEnvelope<Association> inboundEnvelope = CreateInboundAssociationEnvelope();
            SetupReadEnvelopeChainedFromTheInboundEnvelope();
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
            ValueTask<Comment> retrieveCommentByIdTask =
                this.commentService.RetrieveCommentByIdAsync(
                    someCommentId,
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            CommentDependencyException actualCommentDependencyException =
                await Assert.ThrowsAsync<CommentDependencyException>(
                    retrieveCommentByIdTask.AsTask);

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
        public async Task ShouldThrowServiceExceptionOnRetrieveByIdAsTheInboundEnvelopesCallerIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Guid someCommentId = Guid.NewGuid();
            EventEnvelope<Association> inboundEnvelope = CreateInboundAssociationEnvelope();
            SetupReadEnvelopeChainedFromTheInboundEnvelope();
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
            ValueTask<Comment> retrieveCommentByIdTask =
                this.commentService.RetrieveCommentByIdAsync(
                    someCommentId,
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            CommentServiceException actualCommentServiceException =
                await Assert.ThrowsAsync<CommentServiceException>(
                    retrieveCommentByIdTask.AsTask);

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

        // Chains the read envelope off the inbound one, copying its security context forward, as
        // the real broker does.
        private void SetupReadEnvelopeChainedFromTheInboundEnvelope() =>
            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<Comment>()))
                        .Returns((EventEnvelope<Association> source, Comment content) =>
                            new ValueTask<EventEnvelope<Comment>>(
                                new EventEnvelope<Comment>
                                {
                                    Content = content,
                                    SecurityContext = source.SecurityContext,
                                    Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                                }));
    }
}
