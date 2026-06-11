// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
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
        public async Task ShouldRemoveContentItemByIdAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            randomContentItem.IsDeleted = false;
            ContentItem storageContentItem = randomContentItem;

            ContentItem auditedContentItem = storageContentItem.DeepClone();
            auditedContentItem.IsDeleted = true;

            ContentItem expectedContentItem = auditedContentItem.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    randomContentItem.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageContentItem))
                    .ReturnsAsync(auditedContentItem);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAsync(auditedContentItem, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedContentItem);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAsync(It.IsAny<EventEnvelope<ContentItem>>(), "ContentItemRemoved"))
                    .Returns(ValueTask.CompletedTask);

            // when
            ContentItem actualContentItem =
                await this.contentItemService.RemoveContentItemByIdAsync(
                    randomContentItem.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(
                    randomContentItem.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageContentItem),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemAsync(auditedContentItem, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemAsync(It.IsAny<EventEnvelope<ContentItem>>(), "ContentItemRemoved"),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRemoveContentItemByIdWithDeletionReasonAsync()
        {
            // given
            string someDeletionReason = GetRandomString();
            ContentItem randomContentItem = CreateRandomContentItem();
            randomContentItem.IsDeleted = false;
            ContentItem storageContentItem = randomContentItem;

            ContentItem auditedContentItem = storageContentItem.DeepClone();
            auditedContentItem.IsDeleted = true;
            auditedContentItem.DeletionReason = someDeletionReason;

            ContentItem expectedContentItem = auditedContentItem.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    randomContentItem.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageContentItem))
                    .ReturnsAsync(auditedContentItem);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAsync(auditedContentItem, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedContentItem);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAsync(It.IsAny<EventEnvelope<ContentItem>>(), "ContentItemRemoved"))
                    .Returns(ValueTask.CompletedTask);

            // when
            ContentItem actualContentItem =
                await this.contentItemService.RemoveContentItemByIdAsync(
                    randomContentItem.Id,
                    deletionReason: someDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(
                    randomContentItem.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageContentItem),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemAsync(auditedContentItem, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemAsync(It.IsAny<EventEnvelope<ContentItem>>(), "ContentItemRemoved"),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
