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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Comments;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Comments
{
    public partial class CommentServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveCommentByIdAsync()
        {
            // given
            Comment randomComment = CreateRandomComment();
            Comment storageComment = randomComment;
            storageComment.IsDeleted = false;
            storageComment.ApprovalStatus = ApprovalStatus.Approved;
            storageComment.IsPublished = true;
            storageComment.PublishDate = null;
            Comment expectedComment = storageComment;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    randomComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageComment);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            // when
            Comment actualComment =
                await this.commentService.RetrieveCommentByIdAsync(
                    randomComment.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualComment.Should().BeEquivalentTo(expectedComment);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    randomComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveNonPublicCommentByIdWhenUserIsOwnerAsync()
        {
            // given
            Comment randomComment = CreateRandomComment();
            Comment storageComment = randomComment;
            storageComment.IsDeleted = false;
            storageComment.ApprovalStatus = ApprovalStatus.Draft;
            storageComment.IsPublished = false;
            Comment expectedComment = storageComment;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    randomComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageComment);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageComment.CreatedBy);

            // when
            Comment actualComment =
                await this.commentService.RetrieveCommentByIdAsync(
                    randomComment.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualComment.Should().BeEquivalentTo(expectedComment);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    randomComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
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
        public async Task ShouldRetrieveNonPublicCommentByIdWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: the caller is not the owner but holds a review role
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            string randomActorUserId = GetRandomString();
            Comment randomComment = CreateRandomComment();
            Comment storageComment = randomComment;
            storageComment.IsDeleted = false;
            storageComment.ApprovalStatus = ApprovalStatus.Draft;
            storageComment.IsPublished = false;
            Comment expectedComment = storageComment;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    randomComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageComment);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            Comment actualComment =
                await this.commentService.RetrieveCommentByIdAsync(
                    randomComment.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualComment.Should().BeEquivalentTo(expectedComment);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    randomComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
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
