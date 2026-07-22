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
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemSettings
{
    public partial class ContentItemSettingServiceTests
    {
        [Fact]
        public async Task ShouldRemoveContentItemSettingByIdAsync()
        {
            // given
            ContentItemSetting randomContentItemSetting = CreateRandomContentItemSetting();
            randomContentItemSetting.IsDeleted = false;
            ContentItemSetting storageContentItemSetting = randomContentItemSetting;

            ContentItemSetting auditedContentItemSetting = storageContentItemSetting.DeepClone();
            auditedContentItemSetting.IsDeleted = true;

            ContentItemSetting expectedContentItemSetting = auditedContentItemSetting.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    randomContentItemSetting.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedContentItemSetting);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemSettingAsync(auditedContentItemSetting, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedContentItemSetting);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemSettingAsync(
                    It.IsAny<EventEnvelope<ContentItemSetting>>(),
                    ContentItemSettingEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ContentItemSetting>>(
                        new EventPublishResult<ContentItemSetting>()));

            // when
            ContentItemSetting actualContentItemSetting =
                await this.contentItemSettingService.RemoveContentItemSettingByIdAsync(
                    randomContentItemSetting.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemSetting.Should().BeEquivalentTo(expectedContentItemSetting);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    randomContentItemSetting.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemSettingAsync(auditedContentItemSetting, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemSettingAsync(
                    It.IsAny<EventEnvelope<ContentItemSetting>>(),
                    ContentItemSettingEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ContentItemSettingOnRemovingContentItemSettingByIdSubscriptionName),
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

        [Fact]
        public async Task ShouldRemoveContentItemSettingByIdWithDeletionReasonAsync()
        {
            // given
            string someDeletionReason = GetRandomString();
            ContentItemSetting randomContentItemSetting = CreateRandomContentItemSetting();
            randomContentItemSetting.IsDeleted = false;
            ContentItemSetting storageContentItemSetting = randomContentItemSetting;

            ContentItemSetting auditedContentItemSetting = storageContentItemSetting.DeepClone();
            auditedContentItemSetting.IsDeleted = true;
            auditedContentItemSetting.DeletionReason = someDeletionReason;

            ContentItemSetting expectedContentItemSetting = auditedContentItemSetting.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    randomContentItemSetting.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedContentItemSetting);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemSettingAsync(auditedContentItemSetting, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedContentItemSetting);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemSettingAsync(
                    It.IsAny<EventEnvelope<ContentItemSetting>>(),
                    ContentItemSettingEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ContentItemSetting>>(
                        new EventPublishResult<ContentItemSetting>()));

            // when
            ContentItemSetting actualContentItemSetting =
                await this.contentItemSettingService.RemoveContentItemSettingByIdAsync(
                    randomContentItemSetting.Id,
                    deletionReason: someDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemSetting.Should().BeEquivalentTo(expectedContentItemSetting);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    randomContentItemSetting.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemSettingAsync(auditedContentItemSetting, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemSettingAsync(
                    It.IsAny<EventEnvelope<ContentItemSetting>>(),
                    ContentItemSettingEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ContentItemSettingOnRemovingContentItemSettingByIdSubscriptionName),
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

        [Fact]
        public async Task ShouldReturnEarlyOnRemoveByIdIfAlreadyDeletedAsync()
        {
            // given
            ContentItemSetting alreadyDeletedContentItemSetting = CreateRandomContentItemSetting();
            alreadyDeletedContentItemSetting.IsDeleted = true;
            Guid someContentItemSettingId = alreadyDeletedContentItemSetting.Id;
            ContentItemSetting expectedContentItemSetting = alreadyDeletedContentItemSetting;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(alreadyDeletedContentItemSetting);

            // when
            ContentItemSetting actualContentItemSetting =
                await this.contentItemSettingService.RemoveContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualContentItemSetting.Should().BeEquivalentTo(expectedContentItemSetting);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
