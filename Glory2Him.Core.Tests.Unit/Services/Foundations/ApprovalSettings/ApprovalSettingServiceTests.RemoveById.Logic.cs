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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    public partial class ApprovalSettingServiceTests
    {
        [Fact]
        public async Task ShouldRemoveApprovalSettingByIdAsync()
        {
            // given
            ApprovalSetting randomApprovalSetting = CreateRandomApprovalSetting();
            randomApprovalSetting.IsDeleted = false;
            ApprovalSetting storageApprovalSetting = randomApprovalSetting;

            ApprovalSetting auditedApprovalSetting = storageApprovalSetting.DeepClone();
            auditedApprovalSetting.IsDeleted = true;

            ApprovalSetting expectedApprovalSetting = auditedApprovalSetting.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    randomApprovalSetting.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedApprovalSetting);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingAsync(auditedApprovalSetting, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApprovalSetting);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingAsync(
                    It.IsAny<EventEnvelope<ApprovalSetting>>(),
                    ApprovalSettingEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSetting>>(
                        new EventPublishResult<ApprovalSetting>()));

            // when
            ApprovalSetting actualApprovalSetting =
                await this.approvalSettingService.RemoveApprovalSettingByIdAsync(
                    randomApprovalSetting.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSetting.Should().BeEquivalentTo(expectedApprovalSetting);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    randomApprovalSetting.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalSettingAsync(auditedApprovalSetting, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalSettingAsync(
                    It.IsAny<EventEnvelope<ApprovalSetting>>(),
                    ApprovalSettingEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingOnRemovingApprovalSettingByIdSubscriptionName),
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
        public async Task ShouldRemoveApprovalSettingByIdWithDeletionReasonAsync()
        {
            // given
            string someDeletionReason = GetRandomString();
            ApprovalSetting randomApprovalSetting = CreateRandomApprovalSetting();
            randomApprovalSetting.IsDeleted = false;
            ApprovalSetting storageApprovalSetting = randomApprovalSetting;

            ApprovalSetting auditedApprovalSetting = storageApprovalSetting.DeepClone();
            auditedApprovalSetting.IsDeleted = true;
            auditedApprovalSetting.DeletionReason = someDeletionReason;

            ApprovalSetting expectedApprovalSetting = auditedApprovalSetting.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    randomApprovalSetting.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedApprovalSetting);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingAsync(auditedApprovalSetting, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApprovalSetting);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingAsync(
                    It.IsAny<EventEnvelope<ApprovalSetting>>(),
                    ApprovalSettingEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSetting>>(
                        new EventPublishResult<ApprovalSetting>()));

            // when
            ApprovalSetting actualApprovalSetting =
                await this.approvalSettingService.RemoveApprovalSettingByIdAsync(
                    randomApprovalSetting.Id,
                    deletionReason: someDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSetting.Should().BeEquivalentTo(expectedApprovalSetting);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    randomApprovalSetting.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalSettingAsync(auditedApprovalSetting, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalSettingAsync(
                    It.IsAny<EventEnvelope<ApprovalSetting>>(),
                    ApprovalSettingEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingOnRemovingApprovalSettingByIdSubscriptionName),
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
            ApprovalSetting alreadyDeletedApprovalSetting = CreateRandomApprovalSetting();
            alreadyDeletedApprovalSetting.IsDeleted = true;
            Guid someApprovalSettingId = alreadyDeletedApprovalSetting.Id;
            ApprovalSetting expectedApprovalSetting = alreadyDeletedApprovalSetting;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(alreadyDeletedApprovalSetting);

            // when
            ApprovalSetting actualApprovalSetting =
                await this.approvalSettingService.RemoveApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualApprovalSetting.Should().BeEquivalentTo(expectedApprovalSetting);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    someApprovalSettingId,
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
