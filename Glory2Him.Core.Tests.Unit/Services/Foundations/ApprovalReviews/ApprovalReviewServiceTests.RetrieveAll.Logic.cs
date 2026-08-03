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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        public async Task ShouldRetrieveAllApprovalReviewsAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            IQueryable<ApprovalReview> randomApprovalReviews = CreateRandomApprovalReviews();

            foreach (ApprovalReview approvalReview in randomApprovalReviews)
            {
                approvalReview.IsDeleted = false;
            }

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

        [Fact]
        public async Task ShouldRetrieveNoApprovalReviewsWhenCallerIsAnonymousAsync()
        {
            // given: verdicts are never public — an anonymous caller gets the empty set,
            // not an error, so the collection reveals no counts
            this.ambientSecurityContext = new SecurityContext { IsAuthenticated = false };
            IQueryable<ApprovalReview> storageApprovalReviews = CreateRandomApprovalReviews();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalReviewsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovalReviews);

            // when
            IQueryable<ApprovalReview> actualApprovalReviews =
                await this.approvalReviewService.RetrieveAllApprovalReviewsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalReviews.Should().BeEmpty();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalReviewsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveOnlyOwnApprovalReviewsWhenUserHasNoReviewRoleAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            string randomActorUserId = GetRandomString();

            ApprovalReview ownApprovalReview = CreateRandomApprovalReview();
            ownApprovalReview.IsDeleted = false;
            ownApprovalReview.CreatedBy = randomActorUserId;

            ApprovalReview othersApprovalReview = CreateRandomApprovalReview();
            othersApprovalReview.IsDeleted = false;

            ApprovalReview ownDeletedApprovalReview = CreateRandomApprovalReview();
            ownDeletedApprovalReview.IsDeleted = true;
            ownDeletedApprovalReview.CreatedBy = randomActorUserId;

            IQueryable<ApprovalReview> storageApprovalReviews = new List<ApprovalReview>
            {
                ownApprovalReview,
                othersApprovalReview,
                ownDeletedApprovalReview
            }.AsQueryable();

            IQueryable<ApprovalReview> expectedApprovalReviews = new List<ApprovalReview>
            {
                ownApprovalReview
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalReviewsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovalReviews);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<ApprovalReview> actualApprovalReviews =
                await this.approvalReviewService.RetrieveAllApprovalReviewsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalReviews.Should().BeEquivalentTo(expectedApprovalReviews);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalReviewsAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldRetrieveAllNonDeletedApprovalReviewsWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: a review-role caller sees every non-deleted verdict — no user-id
            // resolution needed
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);

            ApprovalReview firstApprovalReview = CreateRandomApprovalReview();
            firstApprovalReview.IsDeleted = false;

            ApprovalReview secondApprovalReview = CreateRandomApprovalReview();
            secondApprovalReview.IsDeleted = false;

            ApprovalReview deletedApprovalReview = CreateRandomApprovalReview();
            deletedApprovalReview.IsDeleted = true;

            IQueryable<ApprovalReview> storageApprovalReviews = new List<ApprovalReview>
            {
                firstApprovalReview,
                secondApprovalReview,
                deletedApprovalReview
            }.AsQueryable();

            IQueryable<ApprovalReview> expectedApprovalReviews = new List<ApprovalReview>
            {
                firstApprovalReview,
                secondApprovalReview
            }.AsQueryable();

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
