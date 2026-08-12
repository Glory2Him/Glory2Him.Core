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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.Comments.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Comments
{
    public partial class CommentServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnSubmitIfIdIsInvalidAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.UpsertDataList(
                key: nameof(Comment.Id),
                value: "Id is required");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            // when
            ValueTask<Comment> submitTask =
                this.commentService.SubmitCommentByIdAsync(
                    Guid.Empty,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualException =
                await Assert.ThrowsAsync<CommentValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedCommentValidationException);

            // an invalid id never reaches storage
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectCommentByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowUnauthorizedOnSubmitIfCallerIsNotAuthenticatedAsync(
            SecurityContext unauthenticatedContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedContext;

            // when
            ValueTask<Comment> submitTask =
                this.commentService.SubmitCommentByIdAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<CommentValidationException>(submitTask.AsTask);

            // then: the contribution gate refuses before any row is read
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectCommentByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.CommentReadOnly)]
        public async Task ShouldThrowUnauthorizedOnSubmitIfCallerIsBlockedFromContributingAsync(
            string blockingRole)
        {
            // given: a read-only caller is blocked from every write, submit included, before the
            // row is even read
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(blockingRole);

            var unauthorizedCommentException =
                new UnauthorizedCommentException(
                    message: "The current user is blocked from contributing comments.");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedCommentException);

            // when
            ValueTask<Comment> submitTask =
                this.commentService.SubmitCommentByIdAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            CommentValidationException actualException =
                await Assert.ThrowsAsync<CommentValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectCommentByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnSubmitIfTheRowIsMissingAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Guid commentId = Guid.NewGuid();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    commentId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Comment)null);

            var notFoundCommentException =
                new NotFoundCommentException(
                    message: $"Comment not found with id: {commentId}.");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: notFoundCommentException);

            // when
            ValueTask<Comment> submitTask =
                this.commentService.SubmitCommentByIdAsync(
                    commentId,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualException =
                await Assert.ThrowsAsync<CommentValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateCommentAsync(
                        It.IsAny<Comment>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnSubmitIfTheRowIsSoftDeletedAsync()
        {
            // given: a soft-removed row is reported as not-found, matching the read posture
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Comment storageComment = CreateSubmittableStorageComment();
            storageComment.IsDeleted = true;

            SetupCommentStorageRead(storageComment);

            var notFoundCommentException =
                new NotFoundCommentException(
                    message: $"Comment not found with id: {storageComment.Id}.");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: notFoundCommentException);

            // when
            ValueTask<Comment> submitTask =
                this.commentService.SubmitCommentByIdAsync(
                    storageComment.Id,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualException =
                await Assert.ThrowsAsync<CommentValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateCommentAsync(
                        It.IsAny<Comment>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(NonPublisherRoleSets))]
        public async Task ShouldThrowUnauthorizedOnSubmitIfCallerIsNeitherOwnerNorPublisherAsync(
            string[] roles)
        {
            // given: a caller who neither owns the row nor holds the publisher tier may not
            // submit it. A Reviewer is included among the role sets: they hold write permission
            // on content, but moving a submission status is never theirs (§8.6 HR-3).
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);

            Comment storageComment = CreateSubmittableStorageComment();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync($"not-the-owner-{Guid.NewGuid()}");

            SetupCommentStorageRead(storageComment);

            var unauthorizedCommentException =
                new UnauthorizedCommentException(
                    message: "The current user is not allowed to submit this comment.");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedCommentException);

            // when
            ValueTask<Comment> submitTask =
                this.commentService.SubmitCommentByIdAsync(
                    storageComment.Id,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualException =
                await Assert.ThrowsAsync<CommentValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateCommentAsync(
                        It.IsAny<Comment>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(ApprovalStatus.Submitted)]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        [InlineData(ApprovalStatus.Dismissed)]
        public async Task ShouldThrowValidationExceptionOnSubmitIfTheStoredRowIsNotDraftAsync(
            ApprovalStatus storageStatus)
        {
            // given: only a Draft may be submitted (issue #111 case 7). A row already Submitted
            // or Approved is not a fresh submission — re-submitting one would either re-open a
            // decided item or re-announce a pending one. The caller is the owner, so this proves
            // the state gate stands on its own, after authorization passes.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Comment storageComment = CreateSubmittableStorageComment();
            storageComment.ApprovalStatus = storageStatus;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageComment.CreatedBy);

            SetupCommentStorageRead(storageComment);

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment cannot be submitted from status " +
                        $"{storageStatus}.");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            // when
            ValueTask<Comment> submitTask =
                this.commentService.SubmitCommentByIdAsync(
                    storageComment.Id,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualException =
                await Assert.ThrowsAsync<CommentValidationException>(submitTask.AsTask);

            // then: nothing written, nothing announced
            actualException.Should().BeEquivalentTo(expectedCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateCommentAsync(
                        It.IsAny<Comment>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishCommentAsync(
                        It.IsAny<EventEnvelope<Comment>>(),
                        It.IsAny<CommentEventOperation>()),
                Times.Never);
        }
    }
}
