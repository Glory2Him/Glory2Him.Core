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
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingReviewerRoles
{
    public partial class ApprovalSettingReviewerRoleServiceTests
    {
        [Fact]
        public async Task ShouldHardRemoveApprovalSettingReviewerRoleByIdAsync()
        {
            // given
            ApprovalSettingReviewerRole randomApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();
            ApprovalSettingReviewerRole storageApprovalSettingReviewerRole = randomApprovalSettingReviewerRole;
            ApprovalSettingReviewerRole expectedApprovalSettingReviewerRole = storageApprovalSettingReviewerRole.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    randomApprovalSettingReviewerRole.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSettingReviewerRole);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteApprovalSettingReviewerRoleAsync(storageApprovalSettingReviewerRole, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApprovalSettingReviewerRole);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingReviewerRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingReviewerRole>>(),
                    ApprovalSettingReviewerRoleEventOperation.HardRemoved))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSettingReviewerRole>>(
                        new EventPublishResult<ApprovalSettingReviewerRole>()));

            // when
            ApprovalSettingReviewerRole actualApprovalSettingReviewerRole =
                await this.approvalSettingReviewerRoleService.HardRemoveApprovalSettingReviewerRoleByIdAsync(
                    randomApprovalSettingReviewerRole.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingReviewerRole.Should().BeEquivalentTo(expectedApprovalSettingReviewerRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    randomApprovalSettingReviewerRole.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteApprovalSettingReviewerRoleAsync(storageApprovalSettingReviewerRole, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalSettingReviewerRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingReviewerRole>>(),
                    ApprovalSettingReviewerRoleEventOperation.HardRemoved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnHardRemovingApprovalSettingReviewerRoleByIdSubscriptionName),
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
