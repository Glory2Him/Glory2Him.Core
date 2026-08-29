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

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviewRequests
{
    public partial class ApprovalReviewRequestServiceTests
    {
        [Theory]
        [MemberData(nameof(ReviewRoles))]
        public async Task ShouldRetrieveAllApprovalReviewRequestsWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);

            IQueryable<ApprovalReviewRequest> randomApprovalReviewRequests =
                CreateRandomApprovalReviewRequests();

            IQueryable<ApprovalReviewRequest> storageApprovalReviewRequests = randomApprovalReviewRequests;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalReviewRequestsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovalReviewRequests);

            // when
            IQueryable<ApprovalReviewRequest> actualApprovalReviewRequests =
                await this.approvalReviewRequestService.RetrieveAllApprovalReviewRequestsAsync(
                    TestContext.Current.CancellationToken);

            // then: the tier sees the whole round's invitations
            actualApprovalReviewRequests.Should().BeEquivalentTo(storageApprovalReviewRequests);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalReviewRequestsAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// A row the caller may not see drops out of the set rather than erroring, so a collection
        /// read never reveals how many invitations exist. An anonymous caller sees none at all.
        /// </summary>
        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldReturnNoApprovalReviewRequestsToAnAnonymousCallerAsync(
            SecurityContext unauthenticatedSecurityContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedSecurityContext;

            IQueryable<ApprovalReviewRequest> storageApprovalReviewRequests =
                CreateRandomApprovalReviewRequests();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalReviewRequestsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovalReviewRequests);

            // when
            IQueryable<ApprovalReviewRequest> actualApprovalReviewRequests =
                await this.approvalReviewRequestService.RetrieveAllApprovalReviewRequestsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalReviewRequests.Should().BeEmpty();
        }

        /// <summary>
        /// Outside the tier, a caller sees only the invitations they are a party to — the ones
        /// they raised and the ones addressed to them — and never anybody else's.
        /// </summary>
        [Fact]
        public async Task ShouldReturnOnlyOwnApprovalReviewRequestsToACallerOutsideTheTierAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            string callerUserId = Guid.NewGuid().ToString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReviewRequest raisedByCaller =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset, userId: callerUserId).Create();

            ApprovalReviewRequest addressedToCaller =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset).Create();

            addressedToCaller.RequestedUserId = callerUserId;

            ApprovalReviewRequest somebodyElses =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset).Create();

            ApprovalReviewRequest withdrawnButOwnedByCaller =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset, userId: callerUserId).Create();

            withdrawnButOwnedByCaller.IsDeleted = true;

            IQueryable<ApprovalReviewRequest> storageApprovalReviewRequests = new[]
            {
                raisedByCaller,
                addressedToCaller,
                somebodyElses,
                withdrawnButOwnedByCaller
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalReviewRequestsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovalReviewRequests);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(callerUserId);

            // when
            IQueryable<ApprovalReviewRequest> actualApprovalReviewRequests =
                await this.approvalReviewRequestService.RetrieveAllApprovalReviewRequestsAsync(
                    TestContext.Current.CancellationToken);

            // then: both parties' rows, and the withdrawn one is gone even though it is theirs
            actualApprovalReviewRequests.Should().BeEquivalentTo(
                new[] { raisedByCaller, addressedToCaller });
        }
    }
}
