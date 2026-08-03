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
        public async Task ShouldModifyCommentAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Comment randomComment = CreateRandomModifyComment(randomDateTimeOffset, randomUserId);
            Comment inputComment = randomComment;
            Comment auditAppliedComment = inputComment.DeepClone();
            Comment storageComment = auditAppliedComment.DeepClone();
            storageComment.UpdatedWhen = storageComment.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            Comment auditPreservedComment = auditAppliedComment.DeepClone();
            Comment updatedComment = auditPreservedComment.DeepClone();
            Comment expectedComment = updatedComment.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedComment);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    auditAppliedComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedComment,
                    storageComment))
                        .ReturnsAsync(auditPreservedComment);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateCommentAsync(auditPreservedComment, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedComment);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishCommentAsync(
                    It.IsAny<EventEnvelope<Comment>>(),
                    CommentEventOperation.Modified))
                    .Returns(new ValueTask<EventPublishResult<Comment>>(
                        new EventPublishResult<Comment>()));

            // when
            Comment actualComment =
                await this.commentService.ModifyCommentAsync(
                    inputComment,
                    TestContext.Current.CancellationToken);

            // then
            actualComment.Should().BeEquivalentTo(expectedComment);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(inputComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectCommentByIdAsync(
                        auditAppliedComment.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                        auditAppliedComment,
                        storageComment),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateCommentAsync(auditPreservedComment, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishCommentAsync(
                        It.IsAny<EventEnvelope<Comment>>(),
                        CommentEventOperation.Modified),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.CommentOnModifyingCommentSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
