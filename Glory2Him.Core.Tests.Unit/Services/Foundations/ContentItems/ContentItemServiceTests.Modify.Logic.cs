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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldModifyContentItemAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentItem randomContentItem = CreateRandomModifyContentItem(randomDateTimeOffset, randomUserId);
            ContentItem inputContentItem = randomContentItem;
            ContentItem auditAppliedContentItem = inputContentItem.DeepClone();
            ContentItem storageContentItem = auditAppliedContentItem.DeepClone();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            ContentItem auditPreservedContentItem = auditAppliedContentItem.DeepClone();
            ContentItem updatedContentItem = auditPreservedContentItem.DeepClone();
            ContentItem expectedContentItem = updatedContentItem.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedContentItem);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    auditAppliedContentItem.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedContentItem,
                    storageContentItem))
                        .ReturnsAsync(auditPreservedContentItem);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAsync(auditPreservedContentItem, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedContentItem);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    ContentItemEventOperation.Modified))
                    .Returns(new ValueTask<EventPublishResult<ContentItem>>(
                        new EventPublishResult<ContentItem>()));

            // when
            ContentItem actualContentItem =
                await this.contentItemService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(inputContentItem, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        auditAppliedContentItem.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                        auditAppliedContentItem,
                        storageContentItem),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(auditPreservedContentItem, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Modified),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ContentItemOnModifyingContentItemSubscriptionName),
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
