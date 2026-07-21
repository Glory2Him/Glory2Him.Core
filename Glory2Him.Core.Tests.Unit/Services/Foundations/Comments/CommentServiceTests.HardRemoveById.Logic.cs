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

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Comments
{
    public partial class CommentServiceTests
    {
        [Fact]
        public async Task ShouldHardRemoveCommentByIdAsync()
        {
            // given
            Comment randomComment = CreateRandomComment();
            Comment storageComment = randomComment;
            Comment expectedComment = storageComment.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    randomComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageComment);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteCommentAsync(storageComment, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedComment);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishCommentAsync(
                    It.IsAny<EventEnvelope<Comment>>(),
                    CommentEventOperation.HardRemoved))
                    .Returns(new ValueTask<EventPublishResult<Comment>>(
                        new EventPublishResult<Comment>()));

            // when
            Comment actualComment =
                await this.commentService.HardRemoveCommentByIdAsync(
                    randomComment.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualComment.Should().BeEquivalentTo(expectedComment);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    randomComment.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteCommentAsync(storageComment, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishCommentAsync(
                    It.IsAny<EventEnvelope<Comment>>(),
                    CommentEventOperation.HardRemoved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.CommentOnHardRemovingCommentByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
