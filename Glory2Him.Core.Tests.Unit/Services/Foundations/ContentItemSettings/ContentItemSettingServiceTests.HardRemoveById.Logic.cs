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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemSettings
{
    public partial class ContentItemSettingServiceTests
    {
        [Fact]
        public async Task ShouldHardRemoveContentItemSettingByIdAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ContentItemSetting randomContentItemSetting = CreateRandomContentItemSetting();
            ContentItemSetting storageContentItemSetting = randomContentItemSetting;
            ContentItemSetting expectedContentItemSetting = storageContentItemSetting.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    randomContentItemSetting.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItemSetting);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteContentItemSettingAsync(storageContentItemSetting, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedContentItemSetting);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemSettingAsync(
                    It.IsAny<EventEnvelope<ContentItemSetting>>(),
                    ContentItemSettingEventOperation.HardRemoved))
                    .Returns(new ValueTask<EventPublishResult<ContentItemSetting>>(
                        new EventPublishResult<ContentItemSetting>()));

            // when
            ContentItemSetting actualContentItemSetting =
                await this.contentItemSettingService.HardRemoveContentItemSettingByIdAsync(
                    randomContentItemSetting.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemSetting.Should().BeEquivalentTo(expectedContentItemSetting);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    randomContentItemSetting.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteContentItemSettingAsync(storageContentItemSetting, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemSettingAsync(
                    It.IsAny<EventEnvelope<ContentItemSetting>>(),
                    ContentItemSettingEventOperation.HardRemoved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .ContentItemSettingOnHardRemovingContentItemSettingByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
