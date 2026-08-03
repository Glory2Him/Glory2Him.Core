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
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingPublisherRoles
{
    public partial class ApprovalSettingPublisherRoleServiceTests
    {
        [Fact]
        public async Task ShouldHardRemoveApprovalSettingPublisherRoleByIdAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ApprovalSettingPublisherRole randomApprovalSettingPublisherRole = CreateRandomApprovalSettingPublisherRole();
            ApprovalSettingPublisherRole storageApprovalSettingPublisherRole = randomApprovalSettingPublisherRole;
            ApprovalSettingPublisherRole expectedApprovalSettingPublisherRole = storageApprovalSettingPublisherRole.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    randomApprovalSettingPublisherRole.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSettingPublisherRole);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteApprovalSettingPublisherRoleAsync(storageApprovalSettingPublisherRole, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApprovalSettingPublisherRole);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingPublisherRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingPublisherRole>>(),
                    ApprovalSettingPublisherRoleEventOperation.HardRemoved))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSettingPublisherRole>>(
                        new EventPublishResult<ApprovalSettingPublisherRole>()));

            // when
            ApprovalSettingPublisherRole actualApprovalSettingPublisherRole =
                await this.approvalSettingPublisherRoleService.HardRemoveApprovalSettingPublisherRoleByIdAsync(
                    randomApprovalSettingPublisherRole.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingPublisherRole.Should().BeEquivalentTo(expectedApprovalSettingPublisherRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    randomApprovalSettingPublisherRole.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteApprovalSettingPublisherRoleAsync(storageApprovalSettingPublisherRole, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalSettingPublisherRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingPublisherRole>>(),
                    ApprovalSettingPublisherRoleEventOperation.HardRemoved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnHardRemovingApprovalSettingPublisherRoleByIdSubscriptionName),
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
