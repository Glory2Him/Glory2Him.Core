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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Comments
{
    public partial class CommentServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllCommentsAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            IQueryable<Comment> randomComments = CreateRandomComments();

            foreach (Comment comment in randomComments)
            {
                comment.IsDeleted = false;
            }

            IQueryable<Comment> storageComments = randomComments;
            IQueryable<Comment> expectedComments = storageComments;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllCommentsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageComments);

            // when
            IQueryable<Comment> actualComments =
                await this.commentService.RetrieveAllCommentsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualComments.Should().BeEquivalentTo(expectedComments);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllCommentsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveAllOnlyPublicCommentsWhenCallerIsAnonymousAsync()
        {
            // given
            this.ambientSecurityContext = new SecurityContext { IsAuthenticated = false };
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Comment publicComment = CreateRandomComment();
            publicComment.IsDeleted = false;
            publicComment.ApprovalStatus = ApprovalStatus.Approved;
            publicComment.IsPublished = true;
            publicComment.PublishDate = null;

            Comment pastPublishedComment = CreateRandomComment();
            pastPublishedComment.IsDeleted = false;
            pastPublishedComment.ApprovalStatus = ApprovalStatus.Approved;
            pastPublishedComment.IsPublished = true;
            pastPublishedComment.PublishDate = randomDateTimeOffset.AddDays(GetRandomNegativeNumber());

            Comment draftComment = CreateRandomComment();
            draftComment.IsDeleted = false;
            draftComment.ApprovalStatus = ApprovalStatus.Draft;
            draftComment.IsPublished = false;

            Comment futurePublishedComment = CreateRandomComment();
            futurePublishedComment.IsDeleted = false;
            futurePublishedComment.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedComment.IsPublished = true;
            futurePublishedComment.PublishDate = randomDateTimeOffset.AddDays(GetRandomNumber());

            Comment deletedComment = CreateRandomComment();
            deletedComment.IsDeleted = true;
            deletedComment.ApprovalStatus = ApprovalStatus.Approved;
            deletedComment.IsPublished = true;
            deletedComment.PublishDate = null;

            IQueryable<Comment> storageComments = new List<Comment>
            {
                publicComment,
                pastPublishedComment,
                draftComment,
                futurePublishedComment,
                deletedComment
            }.AsQueryable();

            IQueryable<Comment> expectedComments = new List<Comment>
            {
                publicComment,
                pastPublishedComment
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllCommentsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageComments);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            IQueryable<Comment> actualComments =
                await this.commentService.RetrieveAllCommentsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualComments.Should().BeEquivalentTo(expectedComments);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllCommentsAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldRetrieveAllPublicAndOwnCommentsWhenUserHasNoReviewRoleAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            string randomActorUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Comment publicComment = CreateRandomComment();
            publicComment.IsDeleted = false;
            publicComment.ApprovalStatus = ApprovalStatus.Approved;
            publicComment.IsPublished = true;
            publicComment.PublishDate = null;

            Comment ownDraftComment = CreateRandomComment();
            ownDraftComment.IsDeleted = false;
            ownDraftComment.ApprovalStatus = ApprovalStatus.Draft;
            ownDraftComment.IsPublished = false;
            ownDraftComment.CreatedBy = randomActorUserId;

            Comment othersDraftComment = CreateRandomComment();
            othersDraftComment.IsDeleted = false;
            othersDraftComment.ApprovalStatus = ApprovalStatus.Draft;
            othersDraftComment.IsPublished = false;

            Comment ownDeletedComment = CreateRandomComment();
            ownDeletedComment.IsDeleted = true;
            ownDeletedComment.CreatedBy = randomActorUserId;

            IQueryable<Comment> storageComments = new List<Comment>
            {
                publicComment,
                ownDraftComment,
                othersDraftComment,
                ownDeletedComment
            }.AsQueryable();

            IQueryable<Comment> expectedComments = new List<Comment>
            {
                publicComment,
                ownDraftComment
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllCommentsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageComments);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<Comment> actualComments =
                await this.commentService.RetrieveAllCommentsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualComments.Should().BeEquivalentTo(expectedComments);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllCommentsAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldRetrieveAllNonDeletedCommentsWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: a review-role caller sees every non-deleted row — no clock, no
            // user-id resolution
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);

            Comment publicComment = CreateRandomComment();
            publicComment.IsDeleted = false;
            publicComment.ApprovalStatus = ApprovalStatus.Approved;
            publicComment.IsPublished = true;
            publicComment.PublishDate = null;

            Comment draftComment = CreateRandomComment();
            draftComment.IsDeleted = false;
            draftComment.ApprovalStatus = ApprovalStatus.Draft;
            draftComment.IsPublished = false;

            Comment futurePublishedComment = CreateRandomComment();
            futurePublishedComment.IsDeleted = false;
            futurePublishedComment.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedComment.IsPublished = true;
            futurePublishedComment.PublishDate = GetRandomDateTimeOffset().AddDays(GetRandomNumber());

            Comment deletedComment = CreateRandomComment();
            deletedComment.IsDeleted = true;

            IQueryable<Comment> storageComments = new List<Comment>
            {
                publicComment,
                draftComment,
                futurePublishedComment,
                deletedComment
            }.AsQueryable();

            IQueryable<Comment> expectedComments = new List<Comment>
            {
                publicComment,
                draftComment,
                futurePublishedComment
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllCommentsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageComments);

            // when
            IQueryable<Comment> actualComments =
                await this.commentService.RetrieveAllCommentsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualComments.Should().BeEquivalentTo(expectedComments);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllCommentsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
