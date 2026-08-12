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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        [Fact]
        public async Task ShouldAddTagAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Tag randomTag = CreateTagFiller(randomDateTimeOffset).Create();
            Tag inputTag = randomTag;
            Tag auditAppliedTag = inputTag.DeepClone();
            Tag storageTag = auditAppliedTag.DeepClone();
            Tag expectedTag = storageTag.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedTag.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertTagAsync(auditAppliedTag, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageTag);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(It.IsAny<EventEnvelope<Tag>>(), TagEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<Tag>>(
                        new EventPublishResult<Tag>()));

            // when
            Tag actualTag =
                await this.tagService.AddTagAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            // then
            actualTag.Should().BeEquivalentTo(expectedTag);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(inputTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertTagAsync(auditAppliedTag, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Tag>>(),
                    TagEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.TagOnAddingTagSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldAddIfApprovalStatusIsSubmittedAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Tag randomTag = CreateTagFiller(randomDateTimeOffset).Create();
            Tag inputTag = randomTag;
            inputTag.ApprovalStatus = ApprovalStatus.Submitted;
            Tag auditAppliedTag = inputTag.DeepClone();
            Tag storageTag = auditAppliedTag.DeepClone();
            Tag expectedTag = storageTag.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedTag.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertTagAsync(auditAppliedTag, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageTag);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(It.IsAny<EventEnvelope<Tag>>(), TagEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<Tag>>(
                        new EventPublishResult<Tag>()));

            // when
            Tag actualTag =
                await this.tagService.AddTagAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            // then
            actualTag.Should().BeEquivalentTo(expectedTag);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(inputTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertTagAsync(auditAppliedTag, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Tag>>(),
                    TagEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.TagOnAddingTagSubscriptionName),
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
