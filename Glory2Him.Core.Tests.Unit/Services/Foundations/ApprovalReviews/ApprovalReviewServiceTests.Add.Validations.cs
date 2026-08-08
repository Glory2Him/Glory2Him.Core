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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviews
{
    public partial class ApprovalReviewServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalReviewIsNullAndLogItAsync()
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
            ValueTask<ApprovalReview> addApprovalReviewTask =
                this.approvalReviewService.AddApprovalReviewAsync(
                    nullApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    addApprovalReviewTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalReviewIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
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

            // a bare review defaults StatusId to Draft, which is an entity state rather than
            // a verdict — the closed set is Approved or Rejected (design §7.7 rule 2)
            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.StatusId),
                values: "Value must be Approved or Rejected");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.CreatedBy),
                values: "Text is required");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.UpdatedBy),
                values: "Text is required");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.CreatedWhen),
                values: new[]
                {
                    "Date is required",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.UpdatedWhen),
                values: "Date is required");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalReview> addApprovalReviewTask =
                this.approvalReviewService.AddApprovalReviewAsync(
                    invalidApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    addApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalReviewTextExceedsMaxLengthAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReview invalidApprovalReview =
                CreateApprovalReviewFiller(randomDateTimeOffset, randomUserId).Create();

            invalidApprovalReview.ReviewerId = GetRandomStringWithLengthOf(451);

            var invalidApprovalReviewException =
                new InvalidApprovalReviewException(
                    message: "Approval review is invalid, fix the errors and try again.");

            // two rules now fire on this field: the length cap, and the actor binding — the
            // over-long ReviewerId is by definition not the caller's id
            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.ReviewerId),
                values: new[]
                {
                    $"Text exceed max length of {invalidApprovalReview.ReviewerId.Length - 1} characters",
                    $"Expected value to be '{randomUserId}' but found '{invalidApprovalReview.ReviewerId}'."
                });

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
                broker.ApplyAddAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalReview> addApprovalReviewTask =
                this.approvalReviewService.AddApprovalReviewAsync(
                    invalidApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    addApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowValidationExceptionOnAddIfUpdatedWhenIsNotSameAsCreatedWhenAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalReview randomApprovalReview = CreateApprovalReviewFiller(randomDateTimeOffset, randomUserId).Create();
            ApprovalReview invalidApprovalReview = randomApprovalReview;
            invalidApprovalReview.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidApprovalReviewException =
                new InvalidApprovalReviewException(
                    message: "Approval review is invalid, fix the errors and try again.");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.UpdatedWhen),
                values: $"Date is not the same as {nameof(ApprovalReview.CreatedWhen)}");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalReview> addApprovalReviewTask =
                this.approvalReviewService.AddApprovalReviewAsync(
                    invalidApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    addApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowValidationExceptionOnAddIfCreatedByIsNotSameAsCurrentUserIdAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            string randomUserId = GetRandomString();
            string differentUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalReview randomApprovalReview = CreateApprovalReviewFiller(randomDateTimeOffset, randomUserId).Create();
            ApprovalReview invalidApprovalReview = randomApprovalReview;
            invalidApprovalReview.CreatedBy = differentUserId;
            invalidApprovalReview.UpdatedBy = differentUserId;

            var invalidApprovalReviewException =
                new InvalidApprovalReviewException(
                    message: "Approval review is invalid, fix the errors and try again.");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.CreatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalReview> addApprovalReviewTask =
                this.approvalReviewService.AddApprovalReviewAsync(
                    invalidApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    addApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()),
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

        public static TheoryData<ApprovalStatus> NonVerdictReviewStatuses() =>
            new TheoryData<ApprovalStatus>
            {
                ApprovalStatus.Draft,
                ApprovalStatus.Submitted,
                ApprovalStatus.Dismissed,
                (ApprovalStatus)int.MaxValue
            };

        [Theory]
        [MemberData(nameof(NonVerdictReviewStatuses))]
        public async Task ShouldThrowValidationExceptionOnAddIfStatusIsNotAVerdictAndLogItAsync(
            ApprovalStatus nonVerdictStatus)
        {
            // given: a review IS a verdict, so the set it may carry is closed to the two a
            // reviewer can reach. Draft and Submitted are entity states. Dismissed is what
            // HAPPENS to a review when an entity-scoped change invalidates it (§9.5) — a
            // reviewer who could declare it would retract a rejection without recording a
            // verdict, leaving no trace of the change. And StatusId persists as an int, so
            // without this rule nothing at all refuses an undefined member.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReview invalidApprovalReview =
                CreateApprovalReviewFiller(randomDateTimeOffset, randomUserId).Create();

            invalidApprovalReview.StatusId = nonVerdictStatus;

            var invalidApprovalReviewException =
                new InvalidApprovalReviewException(
                    message: "Approval review is invalid, fix the errors and try again.");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.StatusId),
                values: "Value must be Approved or Rejected");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalReview> addApprovalReviewTask =
                this.approvalReviewService.AddApprovalReviewAsync(
                    invalidApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    addApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfReviewerIdIsNotSameAsCurrentUserIdAndLogItAsync()
        {
            // given: the ballot-stuffing guard. ReviewerId is the second half of
            // UX_ApprovalReviews_ApprovalId_ReviewerId, which is the ONLY place design §7.7
            // rule 1 — one active review per reviewer per approval — is expressed. Left
            // caller-supplied it is free text: one reviewer files three verdicts under three
            // invented ids, clears the index each time, and meets RequiredNumberOfApprovals
            // = 3 single-handed. CreatedBy is left matching the caller here so the failure
            // can only come from the ReviewerId rule.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            string randomUserId = GetRandomString();
            string differentUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReview randomApprovalReview =
                CreateApprovalReviewFiller(randomDateTimeOffset, randomUserId).Create();

            ApprovalReview invalidApprovalReview = randomApprovalReview;
            invalidApprovalReview.ReviewerId = differentUserId;

            var invalidApprovalReviewException =
                new InvalidApprovalReviewException(
                    message: "Approval review is invalid, fix the errors and try again.");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.ReviewerId),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalReview> addApprovalReviewTask =
                this.approvalReviewService.AddApprovalReviewAsync(
                    invalidApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    addApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowValidationExceptionOnAddIfUpdatedByIsNotSameAsCreatedByAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalReview randomApprovalReview = CreateApprovalReviewFiller(randomDateTimeOffset, randomUserId).Create();
            ApprovalReview invalidApprovalReview = randomApprovalReview;
            invalidApprovalReview.UpdatedBy = GetRandomString();

            var invalidApprovalReviewException =
                new InvalidApprovalReviewException(
                    message: "Approval review is invalid, fix the errors and try again.");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.UpdatedBy),
                values: $"Text is not the same as {nameof(ApprovalReview.CreatedBy)}");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalReview> addApprovalReviewTask =
                this.approvalReviewService.AddApprovalReviewAsync(
                    invalidApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    addApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowValidationExceptionOnAddIfCreatedWhenIsNotRecentAndLogItAsync(int minutes)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalReview randomApprovalReview = CreateApprovalReviewFiller(randomDateTimeOffset, randomUserId).Create();
            ApprovalReview invalidApprovalReview = randomApprovalReview;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidApprovalReview.CreatedWhen = randomDateTimeOffset.AddMinutes(minutes);
            invalidApprovalReview.UpdatedWhen = invalidApprovalReview.CreatedWhen;

            var invalidApprovalReviewException =
                new InvalidApprovalReviewException(
                    message: "Approval review is invalid, fix the errors and try again.");

            invalidApprovalReviewException.AddData(
                key: nameof(ApprovalReview.CreatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidApprovalReview.CreatedWhen}");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<ApprovalReview> addApprovalReviewTask =
                this.approvalReviewService.AddApprovalReviewAsync(
                    invalidApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    addApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalReview, It.IsAny<SecurityContext>()),
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
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnAddIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            ApprovalReview someApprovalReview = CreateRandomApprovalReview();

            var unauthorizedApprovalReviewException = new UnauthorizedApprovalReviewException(
                message: "The current user is not authenticated.");

            var expectedApprovalReviewValidationException = new ApprovalReviewValidationException(
                message: "Approval review validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalReviewException);

            // when
            ValueTask<ApprovalReview> addApprovalReviewTask =
                this.approvalReviewService.AddApprovalReviewAsync(
                    someApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    addApprovalReviewTask.AsTask);

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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfUserIsBlockedFromContributingAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.ReadOnly);
            ApprovalReview someApprovalReview = CreateRandomApprovalReview();

            var unauthorizedApprovalReviewException = new UnauthorizedApprovalReviewException(
                message: "The current user is blocked from contributing approval reviews.");

            var expectedApprovalReviewValidationException = new ApprovalReviewValidationException(
                message: "Approval review validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalReviewException);

            // when
            ValueTask<ApprovalReview> addApprovalReviewTask =
                this.approvalReviewService.AddApprovalReviewAsync(
                    someApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    addApprovalReviewTask.AsTask);

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
        [MemberData(nameof(NonReviewRoleSets))]
        public async Task ShouldThrowValidationExceptionOnAddIfUserHasNoReviewRoleAndLogItAsync(
            string[] nonReviewRoles)
        {
            // given: only reviewers review (§8.9) — a submitter may not record a verdict
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonReviewRoles);
            ApprovalReview someApprovalReview = CreateRandomApprovalReview();

            var unauthorizedApprovalReviewException = new UnauthorizedApprovalReviewException(
                message: "The current user is not allowed to review approvals.");

            var expectedApprovalReviewValidationException = new ApprovalReviewValidationException(
                message: "Approval review validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalReviewException);

            // when
            ValueTask<ApprovalReview> addApprovalReviewTask =
                this.approvalReviewService.AddApprovalReviewAsync(
                    someApprovalReview,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    addApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
