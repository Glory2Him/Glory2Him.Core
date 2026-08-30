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
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalComments
{
    public partial class ApprovalCommentServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllApprovalCommentsAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            IQueryable<ApprovalComment> randomApprovalComments = CreateRandomApprovalComments();

            foreach (ApprovalComment approvalComment in randomApprovalComments)
            {
                approvalComment.IsDeleted = false;
            }

            IQueryable<ApprovalComment> storageApprovalComments = randomApprovalComments;
            IQueryable<ApprovalComment> expectedApprovalComments = storageApprovalComments;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalCommentsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovalComments);

            // when
            IQueryable<ApprovalComment> actualApprovalComments =
                await this.approvalCommentService.RetrieveAllApprovalCommentsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalComments.Should().BeEquivalentTo(expectedApprovalComments);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalCommentsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveNoApprovalCommentsWhenCallerIsAnonymousAsync()
        {
            // given: review threads are never public, so an anonymous caller gets an
            // empty set rather than an error — the read reveals no row count
            this.ambientSecurityContext = new SecurityContext { IsAuthenticated = false };
            IQueryable<ApprovalComment> randomApprovalComments = CreateRandomApprovalComments();

            foreach (ApprovalComment approvalComment in randomApprovalComments)
            {
                approvalComment.IsDeleted = false;
            }

            IQueryable<ApprovalComment> storageApprovalComments = randomApprovalComments;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalCommentsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovalComments);

            // when
            IQueryable<ApprovalComment> actualApprovalComments =
                await this.approvalCommentService.RetrieveAllApprovalCommentsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalComments.Should().BeEmpty();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalCommentsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveAllOwnApprovalCommentsWhenUserHasNoReviewRoleAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            string randomActorUserId = GetRandomString();

            ApprovalComment ownApprovalComment = CreateRandomApprovalComment();
            ownApprovalComment.IsDeleted = false;
            ownApprovalComment.CreatedBy = randomActorUserId;

            ApprovalComment othersApprovalComment = CreateRandomApprovalComment();
            othersApprovalComment.IsDeleted = false;

            ApprovalComment ownDeletedApprovalComment = CreateRandomApprovalComment();
            ownDeletedApprovalComment.IsDeleted = true;
            ownDeletedApprovalComment.CreatedBy = randomActorUserId;

            IQueryable<ApprovalComment> storageApprovalComments = new List<ApprovalComment>
            {
                ownApprovalComment,
                othersApprovalComment,
                ownDeletedApprovalComment
            }.AsQueryable();

            IQueryable<ApprovalComment> expectedApprovalComments = new List<ApprovalComment>
            {
                ownApprovalComment
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalCommentsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovalComments);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<ApprovalComment> actualApprovalComments =
                await this.approvalCommentService.RetrieveAllApprovalCommentsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalComments.Should().BeEquivalentTo(expectedApprovalComments);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalCommentsAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldRetrieveAllNonDeletedApprovalCommentsWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: a review-role caller sees every non-deleted row — no user-id
            // resolution needed
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);

            ApprovalComment firstApprovalComment = CreateRandomApprovalComment();
            firstApprovalComment.IsDeleted = false;

            ApprovalComment secondApprovalComment = CreateRandomApprovalComment();
            secondApprovalComment.IsDeleted = false;

            ApprovalComment deletedApprovalComment = CreateRandomApprovalComment();
            deletedApprovalComment.IsDeleted = true;

            IQueryable<ApprovalComment> storageApprovalComments = new List<ApprovalComment>
            {
                firstApprovalComment,
                secondApprovalComment,
                deletedApprovalComment
            }.AsQueryable();

            IQueryable<ApprovalComment> expectedApprovalComments = new List<ApprovalComment>
            {
                firstApprovalComment,
                secondApprovalComment
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalCommentsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovalComments);

            // when
            IQueryable<ApprovalComment> actualApprovalComments =
                await this.approvalCommentService.RetrieveAllApprovalCommentsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalComments.Should().BeEquivalentTo(expectedApprovalComments);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalCommentsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
