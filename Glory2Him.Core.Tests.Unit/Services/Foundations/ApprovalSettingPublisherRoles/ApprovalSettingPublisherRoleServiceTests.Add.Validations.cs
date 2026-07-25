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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingPublisherRoles
{
    public partial class ApprovalSettingPublisherRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalSettingPublisherRoleIsNullAndLogItAsync()
        {
            // given
            ApprovalSettingPublisherRole nullApprovalSettingPublisherRole = null;

            var nullApprovalSettingPublisherRoleException =
                new NullApprovalSettingPublisherRoleException(message: "Approval setting publisher role is null.");

            var expectedApprovalSettingPublisherRoleValidationException =
                new ApprovalSettingPublisherRoleValidationException(
                    message: "Approval setting publisher role validation error occurred, fix the errors and try again.",
                    innerException: nullApprovalSettingPublisherRoleException);

            // when
            ValueTask<ApprovalSettingPublisherRole> addApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.AddApprovalSettingPublisherRoleAsync(
                    nullApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleValidationException actualApprovalSettingPublisherRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleValidationException>(
                    addApprovalSettingPublisherRoleTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleValidationException))),
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
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalSettingPublisherRoleIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidApprovalSettingPublisherRole = new ApprovalSettingPublisherRole
            {
                Id = Guid.Empty,
                ApprovalSettingId = Guid.Empty,
                RoleName = invalidText,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidApprovalSettingPublisherRoleException =
                new InvalidApprovalSettingPublisherRoleException(
                    message: "Approval setting publisher role is invalid, fix the errors and try again.");

            invalidApprovalSettingPublisherRoleException.AddData(
                key: nameof(ApprovalSettingPublisherRole.Id),
                values: "Id is required");

            invalidApprovalSettingPublisherRoleException.AddData(
                key: nameof(ApprovalSettingPublisherRole.RoleName),
                values: "Text is required");

            invalidApprovalSettingPublisherRoleException.AddData(
                key: nameof(ApprovalSettingPublisherRole.ApprovalSettingId),
                values: "Id is required");

            invalidApprovalSettingPublisherRoleException.AddData(
                key: nameof(ApprovalSettingPublisherRole.CreatedBy),
                values: "Text is required");

            invalidApprovalSettingPublisherRoleException.AddData(
                key: nameof(ApprovalSettingPublisherRole.UpdatedBy),
                values: "Text is required");

            invalidApprovalSettingPublisherRoleException.AddData(
                key: nameof(ApprovalSettingPublisherRole.CreatedWhen),
                values: new[]
                {
                    "Date is required",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            invalidApprovalSettingPublisherRoleException.AddData(
                key: nameof(ApprovalSettingPublisherRole.UpdatedWhen),
                values: "Date is required");

            var expectedApprovalSettingPublisherRoleValidationException =
                new ApprovalSettingPublisherRoleValidationException(
                    message: "Approval setting publisher role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingPublisherRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingPublisherRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingPublisherRole> addApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.AddApprovalSettingPublisherRoleAsync(
                    invalidApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleValidationException actualApprovalSettingPublisherRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleValidationException>(
                    addApprovalSettingPublisherRoleTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalSettingPublisherRoleRoleNameExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalSettingPublisherRole invalidApprovalSettingPublisherRole =
                CreateApprovalSettingPublisherRoleFiller(randomDateTimeOffset, randomUserId).Create();

            invalidApprovalSettingPublisherRole.RoleName = GetRandomStringWithLengthOf(256);

            var invalidApprovalSettingPublisherRoleException =
                new InvalidApprovalSettingPublisherRoleException(
                    message: "Approval setting publisher role is invalid, fix the errors and try again.");

            invalidApprovalSettingPublisherRoleException.AddData(
                key: nameof(ApprovalSettingPublisherRole.RoleName),

                values: "Text exceed max length of " +
                    $"{invalidApprovalSettingPublisherRole.RoleName.Length - 1} characters");

            invalidApprovalSettingPublisherRoleException.AddData(
                key: nameof(ApprovalSettingPublisherRole.CreatedBy),
                values: $"Text exceed max length of {invalidApprovalSettingPublisherRole.CreatedBy.Length - 1} characters");

            invalidApprovalSettingPublisherRoleException.AddData(
                key: nameof(ApprovalSettingPublisherRole.UpdatedBy),
                values: $"Text exceed max length of {invalidApprovalSettingPublisherRole.UpdatedBy.Length - 1} characters");

            var expectedApprovalSettingPublisherRoleValidationException =
                new ApprovalSettingPublisherRoleValidationException(
                    message: "Approval setting publisher role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingPublisherRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingPublisherRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingPublisherRole> addApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.AddApprovalSettingPublisherRoleAsync(
                    invalidApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleValidationException actualApprovalSettingPublisherRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleValidationException>(
                    addApprovalSettingPublisherRoleTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleValidationException))),
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
            ApprovalSettingPublisherRole randomApprovalSettingPublisherRole = CreateApprovalSettingPublisherRoleFiller(randomDateTimeOffset, randomUserId).Create();
            ApprovalSettingPublisherRole invalidApprovalSettingPublisherRole = randomApprovalSettingPublisherRole;
            invalidApprovalSettingPublisherRole.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidApprovalSettingPublisherRoleException =
                new InvalidApprovalSettingPublisherRoleException(
                    message: "Approval setting publisher role is invalid, fix the errors and try again.");

            invalidApprovalSettingPublisherRoleException.AddData(
                key: nameof(ApprovalSettingPublisherRole.UpdatedWhen),
                values: $"Date is not the same as {nameof(ApprovalSettingPublisherRole.CreatedWhen)}");

            var expectedApprovalSettingPublisherRoleValidationException =
                new ApprovalSettingPublisherRoleValidationException(
                    message: "Approval setting publisher role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingPublisherRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingPublisherRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingPublisherRole> addApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.AddApprovalSettingPublisherRoleAsync(
                    invalidApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleValidationException actualApprovalSettingPublisherRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleValidationException>(
                    addApprovalSettingPublisherRoleTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleValidationException))),
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
            ApprovalSettingPublisherRole randomApprovalSettingPublisherRole = CreateApprovalSettingPublisherRoleFiller(randomDateTimeOffset, randomUserId).Create();
            ApprovalSettingPublisherRole invalidApprovalSettingPublisherRole = randomApprovalSettingPublisherRole;
            invalidApprovalSettingPublisherRole.CreatedBy = differentUserId;
            invalidApprovalSettingPublisherRole.UpdatedBy = differentUserId;

            var invalidApprovalSettingPublisherRoleException =
                new InvalidApprovalSettingPublisherRoleException(
                    message: "Approval setting publisher role is invalid, fix the errors and try again.");

            invalidApprovalSettingPublisherRoleException.AddData(
                key: nameof(ApprovalSettingPublisherRole.CreatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedApprovalSettingPublisherRoleValidationException =
                new ApprovalSettingPublisherRoleValidationException(
                    message: "Approval setting publisher role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingPublisherRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingPublisherRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingPublisherRole> addApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.AddApprovalSettingPublisherRoleAsync(
                    invalidApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleValidationException actualApprovalSettingPublisherRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleValidationException>(
                    addApprovalSettingPublisherRoleTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleValidationException))),
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
            ApprovalSettingPublisherRole randomApprovalSettingPublisherRole = CreateApprovalSettingPublisherRoleFiller(randomDateTimeOffset, randomUserId).Create();
            ApprovalSettingPublisherRole invalidApprovalSettingPublisherRole = randomApprovalSettingPublisherRole;
            invalidApprovalSettingPublisherRole.UpdatedBy = GetRandomString();

            var invalidApprovalSettingPublisherRoleException =
                new InvalidApprovalSettingPublisherRoleException(
                    message: "Approval setting publisher role is invalid, fix the errors and try again.");

            invalidApprovalSettingPublisherRoleException.AddData(
                key: nameof(ApprovalSettingPublisherRole.UpdatedBy),
                values: $"Text is not the same as {nameof(ApprovalSettingPublisherRole.CreatedBy)}");

            var expectedApprovalSettingPublisherRoleValidationException =
                new ApprovalSettingPublisherRoleValidationException(
                    message: "Approval setting publisher role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingPublisherRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingPublisherRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingPublisherRole> addApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.AddApprovalSettingPublisherRoleAsync(
                    invalidApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleValidationException actualApprovalSettingPublisherRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleValidationException>(
                    addApprovalSettingPublisherRoleTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleValidationException))),
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
            ApprovalSettingPublisherRole randomApprovalSettingPublisherRole = CreateApprovalSettingPublisherRoleFiller(randomDateTimeOffset, randomUserId).Create();
            ApprovalSettingPublisherRole invalidApprovalSettingPublisherRole = randomApprovalSettingPublisherRole;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidApprovalSettingPublisherRole.CreatedWhen = randomDateTimeOffset.AddMinutes(minutes);
            invalidApprovalSettingPublisherRole.UpdatedWhen = invalidApprovalSettingPublisherRole.CreatedWhen;

            var invalidApprovalSettingPublisherRoleException =
                new InvalidApprovalSettingPublisherRoleException(
                    message: "Approval setting publisher role is invalid, fix the errors and try again.");

            invalidApprovalSettingPublisherRoleException.AddData(
                key: nameof(ApprovalSettingPublisherRole.CreatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidApprovalSettingPublisherRole.CreatedWhen}");

            var expectedApprovalSettingPublisherRoleValidationException =
                new ApprovalSettingPublisherRoleValidationException(
                    message: "Approval setting publisher role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingPublisherRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingPublisherRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<ApprovalSettingPublisherRole> addApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.AddApprovalSettingPublisherRoleAsync(
                    invalidApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleValidationException actualApprovalSettingPublisherRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleValidationException>(
                    addApprovalSettingPublisherRoleTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
