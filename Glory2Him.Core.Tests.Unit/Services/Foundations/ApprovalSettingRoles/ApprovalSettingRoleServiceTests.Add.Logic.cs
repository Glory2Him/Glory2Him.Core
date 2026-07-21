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
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingRoles
{
    public partial class ApprovalSettingRoleServiceTests
    {
        [Fact]
        public async Task ShouldAddApprovalSettingRoleAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSettingRole randomApprovalSettingRole = CreateApprovalSettingRoleFiller(randomDateTimeOffset).Create();
            ApprovalSettingRole inputApprovalSettingRole = randomApprovalSettingRole;
            ApprovalSettingRole auditAppliedApprovalSettingRole = inputApprovalSettingRole.DeepClone();
            ApprovalSettingRole storageApprovalSettingRole = auditAppliedApprovalSettingRole.DeepClone();
            ApprovalSettingRole expectedApprovalSettingRole = storageApprovalSettingRole.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputApprovalSettingRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedApprovalSettingRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedApprovalSettingRole.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertApprovalSettingRoleAsync(auditAppliedApprovalSettingRole, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovalSettingRole);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingRoleAsync(It.IsAny<EventEnvelope<ApprovalSettingRole>>(), ApprovalSettingRoleEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSettingRole>>(
                        new EventPublishResult<ApprovalSettingRole>()));

            // when
            ApprovalSettingRole actualApprovalSettingRole =
                await this.approvalSettingRoleService.AddApprovalSettingRoleAsync(
                    inputApprovalSettingRole,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingRole.Should().BeEquivalentTo(expectedApprovalSettingRole);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(inputApprovalSettingRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertApprovalSettingRoleAsync(auditAppliedApprovalSettingRole, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalSettingRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingRole>>(),
                    ApprovalSettingRoleEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingRoleOnAddingApprovalSettingRoleSubscriptionName),
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
