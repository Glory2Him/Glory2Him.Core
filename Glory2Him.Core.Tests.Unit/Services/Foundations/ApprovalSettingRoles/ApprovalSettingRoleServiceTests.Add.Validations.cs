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
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingRoles
{
    public partial class ApprovalSettingRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalSettingRoleIsNullAndLogItAsync()
        {
            // given
            ApprovalSettingRole nullApprovalSettingRole = null;

            var nullApprovalSettingRoleException =
                new NullApprovalSettingRoleException(message: "Approval setting role is null.");

            var expectedApprovalSettingRoleValidationException =
                new ApprovalSettingRoleValidationException(
                    message: "Approval setting role validation error occurred, fix the errors and try again.",
                    innerException: nullApprovalSettingRoleException);

            // when
            ValueTask<ApprovalSettingRole> addApprovalSettingRoleTask =
                this.approvalSettingRoleService.AddApprovalSettingRoleAsync(
                    nullApprovalSettingRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleValidationException actualApprovalSettingRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingRoleValidationException>(
                    addApprovalSettingRoleTask.AsTask);

            // then
            actualApprovalSettingRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleValidationException))),
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
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalSettingRoleIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidApprovalSettingRole = new ApprovalSettingRole
            {
                Id = Guid.Empty,
                ApprovalSettingId = Guid.Empty,
                RoleName = invalidText,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidApprovalSettingRoleException =
                new InvalidApprovalSettingRoleException(
                    message: "Approval setting role is invalid, fix the errors and try again.");

            invalidApprovalSettingRoleException.AddData(
                key: nameof(ApprovalSettingRole.Id),
                values: "Id is required");

            invalidApprovalSettingRoleException.AddData(
                key: nameof(ApprovalSettingRole.RoleName),
                values: "Text is required");

            invalidApprovalSettingRoleException.AddData(
                key: nameof(ApprovalSettingRole.ApprovalSettingId),
                values: "Id is required");

            invalidApprovalSettingRoleException.AddData(
                key: nameof(ApprovalSettingRole.CreatedBy),
                values: "Text is required");

            invalidApprovalSettingRoleException.AddData(
                key: nameof(ApprovalSettingRole.UpdatedBy),
                values: "Text is required");

            invalidApprovalSettingRoleException.AddData(
                key: nameof(ApprovalSettingRole.CreatedWhen),
                values: new[]
                {
                    "Date is required",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            invalidApprovalSettingRoleException.AddData(
                key: nameof(ApprovalSettingRole.UpdatedWhen),
                values: "Date is required");

            var expectedApprovalSettingRoleValidationException =
                new ApprovalSettingRoleValidationException(
                    message: "Approval setting role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingRole> addApprovalSettingRoleTask =
                this.approvalSettingRoleService.AddApprovalSettingRoleAsync(
                    invalidApprovalSettingRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleValidationException actualApprovalSettingRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingRoleValidationException>(
                    addApprovalSettingRoleTask.AsTask);

            // then
            actualApprovalSettingRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalSettingRoleRoleNameExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalSettingRole invalidApprovalSettingRole =
                CreateApprovalSettingRoleFiller(randomDateTimeOffset, randomUserId).Create();

            invalidApprovalSettingRole.RoleName = GetRandomStringWithLengthOf(256);

            var invalidApprovalSettingRoleException =
                new InvalidApprovalSettingRoleException(
                    message: "Approval setting role is invalid, fix the errors and try again.");

            invalidApprovalSettingRoleException.AddData(
                key: nameof(ApprovalSettingRole.RoleName),

                values: "Text exceed max length of " +
                    $"{invalidApprovalSettingRole.RoleName.Length - 1} characters");

            invalidApprovalSettingRoleException.AddData(
                key: nameof(ApprovalSettingRole.CreatedBy),
                values: $"Text exceed max length of {invalidApprovalSettingRole.CreatedBy.Length - 1} characters");

            invalidApprovalSettingRoleException.AddData(
                key: nameof(ApprovalSettingRole.UpdatedBy),
                values: $"Text exceed max length of {invalidApprovalSettingRole.UpdatedBy.Length - 1} characters");

            var expectedApprovalSettingRoleValidationException =
                new ApprovalSettingRoleValidationException(
                    message: "Approval setting role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingRole> addApprovalSettingRoleTask =
                this.approvalSettingRoleService.AddApprovalSettingRoleAsync(
                    invalidApprovalSettingRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleValidationException actualApprovalSettingRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingRoleValidationException>(
                    addApprovalSettingRoleTask.AsTask);

            // then
            actualApprovalSettingRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleValidationException))),
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
            ApprovalSettingRole randomApprovalSettingRole = CreateApprovalSettingRoleFiller(randomDateTimeOffset, randomUserId).Create();
            ApprovalSettingRole invalidApprovalSettingRole = randomApprovalSettingRole;
            invalidApprovalSettingRole.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidApprovalSettingRoleException =
                new InvalidApprovalSettingRoleException(
                    message: "Approval setting role is invalid, fix the errors and try again.");

            invalidApprovalSettingRoleException.AddData(
                key: nameof(ApprovalSettingRole.UpdatedWhen),
                values: $"Date is not the same as {nameof(ApprovalSettingRole.CreatedWhen)}");

            var expectedApprovalSettingRoleValidationException =
                new ApprovalSettingRoleValidationException(
                    message: "Approval setting role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingRole> addApprovalSettingRoleTask =
                this.approvalSettingRoleService.AddApprovalSettingRoleAsync(
                    invalidApprovalSettingRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleValidationException actualApprovalSettingRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingRoleValidationException>(
                    addApprovalSettingRoleTask.AsTask);

            // then
            actualApprovalSettingRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleValidationException))),
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
            ApprovalSettingRole randomApprovalSettingRole = CreateApprovalSettingRoleFiller(randomDateTimeOffset, randomUserId).Create();
            ApprovalSettingRole invalidApprovalSettingRole = randomApprovalSettingRole;
            invalidApprovalSettingRole.CreatedBy = differentUserId;
            invalidApprovalSettingRole.UpdatedBy = differentUserId;

            var invalidApprovalSettingRoleException =
                new InvalidApprovalSettingRoleException(
                    message: "Approval setting role is invalid, fix the errors and try again.");

            invalidApprovalSettingRoleException.AddData(
                key: nameof(ApprovalSettingRole.CreatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedApprovalSettingRoleValidationException =
                new ApprovalSettingRoleValidationException(
                    message: "Approval setting role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingRole> addApprovalSettingRoleTask =
                this.approvalSettingRoleService.AddApprovalSettingRoleAsync(
                    invalidApprovalSettingRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleValidationException actualApprovalSettingRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingRoleValidationException>(
                    addApprovalSettingRoleTask.AsTask);

            // then
            actualApprovalSettingRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleValidationException))),
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
            ApprovalSettingRole randomApprovalSettingRole = CreateApprovalSettingRoleFiller(randomDateTimeOffset, randomUserId).Create();
            ApprovalSettingRole invalidApprovalSettingRole = randomApprovalSettingRole;
            invalidApprovalSettingRole.UpdatedBy = GetRandomString();

            var invalidApprovalSettingRoleException =
                new InvalidApprovalSettingRoleException(
                    message: "Approval setting role is invalid, fix the errors and try again.");

            invalidApprovalSettingRoleException.AddData(
                key: nameof(ApprovalSettingRole.UpdatedBy),
                values: $"Text is not the same as {nameof(ApprovalSettingRole.CreatedBy)}");

            var expectedApprovalSettingRoleValidationException =
                new ApprovalSettingRoleValidationException(
                    message: "Approval setting role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingRole> addApprovalSettingRoleTask =
                this.approvalSettingRoleService.AddApprovalSettingRoleAsync(
                    invalidApprovalSettingRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleValidationException actualApprovalSettingRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingRoleValidationException>(
                    addApprovalSettingRoleTask.AsTask);

            // then
            actualApprovalSettingRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleValidationException))),
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
            ApprovalSettingRole randomApprovalSettingRole = CreateApprovalSettingRoleFiller(randomDateTimeOffset, randomUserId).Create();
            ApprovalSettingRole invalidApprovalSettingRole = randomApprovalSettingRole;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidApprovalSettingRole.CreatedWhen = randomDateTimeOffset.AddMinutes(minutes);
            invalidApprovalSettingRole.UpdatedWhen = invalidApprovalSettingRole.CreatedWhen;

            var invalidApprovalSettingRoleException =
                new InvalidApprovalSettingRoleException(
                    message: "Approval setting role is invalid, fix the errors and try again.");

            invalidApprovalSettingRoleException.AddData(
                key: nameof(ApprovalSettingRole.CreatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidApprovalSettingRole.CreatedWhen}");

            var expectedApprovalSettingRoleValidationException =
                new ApprovalSettingRoleValidationException(
                    message: "Approval setting role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<ApprovalSettingRole> addApprovalSettingRoleTask =
                this.approvalSettingRoleService.AddApprovalSettingRoleAsync(
                    invalidApprovalSettingRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleValidationException actualApprovalSettingRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingRoleValidationException>(
                    addApprovalSettingRoleTask.AsTask);

            // then
            actualApprovalSettingRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
