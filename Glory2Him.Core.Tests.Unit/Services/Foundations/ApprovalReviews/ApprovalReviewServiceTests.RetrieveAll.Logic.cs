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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviews
{
    public partial class ApprovalReviewServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllApprovalReviewsAsync()
        {
            // given
            IQueryable<ApprovalReview> randomApprovalReviews = CreateRandomApprovalReviews();
            IQueryable<ApprovalReview> storageApprovalReviews = randomApprovalReviews;
            IQueryable<ApprovalReview> expectedApprovalReviews = storageApprovalReviews;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalReviewsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovalReviews);

            // when
            IQueryable<ApprovalReview> actualApprovalReviews =
                await this.approvalReviewService.RetrieveAllApprovalReviewsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalReviews.Should().BeEquivalentTo(expectedApprovalReviews);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalReviewsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
