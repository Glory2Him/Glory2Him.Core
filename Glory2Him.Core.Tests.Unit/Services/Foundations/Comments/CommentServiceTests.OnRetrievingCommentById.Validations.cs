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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.Comments.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Comments
{
    public partial class CommentServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrievingCommentByIdEventWhenEnvelopeIsInvalidAsync()
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
            ValueTask<EventEnvelope<Comment>?> onRetrieveTask =
                this.commentService.OnRetrievingCommentByIdAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    onRetrieveTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPassThroughNotFoundValidationExceptionOnRetrievingCommentByIdEventAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<Comment>
            {
                Content = new Comment { Id = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    requestEnvelope.Content.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync((Comment?)null);

            // when
            ValueTask<EventEnvelope<Comment>?> onRetrieveTask =
                this.commentService.OnRetrievingCommentByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    onRetrieveTask.AsTask);

            // then: the nested retrieve's categorized exception surfaces unwrapped —
            // the substrate wrapper must not double-wrap it.
            actualCommentValidationException.InnerException
                .Should().BeOfType<NotFoundCommentException>();

            this.eventBrokerMock.VerifyNoOtherCalls();
        }
    }
}
