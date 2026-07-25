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
        public async Task ShouldModifyApprovalSettingPublisherRoleAsync()
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

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingPublisherRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingPublisherRole>>(),
                    ApprovalSettingPublisherRoleEventOperation.Modified))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSettingPublisherRole>>(
                        new EventPublishResult<ApprovalSettingPublisherRole>()));

            // when
            ApprovalSettingPublisherRole actualApprovalSettingPublisherRole =
                await this.approvalSettingPublisherRoleService.ModifyApprovalSettingPublisherRoleAsync(
                    inputApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingPublisherRole.Should().BeEquivalentTo(expectedApprovalSettingPublisherRole);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(inputApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectApprovalSettingPublisherRoleByIdAsync(
                        auditAppliedApprovalSettingPublisherRole.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                        auditAppliedApprovalSettingPublisherRole,
                        storageApprovalSettingPublisherRole),
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
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName),
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
