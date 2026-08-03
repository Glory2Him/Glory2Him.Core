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
        public async Task ShouldRetrieveApprovalCommentByIdAsync()
        {
            // given: a review thread is never public, so the author is the plainest
            // caller who may read their own comment back
            ApprovalComment randomApprovalComment = CreateRandomApprovalComment();
            ApprovalComment storageApprovalComment = randomApprovalComment;
            storageApprovalComment.IsDeleted = false;
            ApprovalComment expectedApprovalComment = storageApprovalComment;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    randomApprovalComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageApprovalComment.CreatedBy);

            // when
            ApprovalComment actualApprovalComment =
                await this.approvalCommentService.RetrieveApprovalCommentByIdAsync(
                    randomApprovalComment.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalComment.Should().BeEquivalentTo(expectedApprovalComment);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    randomApprovalComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ReviewRoles))]
        public async Task ShouldRetrieveApprovalCommentByIdWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: the caller did not write the comment but holds a review role
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            string randomActorUserId = GetRandomString();
            ApprovalComment randomApprovalComment = CreateRandomApprovalComment();
            ApprovalComment storageApprovalComment = randomApprovalComment;
            storageApprovalComment.IsDeleted = false;
            ApprovalComment expectedApprovalComment = storageApprovalComment;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    randomApprovalComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ApprovalComment actualApprovalComment =
                await this.approvalCommentService.RetrieveApprovalCommentByIdAsync(
                    randomApprovalComment.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalComment.Should().BeEquivalentTo(expectedApprovalComment);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    randomApprovalComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
