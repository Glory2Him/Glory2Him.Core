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
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingRoles
{
    public partial class ApprovalSettingRoleServiceTests
    {
        [Fact]
        public async Task ShouldHardRemoveApprovalSettingRoleByIdAsync()
        {
            // given
            ApprovalSettingRole randomApprovalSettingRole = CreateRandomApprovalSettingRole();
            ApprovalSettingRole storageApprovalSettingRole = randomApprovalSettingRole;
            ApprovalSettingRole expectedApprovalSettingRole = storageApprovalSettingRole.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    randomApprovalSettingRole.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSettingRole);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteApprovalSettingRoleAsync(storageApprovalSettingRole, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApprovalSettingRole);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingRole>>(),
                    ApprovalSettingRoleEventOperation.HardRemoved))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSettingRole>>(
                        new EventPublishResult<ApprovalSettingRole>()));

            // when
            ApprovalSettingRole actualApprovalSettingRole =
                await this.approvalSettingRoleService.HardRemoveApprovalSettingRoleByIdAsync(
                    randomApprovalSettingRole.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingRole.Should().BeEquivalentTo(expectedApprovalSettingRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    randomApprovalSettingRole.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteApprovalSettingRoleAsync(storageApprovalSettingRole, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalSettingRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingRole>>(),
                    ApprovalSettingRoleEventOperation.HardRemoved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingRoleOnHardRemovingApprovalSettingRoleByIdSubscriptionName),
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
