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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.Comments.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Comments
{
    public partial class CommentServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfCommentIsNullAndLogItAsync()
        {
            // given
            Comment nullComment = null;

            var nullCommentException =
                new NullCommentException(message: "Comment is null.");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: nullCommentException);

            // when
            ValueTask<Comment> addCommentTask =
                this.commentService.AddCommentAsync(
                    nullComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    addCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldThrowValidationExceptionOnAddIfCommentIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidComment = new Comment
            {
                Id = Guid.Empty,
                Content = invalidText,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.Id),
                values: "Id is required");

            invalidCommentException.AddData(
                key: nameof(Comment.Content),
                values: "Text is required");

            invalidCommentException.AddData(
                key: nameof(Comment.CreatedBy),
                values: "Text is required");

            invalidCommentException.AddData(
                key: nameof(Comment.UpdatedBy),
                values: "Text is required");

            invalidCommentException.AddData(
                key: nameof(Comment.CreatedWhen),
                values: new[]
                {
                    "Date is required",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            invalidCommentException.AddData(
                key: nameof(Comment.UpdatedWhen),
                values: "Date is required");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Comment> addCommentTask =
                this.commentService.AddCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    addCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfCommentExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Comment invalidComment = CreateCommentFiller(randomDateTimeOffset, randomUserId).Create();

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.CreatedBy),
                values: $"Text exceed max length of {invalidComment.CreatedBy.Length - 1} characters");

            invalidCommentException.AddData(
                key: nameof(Comment.UpdatedBy),
                values: $"Text exceed max length of {invalidComment.UpdatedBy.Length - 1} characters");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Comment> addCommentTask =
                this.commentService.AddCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    addCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfUpdatedWhenIsNotSameAsCreatedWhenAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Comment randomComment = CreateCommentFiller(randomDateTimeOffset, randomUserId).Create();
            Comment invalidComment = randomComment;
            invalidComment.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.UpdatedWhen),
                values: $"Date is not the same as {nameof(Comment.CreatedWhen)}");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Comment> addCommentTask =
                this.commentService.AddCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    addCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfCreatedByIsNotSameAsCurrentUserIdAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            string differentUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Comment randomComment = CreateCommentFiller(randomDateTimeOffset, randomUserId).Create();
            Comment invalidComment = randomComment;
            invalidComment.CreatedBy = differentUserId;
            invalidComment.UpdatedBy = differentUserId;

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.CreatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Comment> addCommentTask =
                this.commentService.AddCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    addCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfUpdatedByIsNotSameAsCreatedByAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Comment randomComment = CreateCommentFiller(randomDateTimeOffset, randomUserId).Create();
            Comment invalidComment = randomComment;
            invalidComment.UpdatedBy = GetRandomString();

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.UpdatedBy),
                values: $"Text is not the same as {nameof(Comment.CreatedBy)}");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Comment> addCommentTask =
                this.commentService.AddCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    addCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(MinutesBeforeOrAfter))]
        public async Task ShouldThrowValidationExceptionOnAddIfCreatedWhenIsNotRecentAndLogItAsync(int minutes)
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Comment randomComment = CreateCommentFiller(randomDateTimeOffset, randomUserId).Create();
            Comment invalidComment = randomComment;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidComment.CreatedWhen = randomDateTimeOffset.AddMinutes(minutes);
            invalidComment.UpdatedWhen = invalidComment.CreatedWhen;

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.CreatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidComment.CreatedWhen}");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<Comment> addCommentTask =
                this.commentService.AddCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    addCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnAddIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            Comment someComment = CreateRandomComment();

            var unauthorizedCommentException = new UnauthorizedCommentException(
                message: "The current user is not authenticated.");

            var expectedCommentValidationException = new CommentValidationException(
                message: "Comment validation error occurred, fix the errors and try again.",
                innerException: unauthorizedCommentException);

            // when
            ValueTask<Comment> addCommentTask =
                this.commentService.AddCommentAsync(
                    someComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    addCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.CommentReadOnly)]
        public async Task ShouldThrowValidationExceptionOnAddIfUserIsBlockedFromContributingAndLogItAsync(
            string blockedRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(blockedRole);
            Comment someComment = CreateRandomComment();

            var unauthorizedCommentException = new UnauthorizedCommentException(
                message: "The current user is blocked from contributing comments.");

            var expectedCommentValidationException = new CommentValidationException(
                message: "Comment validation error occurred, fix the errors and try again.",
                innerException: unauthorizedCommentException);

            // when
            ValueTask<Comment> addCommentTask =
                this.commentService.AddCommentAsync(
                    someComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    addCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfIsPublishedIsSetAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Comment randomComment = CreateCommentFiller(randomDateTimeOffset, randomUserId).Create();
            Comment invalidComment = randomComment;
            invalidComment.IsPublished = true;

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.IsPublished),
                values: "Value is not allowed on add");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Comment> addCommentTask =
                this.commentService.AddCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    addCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfPublishDateIsSetAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Comment randomComment = CreateCommentFiller(randomDateTimeOffset, randomUserId).Create();
            Comment invalidComment = randomComment;
            invalidComment.PublishDate = randomDateTimeOffset;

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.PublishDate),
                values: "Date is not allowed on add");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Comment> addCommentTask =
                this.commentService.AddCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    addCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalStatusIsAVerdictAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Comment randomComment = CreateCommentFiller(randomDateTimeOffset, randomUserId).Create();
            Comment invalidComment = randomComment;
            invalidComment.ApprovalStatus = ApprovalStatus.Approved;

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.ApprovalStatus),
                values: "Value must be Draft or Submitted on add");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Comment> addCommentTask =
                this.commentService.AddCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    addCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
