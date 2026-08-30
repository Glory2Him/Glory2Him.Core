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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemSettings
{
    public partial class ContentItemSettingServiceTests
    {
        [Fact]
        public async Task ShouldAddContentItemSettingAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentItemSetting randomContentItemSetting = CreateContentItemSettingFiller(randomDateTimeOffset).Create();
            ContentItemSetting inputContentItemSetting = randomContentItemSetting;
            ContentItemSetting auditAppliedContentItemSetting = inputContentItemSetting.DeepClone();
            ContentItemSetting storageContentItemSetting = auditAppliedContentItemSetting.DeepClone();
            ContentItemSetting expectedContentItemSetting = storageContentItemSetting.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedContentItemSetting.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertContentItemSettingAsync(auditAppliedContentItemSetting, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItemSetting);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemSettingAsync(
                    It.IsAny<EventEnvelope<ContentItemSetting>>(),
                    ContentItemSettingEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<ContentItemSetting>>(
                        new EventPublishResult<ContentItemSetting>()));

            // when
            ContentItemSetting actualContentItemSetting =
                await this.contentItemSettingService.AddContentItemSettingAsync(
                    inputContentItemSetting,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemSetting.Should().BeEquivalentTo(expectedContentItemSetting);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(inputContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertContentItemSettingAsync(auditAppliedContentItemSetting, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemSettingAsync(
                    It.IsAny<EventEnvelope<ContentItemSetting>>(),
                    ContentItemSettingEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ContentItemSettingOnAddingContentItemSettingSubscriptionName),
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
