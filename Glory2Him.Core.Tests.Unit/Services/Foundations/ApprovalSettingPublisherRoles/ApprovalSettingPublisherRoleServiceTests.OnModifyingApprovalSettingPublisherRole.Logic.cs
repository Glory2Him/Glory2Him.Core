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
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingPublisherRoles
{
    public partial class ApprovalSettingPublisherRoleServiceTests
    {
        [Fact]
        public async Task ShouldModifyApprovalSettingPublisherRoleAndReplyOnModifyingApprovalSettingPublisherRoleEventAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSettingPublisherRole randomApprovalSettingPublisherRole = CreateRandomModifyApprovalSettingPublisherRole(randomDateTimeOffset, randomUserId);
            ApprovalSettingPublisherRole inputApprovalSettingPublisherRole = randomApprovalSettingPublisherRole;
            ApprovalSettingPublisherRole auditAppliedApprovalSettingPublisherRole = inputApprovalSettingPublisherRole.DeepClone();
            ApprovalSettingPublisherRole storageApprovalSettingPublisherRole = auditAppliedApprovalSettingPublisherRole.DeepClone();
            storageApprovalSettingPublisherRole.UpdatedWhen = storageApprovalSettingPublisherRole.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            ApprovalSettingPublisherRole auditPreservedApprovalSettingPublisherRole = auditAppliedApprovalSettingPublisherRole.DeepClone();
            ApprovalSettingPublisherRole updatedApprovalSettingPublisherRole = auditPreservedApprovalSettingPublisherRole.DeepClone();
            ApprovalSettingPublisherRole expectedApprovalSettingPublisherRole = updatedApprovalSettingPublisherRole.DeepClone();

            var requestEnvelope = new EventEnvelope<ApprovalSettingPublisherRole>
            {
                Content = inputApprovalSettingPublisherRole,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedApprovalSettingPublisherRole);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    auditAppliedApprovalSettingPublisherRole.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSettingPublisherRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedApprovalSettingPublisherRole,
                    storageApprovalSettingPublisherRole))
                        .ReturnsAsync(auditPreservedApprovalSettingPublisherRole);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingPublisherRoleAsync(auditPreservedApprovalSettingPublisherRole, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedApprovalSettingPublisherRole);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingPublisherRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingPublisherRole>>(),
                    ApprovalSettingPublisherRoleEventOperation.Modified))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSettingPublisherRole>>(
                        new EventPublishResult<ApprovalSettingPublisherRole>()));

            // when
            EventEnvelope<ApprovalSettingPublisherRole>? actualReplyEnvelope =
                await this.approvalSettingPublisherRoleService.OnModifyingApprovalSettingPublisherRoleAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalSettingPublisherRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    auditAppliedApprovalSettingPublisherRole.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalSettingPublisherRoleAsync(auditPreservedApprovalSettingPublisherRole, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalSettingPublisherRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingPublisherRole>>(),
                    ApprovalSettingPublisherRoleEventOperation.Modified),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.EventId == requestEnvelope.Metadata.EventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSkipModifyAndReplyNullWhenModifyingApprovalSettingPublisherRoleEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ApprovalSettingPublisherRole>
            {
                Content = new ApprovalSettingPublisherRole { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ApprovalSettingPublisherRole>? actualReplyEnvelope =
                await this.approvalSettingPublisherRoleService.OnModifyingApprovalSettingPublisherRoleAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
