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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviews
{
    public partial class ApprovalReviewServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveApprovalReviewByIdAsync()
        {
            // given: the caller is the reviewer who wrote the verdict
            ApprovalReview randomApprovalReview = CreateRandomApprovalReview();
            ApprovalReview storageApprovalReview = randomApprovalReview;
            storageApprovalReview.IsDeleted = false;
            ApprovalReview expectedApprovalReview = storageApprovalReview;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    randomApprovalReview.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageApprovalReview.CreatedBy);

            // when
            ApprovalReview actualApprovalReview =
                await this.approvalReviewService.RetrieveApprovalReviewByIdAsync(
                    randomApprovalReview.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalReview.Should().BeEquivalentTo(expectedApprovalReview);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    randomApprovalReview.Id,
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
        public async Task ShouldRetrieveApprovalReviewByIdWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: the caller did not write the verdict but holds a review role — the
            // entity-scoped roles count by their §16.6 suffix
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            string randomActorUserId = GetRandomString();
            ApprovalReview randomApprovalReview = CreateRandomApprovalReview();
            ApprovalReview storageApprovalReview = randomApprovalReview;
            storageApprovalReview.IsDeleted = false;
            ApprovalReview expectedApprovalReview = storageApprovalReview;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    randomApprovalReview.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ApprovalReview actualApprovalReview =
                await this.approvalReviewService.RetrieveApprovalReviewByIdAsync(
                    randomApprovalReview.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalReview.Should().BeEquivalentTo(expectedApprovalReview);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    randomApprovalReview.Id,
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
