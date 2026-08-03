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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Approvals
{
    public partial class ApprovalServiceTests
    {
        [Fact]
        public async Task ShouldHardRemoveApprovalByIdAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Approval randomApproval = CreateRandomApproval();
            Approval storageApproval = randomApproval;
            Approval expectedApproval = storageApproval.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    randomApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteApprovalAsync(storageApproval, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApproval);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalAsync(
                    It.IsAny<EventEnvelope<Approval>>(),
                    ApprovalEventOperation.HardRemoved))
                    .Returns(new ValueTask<EventPublishResult<Approval>>(
                        new EventPublishResult<Approval>()));

            // when
            Approval actualApproval =
                await this.approvalService.HardRemoveApprovalByIdAsync(
                    randomApproval.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApproval.Should().BeEquivalentTo(expectedApproval);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(
                    randomApproval.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteApprovalAsync(storageApproval, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalAsync(
                    It.IsAny<EventEnvelope<Approval>>(),
                    ApprovalEventOperation.HardRemoved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalOnHardRemovingApprovalByIdSubscriptionName),
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
