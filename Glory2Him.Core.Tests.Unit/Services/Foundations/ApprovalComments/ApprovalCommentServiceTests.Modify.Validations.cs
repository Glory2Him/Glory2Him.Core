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
using Force.DeepCloner;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalComments
{
    public partial class ApprovalCommentServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalCommentIsNullAndLogItAsync()
        {
            // given
            ApprovalComment nullApprovalComment = null;

            var nullApprovalCommentException =
                new NullApprovalCommentException(message: "Approval comment is null.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: nullApprovalCommentException);

            // when
            ValueTask<ApprovalComment> modifyApprovalCommentTask =
                this.approvalCommentService.ModifyApprovalCommentAsync(
                    nullApprovalComment,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    modifyApprovalCommentTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalCommentIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidApprovalComment = new ApprovalComment
            {
                Id = Guid.Empty,
                ApprovalId = Guid.Empty,
                UserId = invalidText,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidApprovalCommentException =
                new InvalidApprovalCommentException(
                    message: "Approval comment is invalid, fix the errors and try again.");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.Id),
                values: "Id is required");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.ApprovalId),
                values: "Id is required");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.UserId),
                values: "Text is required");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.CreatedBy),
                values: "Text is required");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.UpdatedBy),
                values: "Text is required");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.CreatedWhen),
                values: "Date is required");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.UpdatedWhen),
                values: new[]
                {
                    "Date is required",
                    "Date is the same as CreatedWhen",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalComment> modifyApprovalCommentTask =
                this.approvalCommentService.ModifyApprovalCommentAsync(
                    invalidApprovalComment,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    modifyApprovalCommentTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalCommentNotFoundAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ApprovalComment randomApprovalComment =
                CreateRandomModifyApprovalComment(randomDateTimeOffset, randomUserId);

            ApprovalComment nonExistentApprovalComment = randomApprovalComment;
            ApprovalComment noApprovalComment = null;

            var notFoundApprovalCommentException = new NotFoundApprovalCommentException(
                message: $"Approval comment not found with id: {nonExistentApprovalComment.Id}.");

            var expectedApprovalCommentValidationException = new ApprovalCommentValidationException(
                message: "Approval comment validation error occurred, fix the errors and try again.",
                innerException: notFoundApprovalCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentApprovalComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(nonExistentApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    nonExistentApprovalComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noApprovalComment);

            // when
            ValueTask<ApprovalComment> modifyApprovalCommentTask =
                this.approvalCommentService.ModifyApprovalCommentAsync(
                    nonExistentApprovalComment,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    modifyApprovalCommentTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentApprovalComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    nonExistentApprovalComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
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
            ApprovalComment randomApprovalComment =
                CreateRandomModifyApprovalComment(randomDateTimeOffset, randomUserId);

            ApprovalComment invalidApprovalComment = randomApprovalComment;
            ApprovalComment storageApprovalComment = randomApprovalComment.DeepClone();
            storageApprovalComment.CreatedWhen = GetRandomDateTimeOffset();
            storageApprovalComment.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidApprovalCommentException = new InvalidApprovalCommentException(
                message: "Approval comment is invalid, fix the errors and try again.");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.CreatedWhen),
                values: $"Date is not the same as {nameof(ApprovalComment.CreatedWhen)}");

            var expectedApprovalCommentValidationException = new ApprovalCommentValidationException(
                message: "Approval comment validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    invalidApprovalComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalComment,
                    storageApprovalComment))
                        .ReturnsAsync(invalidApprovalComment);

            // when
            ValueTask<ApprovalComment> modifyApprovalCommentTask =
                this.approvalCommentService.ModifyApprovalCommentAsync(
                    invalidApprovalComment,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    modifyApprovalCommentTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    invalidApprovalComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalComment,
                    storageApprovalComment),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
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
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ApprovalComment randomApprovalComment =
                CreateRandomModifyApprovalComment(randomDateTimeOffset, randomUserId);

            ApprovalComment invalidApprovalComment = randomApprovalComment;
            ApprovalComment storageApprovalComment = randomApprovalComment.DeepClone();
            storageApprovalComment.CreatedBy = GetRandomString();
            storageApprovalComment.UpdatedWhen = storageApprovalComment.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidApprovalCommentException =
                new InvalidApprovalCommentException(
                    message: "Approval comment is invalid, fix the errors and try again.");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.CreatedBy),
                values: $"Text is not the same as {nameof(ApprovalComment.CreatedBy)}");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    invalidApprovalComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalComment,
                    storageApprovalComment))
                        .ReturnsAsync(invalidApprovalComment);

            // when
            ValueTask<ApprovalComment> modifyApprovalCommentTask =
                this.approvalCommentService.ModifyApprovalCommentAsync(
                    invalidApprovalComment,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    modifyApprovalCommentTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    invalidApprovalComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalComment,
                    storageApprovalComment),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
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
            ApprovalComment randomApprovalComment =
                CreateRandomModifyApprovalComment(randomDateTimeOffset, randomUserId);

            ApprovalComment invalidApprovalComment = randomApprovalComment;
            ApprovalComment storageApprovalComment = randomApprovalComment.DeepClone();

            var invalidApprovalCommentException =
                new InvalidApprovalCommentException(
                    message: "Approval comment is invalid, fix the errors and try again.");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.UpdatedWhen),
                values: $"Date is the same as {nameof(ApprovalComment.UpdatedWhen)}");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    invalidApprovalComment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalComment,
                    storageApprovalComment))
                        .ReturnsAsync(invalidApprovalComment);

            // when
            ValueTask<ApprovalComment> modifyApprovalCommentTask =
                this.approvalCommentService.ModifyApprovalCommentAsync(
                    invalidApprovalComment,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    modifyApprovalCommentTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    invalidApprovalComment.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalComment,
                    storageApprovalComment),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
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
            ApprovalComment randomApprovalComment =
                CreateRandomModifyApprovalComment(randomDateTimeOffset, randomUserId);

            ApprovalComment invalidApprovalComment = randomApprovalComment;
            invalidApprovalComment.UpdatedBy = differentUserId;

            var invalidApprovalCommentException =
                new InvalidApprovalCommentException(
                    message: "Approval comment is invalid, fix the errors and try again.");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.UpdatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalComment> modifyApprovalCommentTask =
                this.approvalCommentService.ModifyApprovalCommentAsync(
                    invalidApprovalComment,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    modifyApprovalCommentTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
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
            ApprovalComment randomApprovalComment =
                CreateRandomModifyApprovalComment(randomDateTimeOffset, randomUserId);

            ApprovalComment invalidApprovalComment = randomApprovalComment;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidApprovalComment.UpdatedWhen = invalidApprovalComment.CreatedWhen;

            var invalidApprovalCommentException =
                new InvalidApprovalCommentException(
                    message: "Approval comment is invalid, fix the errors and try again.");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.UpdatedWhen),
                values: new[]
                {
                    $"Date is the same as {nameof(ApprovalComment.CreatedWhen)}",
                    $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                        $"but found {invalidApprovalComment.UpdatedWhen}"
                });

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalComment> modifyApprovalCommentTask =
                this.approvalCommentService.ModifyApprovalCommentAsync(
                    invalidApprovalComment,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    modifyApprovalCommentTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
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
            ApprovalComment randomApprovalComment =
                CreateRandomModifyApprovalComment(randomDateTimeOffset, randomUserId);

            ApprovalComment invalidApprovalComment = randomApprovalComment;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidApprovalComment.UpdatedWhen = randomDateTimeOffset.AddMinutes(minutes);

            var invalidApprovalCommentException =
                new InvalidApprovalCommentException(
                    message: "Approval comment is invalid, fix the errors and try again.");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.UpdatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidApprovalComment.UpdatedWhen}");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<ApprovalComment> modifyApprovalCommentTask =
                this.approvalCommentService.ModifyApprovalCommentAsync(
                    invalidApprovalComment,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    modifyApprovalCommentTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalCommentExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalComment invalidApprovalComment =
                CreateRandomModifyApprovalComment(randomDateTimeOffset, randomUserId);


            var invalidApprovalCommentException =
                new InvalidApprovalCommentException(
                    message: "Approval comment is invalid, fix the errors and try again.");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.CreatedBy),
                values: $"Text exceed max length of {invalidApprovalComment.CreatedBy.Length - 1} characters");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.UpdatedBy),
                values: $"Text exceed max length of {invalidApprovalComment.UpdatedBy.Length - 1} characters");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalComment> modifyApprovalCommentTask =
                this.approvalCommentService.ModifyApprovalCommentAsync(
                    invalidApprovalComment,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    modifyApprovalCommentTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}