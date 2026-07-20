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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldHardRemoveContentItemByIdAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem storageContentItem = randomContentItem;
            ContentItem expectedContentItem = storageContentItem.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    randomContentItem.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteContentItemAsync(storageContentItem, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedContentItem);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    ContentItemEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ContentItem>>(
                        new EventPublishResult<ContentItem>()));

            // when
            ContentItem actualContentItem =
                await this.contentItemService.HardRemoveContentItemByIdAsync(
                    randomContentItem.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(
                    randomContentItem.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteContentItemAsync(storageContentItem, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    ContentItemEventOperation.Removed),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
