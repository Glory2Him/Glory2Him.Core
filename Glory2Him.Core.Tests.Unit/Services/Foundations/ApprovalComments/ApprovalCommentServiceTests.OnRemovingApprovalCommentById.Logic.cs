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
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalComments
{
    public partial class ApprovalCommentServiceTests
    {
        [Fact]
        public async Task ShouldRemoveApprovalCommentByIdAndReplyOnRemovingApprovalCommentByIdEventAsync()
        {
            // given
            string randomDeletionReason = GetRandomString();
            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            storageApprovalComment.IsDeleted = false;
            ApprovalComment auditedApprovalComment = storageApprovalComment.DeepClone();
            ApprovalComment removedApprovalComment = auditedApprovalComment.DeepClone();
            ApprovalComment expectedApprovalComment = removedApprovalComment.DeepClone();

            var requestEnvelope = new EventEnvelope<ApprovalComment>
            {
                Content = new ApprovalComment
                {
                    Id = storageApprovalComment.Id,
                    DeletionReason = randomDeletionReason
                },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalCommentOnRemovingApprovalCommentByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedApprovalComment);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalCommentAsync(auditedApprovalComment, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(removedApprovalComment);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    ApprovalCommentEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ApprovalComment>>(
                        new EventPublishResult<ApprovalComment>()));

            // when
            EventEnvelope<ApprovalComment>? actualReplyEnvelope =
                await this.approvalCommentService.OnRemovingApprovalCommentByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalComment);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalCommentOnRemovingApprovalCommentByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalCommentAsync(auditedApprovalComment, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    ApprovalCommentEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.EventId == requestEnvelope.Metadata.EventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers.ApprovalCommentOnRemovingApprovalCommentByIdSubscriptionName),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalCommentOnRemovingApprovalCommentByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSkipRemoveAndReplyNullWhenRemovingApprovalCommentByIdEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ApprovalComment>
            {
                Content = new ApprovalComment { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalCommentOnRemovingApprovalCommentByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ApprovalComment>? actualReplyEnvelope =
                await this.approvalCommentService.OnRemovingApprovalCommentByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalCommentOnRemovingApprovalCommentByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReplyWithExistingApprovalCommentOnRemovingApprovalCommentByIdEventWhenAlreadyDeletedAsync()
        {
            // given
            ApprovalComment alreadyDeletedApprovalComment = CreateRandomApprovalComment();
            alreadyDeletedApprovalComment.IsDeleted = true;
            ApprovalComment expectedApprovalComment = alreadyDeletedApprovalComment.DeepClone();

            var requestEnvelope = new EventEnvelope<ApprovalComment>
            {
                Content = new ApprovalComment { Id = alreadyDeletedApprovalComment.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalCommentOnRemovingApprovalCommentByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    alreadyDeletedApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(alreadyDeletedApprovalComment);

            // when
            EventEnvelope<ApprovalComment>? actualReplyEnvelope =
                await this.approvalCommentService.OnRemovingApprovalCommentByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: already deleted — no mutation happened, so no fact is published and
            // nothing is recorded as processed; the existing entity is returned as the reply.
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalComment);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalCommentOnRemovingApprovalCommentByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    alreadyDeletedApprovalComment.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
