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
        public async Task ShouldAddCommentAndReplyOnAddingCommentEventAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Comment randomComment = CreateCommentFiller(randomDateTimeOffset).Create();
            Comment inputComment = randomComment;
            Comment auditAppliedComment = inputComment.DeepClone();
            Comment storageComment = auditAppliedComment.DeepClone();
            Comment expectedComment = storageComment.DeepClone();

            var requestEnvelope = new EventEnvelope<Comment>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = inputComment,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.CommentOnAddingCommentSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedComment.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertCommentAsync(auditAppliedComment, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageComment);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.eventBrokerMock.Setup(broker =>
                broker.PublishCommentAsync(It.IsAny<EventEnvelope<Comment>>(), CommentEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<Comment>>(
                        new EventPublishResult<Comment>()));

            // when
            EventEnvelope<Comment>? actualReplyEnvelope =
                await this.commentService.OnAddingCommentAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedComment);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.CommentOnAddingCommentSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertCommentAsync(auditAppliedComment, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishCommentAsync(
                    It.IsAny<EventEnvelope<Comment>>(),
                    CommentEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.EventId == requestEnvelope.Metadata.EventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers.CommentOnAddingCommentSubscriptionName),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.CommentOnAddingCommentSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSkipAddAndReplyNullWhenAddingCommentEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<Comment>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Comment { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.CommentOnAddingCommentSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<Comment>? actualReplyEnvelope =
                await this.commentService.OnAddingCommentAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.CommentOnAddingCommentSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
