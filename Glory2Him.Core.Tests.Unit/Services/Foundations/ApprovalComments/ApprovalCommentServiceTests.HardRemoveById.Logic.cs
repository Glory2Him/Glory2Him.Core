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
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalComments
{
    public partial class ApprovalCommentServiceTests
    {
        [Fact]
        public async Task ShouldHardRemoveApprovalCommentByIdAsync()
        {
            // given
            ApprovalComment randomApprovalComment = CreateRandomApprovalComment();
            ApprovalComment storageApprovalComment = randomApprovalComment;
            ApprovalComment expectedApprovalComment = storageApprovalComment.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    randomApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteApprovalCommentAsync(storageApprovalComment, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApprovalComment);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    ApprovalCommentEventOperation.HardRemoved))
                    .Returns(new ValueTask<EventPublishResult<ApprovalComment>>(
                        new EventPublishResult<ApprovalComment>()));

            // when
            ApprovalComment actualApprovalComment =
                await this.approvalCommentService.HardRemoveApprovalCommentByIdAsync(
                    randomApprovalComment.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalComment.Should().BeEquivalentTo(expectedApprovalComment);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    randomApprovalComment.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteApprovalCommentAsync(storageApprovalComment, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    ApprovalCommentEventOperation.HardRemoved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalCommentOnHardRemovingApprovalCommentByIdSubscriptionName),
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
