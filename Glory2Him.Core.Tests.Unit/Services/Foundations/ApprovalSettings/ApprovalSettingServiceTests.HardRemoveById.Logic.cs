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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    public partial class ApprovalSettingServiceTests
    {
        [Fact]
        public async Task ShouldHardRemoveApprovalSettingByIdAsync()
        {
            // given
            ApprovalSetting randomApprovalSetting = CreateRandomApprovalSetting();
            ApprovalSetting storageApprovalSetting = randomApprovalSetting;
            ApprovalSetting expectedApprovalSetting = storageApprovalSetting.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    randomApprovalSetting.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSetting);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteApprovalSettingAsync(storageApprovalSetting, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApprovalSetting);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingAsync(
                    It.IsAny<EventEnvelope<ApprovalSetting>>(),
                    ApprovalSettingEventOperation.HardRemoved))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSetting>>(
                        new EventPublishResult<ApprovalSetting>()));

            // when
            ApprovalSetting actualApprovalSetting =
                await this.approvalSettingService.HardRemoveApprovalSettingByIdAsync(
                    randomApprovalSetting.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSetting.Should().BeEquivalentTo(expectedApprovalSetting);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    randomApprovalSetting.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteApprovalSettingAsync(storageApprovalSetting, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalSettingAsync(
                    It.IsAny<EventEnvelope<ApprovalSetting>>(),
                    ApprovalSettingEventOperation.HardRemoved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingOnHardRemovingApprovalSettingByIdSubscriptionName),
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
