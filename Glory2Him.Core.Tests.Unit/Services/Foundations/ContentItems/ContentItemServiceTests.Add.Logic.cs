// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

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
        public async Task ShouldAddContentItemAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            ContentItem auditAppliedContentItem = inputContentItem.DeepClone();
            ContentItem storageContentItem = auditAppliedContentItem.DeepClone();
            ContentItem expectedContentItem = storageContentItem.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputContentItem))
                    .ReturnsAsync(auditAppliedContentItem);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertContentItemAsync(auditAppliedContentItem, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAsync(It.IsAny<EventEnvelope<ContentItem>>(), "ContentItemAdded"))
                    .Returns(ValueTask.CompletedTask);

            // when
            ContentItem actualContentItem =
                await this.contentItemService.AddContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(inputContentItem),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertContentItemAsync(auditAppliedContentItem, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    "ContentItemAdded"),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
