// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems;
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
                broker.GetUserIdAsync())
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputContentItem))
                    .ReturnsAsync(auditAppliedContentItem);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    auditAppliedContentItem.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedContentItem,
                    storageContentItem))
                        .ReturnsAsync(auditPreservedContentItem);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAsync(auditPreservedContentItem, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedContentItem);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAsync(It.IsAny<EventEnvelope<ContentItem>>(), "ContentItemModified"))
                    .Returns(ValueTask.CompletedTask);

            // when
            ContentItem actualContentItem =
                await this.contentItemService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(inputContentItem),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        auditAppliedContentItem.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                        auditAppliedContentItem,
                        storageContentItem),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(auditPreservedContentItem, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(It.IsAny<EventEnvelope<ContentItem>>(), "ContentItemModified"),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
