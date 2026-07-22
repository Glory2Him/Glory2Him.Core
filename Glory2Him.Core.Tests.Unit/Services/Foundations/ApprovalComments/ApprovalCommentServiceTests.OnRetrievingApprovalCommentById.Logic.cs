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

using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalComments
{
    public partial class ApprovalCommentServiceTests
    {
        [Fact]
        public async Task ShouldReplyWithApprovalCommentOnRetrievingApprovalCommentByIdEventAsync()
        {
            // given
            ApprovalComment randomApprovalComment = CreateRandomApprovalComment();
            ApprovalComment storageApprovalComment = randomApprovalComment;
            ApprovalComment expectedApprovalComment = storageApprovalComment;

            var requestEnvelope = new EventEnvelope<ApprovalComment>
            {
                Content = new ApprovalComment { Id = randomApprovalComment.Id }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    randomApprovalComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalComment);

            // when
            EventEnvelope<ApprovalComment>? actualReplyEnvelope =
                await this.approvalCommentService.OnRetrievingApprovalCommentByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalComment);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    randomApprovalComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.eventEnvelopeFactoryMock.Verify(factory =>
                factory.CreateNextAsync(requestEnvelope, storageApprovalComment),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
