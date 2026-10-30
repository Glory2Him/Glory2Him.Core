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

        [Fact]
        public async Task ShouldAddTagUnderAMintedIdAsync()
        {
            // given
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            string contributorUserId = Guid.NewGuid().ToString();
            Tag inputTag = CreateTagFiller(currentDateTime, contributorUserId).Create();

            // a suggestion names the tag and nothing else, so its Id is empty; the new row takes
            // the Id the service mints (§1 rules 1 and 4)
            inputTag.Id = Guid.Empty;
            Guid mintedId = Guid.NewGuid();
            Guid firstProcessedEventId = Guid.NewGuid();
            Guid secondProcessedEventId = Guid.NewGuid();

            Tag expectedInsertedTag = inputTag.DeepClone();
            expectedInsertedTag.Id = mintedId;
            Tag storedTag = expectedInsertedTag.DeepClone();
            Tag insertedTag = null;
            CancellationToken inputCancellationToken = TestContext.Current.CancellationToken;

            // the mint is the identifier broker's first call in the add; the two that follow
            // are the processed-event records written after the insert
            this.identifierBrokerMock.SetupSequence(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(mintedId)
                    .ReturnsAsync(firstProcessedEventId)
                    .ReturnsAsync(secondProcessedEventId);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputTag, this.ambientSecurityContext))
                    .ReturnsAsync((Tag entity, SecurityContext _) => entity);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(contributorUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertTagAsync(
                    It.Is<Tag>(tag => tag.Id == mintedId),
                    inputCancellationToken))
                        .Callback<Tag, CancellationToken>((entity, _) =>
                            insertedTag = entity.DeepClone())
                        .ReturnsAsync(storedTag);

            // when
            Tag actualTag =
                await this.tagService.AddTagAsync(
                    inputTag,
                    inputCancellationToken);

            // then: the row is inserted under the minted Id, and the fact and the returned tag
            // carry it
            insertedTag.Should().BeEquivalentTo(expectedInsertedTag);
            insertedTag.Id.Should().Be(mintedId);
            actualTag.Should().BeSameAs(storedTag);
            actualTag.Id.Should().Be(mintedId);

            // one Id for the new row, and one for each of the two processed-event records
            this.identifierBrokerMock.Verify(broker =>
                    broker.GetIdentifierAsync(),
                Times.Exactly(3));

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(inputTag, this.ambientSecurityContext),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(this.ambientSecurityContext),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertTagAsync(
                        It.Is<Tag>(tag => tag.Id == mintedId),
                        inputCancellationToken),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishTagAsync(
                    It.Is<EventEnvelope<Tag>>(envelope =>
                        envelope.Content == storedTag
                            && envelope.Content.Id == mintedId),
                    TagEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.Id == firstProcessedEventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers.TagOnAddingTagSubscriptionName),
                    inputCancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.Id == secondProcessedEventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers.TagOnAddingTagSubscriptionName),
                    inputCancellationToken),
                Times.Once);

            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
