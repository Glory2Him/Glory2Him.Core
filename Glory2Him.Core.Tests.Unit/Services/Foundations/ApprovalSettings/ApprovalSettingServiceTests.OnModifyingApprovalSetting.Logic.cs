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
        public async Task ShouldModifyApprovalSettingAndReplyOnModifyingApprovalSettingEventAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSetting randomApprovalSetting =
                CreateRandomModifyApprovalSetting(randomDateTimeOffset, randomUserId);
            ApprovalSetting inputApprovalSetting = randomApprovalSetting;
            ApprovalSetting auditAppliedApprovalSetting = inputApprovalSetting.DeepClone();
            ApprovalSetting storageApprovalSetting = auditAppliedApprovalSetting.DeepClone();
            storageApprovalSetting.UpdatedWhen = storageApprovalSetting.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            ApprovalSetting auditPreservedApprovalSetting = auditAppliedApprovalSetting.DeepClone();
            ApprovalSetting updatedApprovalSetting = auditPreservedApprovalSetting.DeepClone();
            ApprovalSetting expectedApprovalSetting = updatedApprovalSetting.DeepClone();

            var requestEnvelope = new EventEnvelope<ApprovalSetting>
            {
                Content = inputApprovalSetting,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingOnModifyingApprovalSettingSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedApprovalSetting);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    auditAppliedApprovalSetting.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedApprovalSetting,
                    storageApprovalSetting))
                        .ReturnsAsync(auditPreservedApprovalSetting);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingAsync(auditPreservedApprovalSetting, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedApprovalSetting);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingAsync(
                    It.IsAny<EventEnvelope<ApprovalSetting>>(),
                    ApprovalSettingEventOperation.Modified))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSetting>>(
                        new EventPublishResult<ApprovalSetting>()));

            // when
            EventEnvelope<ApprovalSetting>? actualReplyEnvelope =
                await this.approvalSettingService.OnModifyingApprovalSettingAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalSetting);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingOnModifyingApprovalSettingSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    auditAppliedApprovalSetting.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalSettingAsync(auditPreservedApprovalSetting, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalSettingAsync(
                    It.IsAny<EventEnvelope<ApprovalSetting>>(),
                    ApprovalSettingEventOperation.Modified),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.EventId == requestEnvelope.Metadata.EventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers.ApprovalSettingOnModifyingApprovalSettingSubscriptionName),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingOnModifyingApprovalSettingSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSkipModifyAndReplyNullWhenModifyingApprovalSettingEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ApprovalSetting>
            {
                Content = new ApprovalSetting { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingOnModifyingApprovalSettingSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ApprovalSetting>? actualReplyEnvelope =
                await this.approvalSettingService.OnModifyingApprovalSettingAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingOnModifyingApprovalSettingSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
