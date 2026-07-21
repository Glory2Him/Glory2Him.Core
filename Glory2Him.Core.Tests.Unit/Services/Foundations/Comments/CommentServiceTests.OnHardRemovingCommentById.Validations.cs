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
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Comments
{
    public partial class CommentServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingCommentByIdEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<Comment>? nullEnvelope = null;

            var invalidCommentEventException =
                new InvalidCommentEventException(
                    message: "Invalid comment event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentEventException);

            // when
            ValueTask<EventEnvelope<Comment>?> onHardRemovingTask =
                this.commentService.OnHardRemovingCommentByIdAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    onHardRemovingTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingCommentByIdEventWhenIdIsInvalidAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<Comment>
            {
                Content = new Comment { Id = Guid.Empty },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var invalidCommentException = new InvalidCommentException(
                message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.UpsertDataList(
                key: nameof(Comment.Id),
                value: "Id is required");

            var expectedCommentValidationException = new CommentValidationException(
                message: "Comment validation error occurred, fix the errors and try again.",
                innerException: invalidCommentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.CommentOnHardRemovingCommentByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            // when
            ValueTask<EventEnvelope<Comment>?> onHardRemovingTask =
                this.commentService.OnHardRemovingCommentByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    onHardRemovingTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.CommentOnHardRemovingCommentByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingCommentByIdEventWhenCommentNotFoundAsync()
        {
            // given
            Guid someCommentId = Guid.NewGuid();
            Comment noComment = null!;

            var requestEnvelope = new EventEnvelope<Comment>
            {
                Content = new Comment { Id = someCommentId },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var notFoundCommentException = new NotFoundCommentException(
                message: $"Comment not found with id: {someCommentId}.");

            var expectedCommentValidationException = new CommentValidationException(
                message: "Comment validation error occurred, fix the errors and try again.",
                innerException: notFoundCommentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.CommentOnHardRemovingCommentByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    someCommentId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noComment);

            // when
            ValueTask<EventEnvelope<Comment>?> onHardRemovingTask =
                this.commentService.OnHardRemovingCommentByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    onHardRemovingTask.AsTask);

            // then: the raw not-found from the shared do-work is categorized the same way
            // the non-event path categorizes it — the event path must not degrade it to a
            // service exception.
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    someCommentId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
