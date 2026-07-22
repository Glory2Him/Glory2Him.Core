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
        public async Task ShouldRemoveApprovalSettingByIdAndReplyOnRemovingApprovalSettingByIdEventAsync()
        {
            // given
            string randomDeletionReason = GetRandomString();
            ApprovalSetting storageApprovalSetting = CreateRandomApprovalSetting();
            storageApprovalSetting.IsDeleted = false;
            ApprovalSetting auditedApprovalSetting = storageApprovalSetting.DeepClone();
            ApprovalSetting removedApprovalSetting = auditedApprovalSetting.DeepClone();
            ApprovalSetting expectedApprovalSetting = removedApprovalSetting.DeepClone();

            var requestEnvelope = new EventEnvelope<ApprovalSetting>
            {
                Content = new ApprovalSetting
                {
                    Id = storageApprovalSetting.Id,
                    DeletionReason = randomDeletionReason
                },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingOnRemovingApprovalSettingByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    storageApprovalSetting.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedApprovalSetting);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingAsync(auditedApprovalSetting, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(removedApprovalSetting);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingAsync(
                    It.IsAny<EventEnvelope<ApprovalSetting>>(),
                    ApprovalSettingEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSetting>>(
                        new EventPublishResult<ApprovalSetting>()));

            // when
            EventEnvelope<ApprovalSetting>? actualReplyEnvelope =
                await this.approvalSettingService.OnRemovingApprovalSettingByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalSetting);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingOnRemovingApprovalSettingByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    storageApprovalSetting.Id,
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
                        processedEvent.EventId == requestEnvelope.Metadata.EventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers.ApprovalSettingOnRemovingApprovalSettingByIdSubscriptionName),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingOnRemovingApprovalSettingByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSkipRemoveAndReplyNullWhenRemovingApprovalSettingByIdEventAlreadyProcessedAsync()
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
                    EventBrokerIdentifiers.ApprovalSettingOnRemovingApprovalSettingByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ApprovalSetting>? actualReplyEnvelope =
                await this.approvalSettingService.OnRemovingApprovalSettingByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingOnRemovingApprovalSettingByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReplyWithExistingApprovalSettingOnRemovingApprovalSettingByIdEventWhenAlreadyDeletedAsync()
        {
            // given
            ApprovalSetting alreadyDeletedApprovalSetting = CreateRandomApprovalSetting();
            alreadyDeletedApprovalSetting.IsDeleted = true;
            ApprovalSetting expectedApprovalSetting = alreadyDeletedApprovalSetting.DeepClone();

            var requestEnvelope = new EventEnvelope<ApprovalSetting>
            {
                Content = new ApprovalSetting { Id = alreadyDeletedApprovalSetting.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingOnRemovingApprovalSettingByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    alreadyDeletedApprovalSetting.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(alreadyDeletedApprovalSetting);

            // when
            EventEnvelope<ApprovalSetting>? actualReplyEnvelope =
                await this.approvalSettingService.OnRemovingApprovalSettingByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: already deleted — no mutation happened, so no fact is published and
            // nothing is recorded as processed; the existing entity is returned as the reply.
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalSetting);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingOnRemovingApprovalSettingByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    alreadyDeletedApprovalSetting.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
