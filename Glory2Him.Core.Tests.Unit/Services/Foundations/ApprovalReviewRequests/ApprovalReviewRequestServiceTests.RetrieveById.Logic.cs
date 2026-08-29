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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
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
        public async Task ShouldRetrieveApprovalReviewRequestByIdWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);

            ApprovalReviewRequest randomApprovalReviewRequest = CreateRandomApprovalReviewRequest();
            Guid inputApprovalReviewRequestId = randomApprovalReviewRequest.Id;
            ApprovalReviewRequest storageApprovalReviewRequest = randomApprovalReviewRequest;
            ApprovalReviewRequest expectedApprovalReviewRequest = storageApprovalReviewRequest.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReviewRequest);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync("a-moderator-who-is-not-a-party");

            // when
            ApprovalReviewRequest actualApprovalReviewRequest =
                await this.approvalReviewRequestService.RetrieveApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalReviewRequest.Should().BeEquivalentTo(expectedApprovalReviewRequest);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// The requester and the invited person are each a party to the invitation, and are named
        /// explicitly rather than left to the tier. A party should still be able to read the row
        /// that names them if their role membership shifts underneath it — the row is about them.
        /// </summary>
        [Fact]
        public async Task ShouldRetrieveApprovalReviewRequestByIdWhenUserIsTheRequesterAsync()
        {
            // given: no review role at all, but the caller raised this invitation
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ApprovalReviewRequest randomApprovalReviewRequest = CreateRandomApprovalReviewRequest();
            Guid inputApprovalReviewRequestId = randomApprovalReviewRequest.Id;
            ApprovalReviewRequest storageApprovalReviewRequest = randomApprovalReviewRequest;
            ApprovalReviewRequest expectedApprovalReviewRequest = storageApprovalReviewRequest.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReviewRequest);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageApprovalReviewRequest.CreatedBy);

            // when
            ApprovalReviewRequest actualApprovalReviewRequest =
                await this.approvalReviewRequestService.RetrieveApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalReviewRequest.Should().BeEquivalentTo(expectedApprovalReviewRequest);
        }

        [Fact]
        public async Task ShouldRetrieveApprovalReviewRequestByIdWhenUserIsTheInvitedPersonAsync()
        {
            // given: no review role, but the caller is who the invitation names
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ApprovalReviewRequest randomApprovalReviewRequest = CreateRandomApprovalReviewRequest();
            Guid inputApprovalReviewRequestId = randomApprovalReviewRequest.Id;
            ApprovalReviewRequest storageApprovalReviewRequest = randomApprovalReviewRequest;
            ApprovalReviewRequest expectedApprovalReviewRequest = storageApprovalReviewRequest.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReviewRequest);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageApprovalReviewRequest.RequestedUserId);

            // when
            ApprovalReviewRequest actualApprovalReviewRequest =
                await this.approvalReviewRequestService.RetrieveApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalReviewRequest.Should().BeEquivalentTo(expectedApprovalReviewRequest);
        }
    }
}
