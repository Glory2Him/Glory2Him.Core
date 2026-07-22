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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviews
{
    public partial class ApprovalReviewServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalReviewIsNullAndLogItAsync()
        {
            // given
            ApprovalReview nullApprovalReview = null;

            var nullApprovalReviewException =
                new NullApprovalReviewException(message: "Approval review is null.");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: nullApprovalReviewException);

            // when
            ValueTask<ApprovalReview> modifyApprovalReviewTask =
                this.approvalReviewService.ModifyApprovalReviewAsync(
                    nullApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    modifyApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalReviewIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidApprovalReview = new ApprovalReview
            {
                Id = Guid.Empty,
                ApprovalId = Guid.Empty,
                ReviewerId = invalidText,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidApprovalReviewException =
                new InvalidApprovalReviewException(
                    message: "Approval review is invalid, fix the errors and try again.");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.Id),
                values: "Id is required");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.ApprovalId),
                values: "Id is required");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.ReviewerId),
                values: "Text is required");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.CreatedBy),
                values: "Text is required");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.UpdatedBy),
                values: "Text is required");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.CreatedWhen),
                values: "Date is required");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.UpdatedWhen),
                values: new[]
                {
                    "Date is required",
                    "Date is the same as CreatedWhen",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalReview> modifyApprovalReviewTask =
                this.approvalReviewService.ModifyApprovalReviewAsync(
                    invalidApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    modifyApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalReviewNotFoundAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ApprovalReview randomApprovalReview = CreateRandomModifyApprovalReview(randomDateTimeOffset, randomUserId);
            ApprovalReview nonExistentApprovalReview = randomApprovalReview;
            ApprovalReview noApprovalReview = null;

            var notFoundApprovalReviewException = new NotFoundApprovalReviewException(
                message: $"Approval review not found with id: {nonExistentApprovalReview.Id}.");

            var expectedApprovalReviewValidationException = new ApprovalReviewValidationException(
                message: "Approval review validation error occurred, fix the errors and try again.",
                innerException: notFoundApprovalReviewException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(nonExistentApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    nonExistentApprovalReview.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noApprovalReview);

            // when
            ValueTask<ApprovalReview> modifyApprovalReviewTask =
                this.approvalReviewService.ModifyApprovalReviewAsync(
                    nonExistentApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    modifyApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentApprovalReview, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    nonExistentApprovalReview.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
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
            ApprovalReview randomApprovalReview = CreateRandomModifyApprovalReview(randomDateTimeOffset, randomUserId);
            ApprovalReview invalidApprovalReview = randomApprovalReview;
            ApprovalReview storageApprovalReview = randomApprovalReview.DeepClone();
            storageApprovalReview.CreatedWhen = GetRandomDateTimeOffset();
            storageApprovalReview.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidApprovalReviewException = new InvalidApprovalReviewException(
                message: "Approval review is invalid, fix the errors and try again.");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.CreatedWhen),
                values: $"Date is not the same as {nameof(ApprovalReview.CreatedWhen)}");

            var expectedApprovalReviewValidationException = new ApprovalReviewValidationException(
                message: "Approval review validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalReviewException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    invalidApprovalReview.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalReview,
                    storageApprovalReview))
                        .ReturnsAsync(invalidApprovalReview);

            // when
            ValueTask<ApprovalReview> modifyApprovalReviewTask =
                this.approvalReviewService.ModifyApprovalReviewAsync(
                    invalidApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    modifyApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    invalidApprovalReview.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalReview,
                    storageApprovalReview),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
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
            ApprovalReview randomApprovalReview = CreateRandomModifyApprovalReview(randomDateTimeOffset, randomUserId);
            ApprovalReview invalidApprovalReview = randomApprovalReview;
            ApprovalReview storageApprovalReview = randomApprovalReview.DeepClone();
            storageApprovalReview.CreatedBy = GetRandomString();
            storageApprovalReview.UpdatedWhen = storageApprovalReview.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidApprovalReviewException =
                new InvalidApprovalReviewException(
                    message: "Approval review is invalid, fix the errors and try again.");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.CreatedBy),
                values: $"Text is not the same as {nameof(ApprovalReview.CreatedBy)}");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    invalidApprovalReview.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalReview,
                    storageApprovalReview))
                        .ReturnsAsync(invalidApprovalReview);

            // when
            ValueTask<ApprovalReview> modifyApprovalReviewTask =
                this.approvalReviewService.ModifyApprovalReviewAsync(
                    invalidApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    modifyApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    invalidApprovalReview.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalReview,
                    storageApprovalReview),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
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
            ApprovalReview randomApprovalReview = CreateRandomModifyApprovalReview(randomDateTimeOffset, randomUserId);
            ApprovalReview invalidApprovalReview = randomApprovalReview;
            ApprovalReview storageApprovalReview = randomApprovalReview.DeepClone();

            var invalidApprovalReviewException =
                new InvalidApprovalReviewException(
                    message: "Approval review is invalid, fix the errors and try again.");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.UpdatedWhen),
                values: $"Date is the same as {nameof(ApprovalReview.UpdatedWhen)}");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    invalidApprovalReview.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalReview,
                    storageApprovalReview))
                        .ReturnsAsync(invalidApprovalReview);

            // when
            ValueTask<ApprovalReview> modifyApprovalReviewTask =
                this.approvalReviewService.ModifyApprovalReviewAsync(
                    invalidApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    modifyApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    invalidApprovalReview.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalReview,
                    storageApprovalReview),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
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
            ApprovalReview randomApprovalReview = CreateRandomModifyApprovalReview(randomDateTimeOffset, randomUserId);
            ApprovalReview invalidApprovalReview = randomApprovalReview;
            invalidApprovalReview.UpdatedBy = differentUserId;

            var invalidApprovalReviewException =
                new InvalidApprovalReviewException(
                    message: "Approval review is invalid, fix the errors and try again.");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.UpdatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalReview> modifyApprovalReviewTask =
                this.approvalReviewService.ModifyApprovalReviewAsync(
                    invalidApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    modifyApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
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
            ApprovalReview randomApprovalReview = CreateRandomModifyApprovalReview(randomDateTimeOffset, randomUserId);
            ApprovalReview invalidApprovalReview = randomApprovalReview;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidApprovalReview.UpdatedWhen = invalidApprovalReview.CreatedWhen;

            var invalidApprovalReviewException =
                new InvalidApprovalReviewException(
                    message: "Approval review is invalid, fix the errors and try again.");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.UpdatedWhen),
                values: new[]
                {
                    $"Date is the same as {nameof(ApprovalReview.CreatedWhen)}",
                    $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                        $"but found {invalidApprovalReview.UpdatedWhen}"
                });

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalReview> modifyApprovalReviewTask =
                this.approvalReviewService.ModifyApprovalReviewAsync(
                    invalidApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    modifyApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
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
            ApprovalReview randomApprovalReview = CreateRandomModifyApprovalReview(randomDateTimeOffset, randomUserId);
            ApprovalReview invalidApprovalReview = randomApprovalReview;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidApprovalReview.UpdatedWhen = randomDateTimeOffset.AddMinutes(minutes);

            var invalidApprovalReviewException =
                new InvalidApprovalReviewException(
                    message: "Approval review is invalid, fix the errors and try again.");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.UpdatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidApprovalReview.UpdatedWhen}");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<ApprovalReview> modifyApprovalReviewTask =
                this.approvalReviewService.ModifyApprovalReviewAsync(
                    invalidApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    modifyApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalReviewExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalReview invalidApprovalReview = CreateRandomModifyApprovalReview(randomDateTimeOffset, randomUserId);

            var invalidApprovalReviewException =
                new InvalidApprovalReviewException(
                    message: "Approval review is invalid, fix the errors and try again.");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.CreatedBy),
                values: $"Text exceed max length of {invalidApprovalReview.CreatedBy.Length - 1} characters");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.UpdatedBy),
                values: $"Text exceed max length of {invalidApprovalReview.UpdatedBy.Length - 1} characters");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalReview> modifyApprovalReviewTask =
                this.approvalReviewService.ModifyApprovalReviewAsync(
                    invalidApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    modifyApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}