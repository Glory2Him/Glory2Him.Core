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
        public async Task ShouldThrowValidationExceptionOnModifyIfCommentIsNullAndLogItAsync()
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
            ValueTask<Comment> modifyCommentTask =
                this.commentService.ModifyCommentAsync(
                    nullComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    modifyCommentTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnModifyIfCommentIsInvalidAndLogItAsync(
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
                values: "Date is required");

            invalidCommentException.AddData(
                key: nameof(Comment.UpdatedWhen),
                values: new[]
                {
                    "Date is required",
                    "Date is the same as CreatedWhen",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Comment> modifyCommentTask =
                this.commentService.ModifyCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    modifyCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfCommentNotFoundAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Comment randomComment = CreateRandomModifyComment(randomDateTimeOffset, randomUserId);
            Comment nonExistentComment = randomComment;
            Comment noComment = null;

            var notFoundCommentException = new NotFoundCommentException(
                message: $"Comment not found with id: {nonExistentComment.Id}.");

            var expectedCommentValidationException = new CommentValidationException(
                message: "Comment validation error occurred, fix the errors and try again.",
                innerException: notFoundCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(nonExistentComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    nonExistentComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noComment);

            // when
            ValueTask<Comment> modifyCommentTask =
                this.commentService.ModifyCommentAsync(
                    nonExistentComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    modifyCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    nonExistentComment.Id,
                    TestContext.Current.CancellationToken),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageCreatedWhenNotSameAsInputAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Comment randomComment = CreateRandomModifyComment(randomDateTimeOffset, randomUserId);
            Comment invalidComment = randomComment;
            Comment storageComment = randomComment.DeepClone();
            storageComment.CreatedWhen = GetRandomDateTimeOffset();
            storageComment.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidCommentException = new InvalidCommentException(
                message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.CreatedWhen),
                values: $"Date is not the same as {nameof(Comment.CreatedWhen)}");

            var expectedCommentValidationException = new CommentValidationException(
                message: "Comment validation error occurred, fix the errors and try again.",
                innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    invalidComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidComment,
                    storageComment))
                        .ReturnsAsync(invalidComment);

            // when
            ValueTask<Comment> modifyCommentTask =
                this.commentService.ModifyCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    modifyCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    invalidComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidComment,
                    storageComment),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageCreatedByNotSameAsInputAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Comment randomComment = CreateRandomModifyComment(randomDateTimeOffset, randomUserId);
            Comment invalidComment = randomComment;
            Comment storageComment = randomComment.DeepClone();
            storageComment.CreatedBy = GetRandomString();
            storageComment.UpdatedWhen = storageComment.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.CreatedBy),
                values: $"Text is not the same as {nameof(Comment.CreatedBy)}");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    invalidComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidComment,
                    storageComment))
                        .ReturnsAsync(invalidComment);

            // when
            ValueTask<Comment> modifyCommentTask =
                this.commentService.ModifyCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    modifyCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    invalidComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidComment,
                    storageComment),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageUpdatedWhenSameAsInputAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Comment randomComment = CreateRandomModifyComment(randomDateTimeOffset, randomUserId);
            Comment invalidComment = randomComment;
            Comment storageComment = randomComment.DeepClone();

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.UpdatedWhen),
                values: $"Date is the same as {nameof(Comment.UpdatedWhen)}");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    invalidComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidComment,
                    storageComment))
                        .ReturnsAsync(invalidComment);

            // when
            ValueTask<Comment> modifyCommentTask =
                this.commentService.ModifyCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    modifyCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    invalidComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidComment,
                    storageComment),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedByIsNotSameAsCurrentUserIdAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            string differentUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Comment randomComment = CreateRandomModifyComment(randomDateTimeOffset, randomUserId);
            Comment invalidComment = randomComment;
            invalidComment.UpdatedBy = differentUserId;

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.UpdatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Comment> modifyCommentTask =
                this.commentService.ModifyCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    modifyCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedWhenIsSameAsCreatedWhenAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Comment randomComment = CreateRandomModifyComment(randomDateTimeOffset, randomUserId);
            Comment invalidComment = randomComment;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidComment.UpdatedWhen = invalidComment.CreatedWhen;

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.UpdatedWhen),
                values: new[]
                {
                    $"Date is the same as {nameof(Comment.CreatedWhen)}",
                    $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                        $"but found {invalidComment.UpdatedWhen}"
                });

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Comment> modifyCommentTask =
                this.commentService.ModifyCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    modifyCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedWhenIsNotRecentAndLogItAsync(int minutes)
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Comment randomComment = CreateRandomModifyComment(randomDateTimeOffset, randomUserId);
            Comment invalidComment = randomComment;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidComment.UpdatedWhen = randomDateTimeOffset.AddMinutes(minutes);

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.UpdatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidComment.UpdatedWhen}");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<Comment> modifyCommentTask =
                this.commentService.ModifyCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    modifyCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfCommentExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Comment invalidComment = CreateRandomModifyComment(randomDateTimeOffset, randomUserId);

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
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Comment> modifyCommentTask =
                this.commentService.ModifyCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    modifyCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsNotAuthenticatedAndLogItAsync(
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
            ValueTask<Comment> modifyCommentTask =
                this.commentService.ModifyCommentAsync(
                    someComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    modifyCommentTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsBlockedFromContributingAndLogItAsync(
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
            ValueTask<Comment> modifyCommentTask =
                this.commentService.ModifyCommentAsync(
                    someComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    modifyCommentTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsNotOwnerAndHasNoReviewRoleAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Comment randomComment = CreateRandomModifyComment(randomDateTimeOffset, randomUserId);
            Comment inputComment = randomComment;
            Comment storageComment = randomComment.DeepClone();
            storageComment.CreatedBy = GetRandomString();
            storageComment.UpdatedWhen = storageComment.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var unauthorizedCommentException = new UnauthorizedCommentException(
                message: "The current user is not allowed to modify this comment.");

            var expectedCommentValidationException = new CommentValidationException(
                message: "Comment validation error occurred, fix the errors and try again.",
                innerException: unauthorizedCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    inputComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageComment);

            // when
            ValueTask<Comment> modifyCommentTask =
                this.commentService.ModifyCommentAsync(
                    inputComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    modifyCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(inputComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    inputComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateCommentAsync(
                    It.IsAny<Comment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

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
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalStatusChangedByNonPublisherAndLogItAsync()
        {
            // given
            // a Reviewer holds write permission but is neither the owner nor in the Publisher
            // tier, so mayTransitionApprovalStatus is false. The move is Draft -> Submitted — one
            // the owner or a Publisher WOULD be allowed — so the refusal comes from the carve-out
            // gate, not from the status being a verdict.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            string ownerUserId = GetRandomString();
            Comment invalidComment = CreateRandomModifyComment(randomDateTimeOffset, randomUserId);
            invalidComment.CreatedBy = ownerUserId;
            invalidComment.ApprovalStatus = ApprovalStatus.Draft;
            Comment storageComment = invalidComment.DeepClone();
            storageComment.UpdatedWhen = storageComment.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            invalidComment.ApprovalStatus = ApprovalStatus.Submitted;

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.ApprovalStatus),
                values: "Value is not the same as storage approval status");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    invalidComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidComment,
                    storageComment))
                        .ReturnsAsync(invalidComment);

            // when
            ValueTask<Comment> modifyCommentTask =
                this.commentService.ModifyCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    modifyCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    invalidComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidComment,
                    storageComment),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfIsPublishedChangedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Comment randomComment = CreateRandomModifyComment(randomDateTimeOffset, randomUserId);
            Comment invalidComment = randomComment;
            Comment storageComment = randomComment.DeepClone();
            storageComment.UpdatedWhen = storageComment.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            invalidComment.IsPublished = true;

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.IsPublished),
                values: "Value is not the same as IsPublished");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    invalidComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidComment,
                    storageComment))
                        .ReturnsAsync(invalidComment);

            // when
            ValueTask<Comment> modifyCommentTask =
                this.commentService.ModifyCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    modifyCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    invalidComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidComment,
                    storageComment),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfPublishDateChangedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Comment randomComment = CreateRandomModifyComment(randomDateTimeOffset, randomUserId);
            Comment invalidComment = randomComment;
            Comment storageComment = randomComment.DeepClone();
            storageComment.UpdatedWhen = storageComment.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            invalidComment.PublishDate = randomDateTimeOffset;

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.PublishDate),
                values: "Date is not the same as PublishDate");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    invalidComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidComment,
                    storageComment))
                        .ReturnsAsync(invalidComment);

            // when
            ValueTask<Comment> modifyCommentTask =
                this.commentService.ModifyCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    modifyCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    invalidComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidComment,
                    storageComment),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfIsApprovedByBypassChangedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Comment randomComment = CreateRandomModifyComment(randomDateTimeOffset, randomUserId);
            Comment invalidComment = randomComment;
            Comment storageComment = randomComment.DeepClone();
            storageComment.UpdatedWhen = storageComment.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            invalidComment.IsApprovedByBypass = !storageComment.IsApprovedByBypass;

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.IsApprovedByBypass),
                values: "Value is not the same as IsApprovedByBypass");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    invalidComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidComment,
                    storageComment))
                        .ReturnsAsync(invalidComment);

            // when
            ValueTask<Comment> modifyCommentTask =
                this.commentService.ModifyCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    modifyCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    invalidComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidComment,
                    storageComment),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovedByBypassReasonChangedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Comment randomComment = CreateRandomModifyComment(randomDateTimeOffset, randomUserId);
            Comment invalidComment = randomComment;
            Comment storageComment = randomComment.DeepClone();
            storageComment.UpdatedWhen = storageComment.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            storageComment.ApprovedByBypassReason = GetRandomString();
            invalidComment.ApprovedByBypassReason = GetRandomString();

            var invalidCommentException =
                new InvalidCommentException(
                    message: "Comment is invalid, fix the errors and try again.");

            invalidCommentException.AddData(
                key: nameof(Comment.ApprovedByBypassReason),
                values: "Text is not the same as ApprovedByBypassReason");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectCommentByIdAsync(
                    invalidComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidComment,
                    storageComment))
                        .ReturnsAsync(invalidComment);

            // when
            ValueTask<Comment> modifyCommentTask =
                this.commentService.ModifyCommentAsync(
                    invalidComment,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualCommentValidationException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    modifyCommentTask.AsTask);

            // then
            actualCommentValidationException.Should().BeEquivalentTo(
                expectedCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectCommentByIdAsync(
                    invalidComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidComment,
                    storageComment),
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
