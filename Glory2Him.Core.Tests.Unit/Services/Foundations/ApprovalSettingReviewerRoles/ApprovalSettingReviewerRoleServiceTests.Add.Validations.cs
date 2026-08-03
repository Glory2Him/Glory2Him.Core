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
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingReviewerRoles
{
    public partial class ApprovalSettingReviewerRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalSettingReviewerRoleIsNullAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ApprovalSettingReviewerRole nullApprovalSettingReviewerRole = null;

            var nullApprovalSettingReviewerRoleException =
                new NullApprovalSettingReviewerRoleException(message: "Approval setting reviewer role is null.");

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: nullApprovalSettingReviewerRoleException);

            // when
            ValueTask<ApprovalSettingReviewerRole> addApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.AddApprovalSettingReviewerRoleAsync(
                    nullApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    addApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
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
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalSettingReviewerRoleIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidApprovalSettingReviewerRole = new ApprovalSettingReviewerRole
            {
                Id = Guid.Empty,
                ApprovalSettingId = Guid.Empty,
                RoleName = invalidText,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidApprovalSettingReviewerRoleException =
                new InvalidApprovalSettingReviewerRoleException(
                    message: "Approval setting reviewer role is invalid, fix the errors and try again.");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.Id),
                values: "Id is required");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.RoleName),
                values: "Text is required");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.ApprovalSettingId),
                values: "Id is required");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.CreatedBy),
                values: "Text is required");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.UpdatedBy),
                values: "Text is required");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.CreatedWhen),
                values: new[]
                {
                    "Date is required",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.UpdatedWhen),
                values: "Date is required");

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingReviewerRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingReviewerRole> addApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.AddApprovalSettingReviewerRoleAsync(
                    invalidApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    addApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalSettingReviewerRoleRoleNameExceedsMaxLengthAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalSettingReviewerRole invalidApprovalSettingReviewerRole =
                CreateApprovalSettingReviewerRoleFiller(randomDateTimeOffset, randomUserId).Create();

            invalidApprovalSettingReviewerRole.RoleName = GetRandomStringWithLengthOf(256);

            var invalidApprovalSettingReviewerRoleException =
                new InvalidApprovalSettingReviewerRoleException(
                    message: "Approval setting reviewer role is invalid, fix the errors and try again.");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.RoleName),

                values: "Text exceed max length of " +
                    $"{invalidApprovalSettingReviewerRole.RoleName.Length - 1} characters");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.CreatedBy),
                values: $"Text exceed max length of {invalidApprovalSettingReviewerRole.CreatedBy.Length - 1} characters");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.UpdatedBy),
                values: $"Text exceed max length of {invalidApprovalSettingReviewerRole.UpdatedBy.Length - 1} characters");

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingReviewerRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingReviewerRole> addApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.AddApprovalSettingReviewerRoleAsync(
                    invalidApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    addApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSettingReviewerRole randomApprovalSettingReviewerRole = CreateApprovalSettingReviewerRoleFiller(randomDateTimeOffset, randomUserId).Create();
            ApprovalSettingReviewerRole invalidApprovalSettingReviewerRole = randomApprovalSettingReviewerRole;
            invalidApprovalSettingReviewerRole.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidApprovalSettingReviewerRoleException =
                new InvalidApprovalSettingReviewerRoleException(
                    message: "Approval setting reviewer role is invalid, fix the errors and try again.");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.UpdatedWhen),
                values: $"Date is not the same as {nameof(ApprovalSettingReviewerRole.CreatedWhen)}");

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingReviewerRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingReviewerRole> addApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.AddApprovalSettingReviewerRoleAsync(
                    invalidApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    addApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            string randomUserId = GetRandomString();
            string differentUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSettingReviewerRole randomApprovalSettingReviewerRole = CreateApprovalSettingReviewerRoleFiller(randomDateTimeOffset, randomUserId).Create();
            ApprovalSettingReviewerRole invalidApprovalSettingReviewerRole = randomApprovalSettingReviewerRole;
            invalidApprovalSettingReviewerRole.CreatedBy = differentUserId;
            invalidApprovalSettingReviewerRole.UpdatedBy = differentUserId;

            var invalidApprovalSettingReviewerRoleException =
                new InvalidApprovalSettingReviewerRoleException(
                    message: "Approval setting reviewer role is invalid, fix the errors and try again.");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.CreatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingReviewerRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingReviewerRole> addApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.AddApprovalSettingReviewerRoleAsync(
                    invalidApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    addApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSettingReviewerRole randomApprovalSettingReviewerRole = CreateApprovalSettingReviewerRoleFiller(randomDateTimeOffset, randomUserId).Create();
            ApprovalSettingReviewerRole invalidApprovalSettingReviewerRole = randomApprovalSettingReviewerRole;
            invalidApprovalSettingReviewerRole.UpdatedBy = GetRandomString();

            var invalidApprovalSettingReviewerRoleException =
                new InvalidApprovalSettingReviewerRoleException(
                    message: "Approval setting reviewer role is invalid, fix the errors and try again.");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.UpdatedBy),
                values: $"Text is not the same as {nameof(ApprovalSettingReviewerRole.CreatedBy)}");

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingReviewerRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingReviewerRole> addApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.AddApprovalSettingReviewerRoleAsync(
                    invalidApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    addApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSettingReviewerRole randomApprovalSettingReviewerRole = CreateApprovalSettingReviewerRoleFiller(randomDateTimeOffset, randomUserId).Create();
            ApprovalSettingReviewerRole invalidApprovalSettingReviewerRole = randomApprovalSettingReviewerRole;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidApprovalSettingReviewerRole.CreatedWhen = randomDateTimeOffset.AddMinutes(minutes);
            invalidApprovalSettingReviewerRole.UpdatedWhen = invalidApprovalSettingReviewerRole.CreatedWhen;

            var invalidApprovalSettingReviewerRoleException =
                new InvalidApprovalSettingReviewerRoleException(
                    message: "Approval setting reviewer role is invalid, fix the errors and try again.");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.CreatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidApprovalSettingReviewerRole.CreatedWhen}");

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingReviewerRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<ApprovalSettingReviewerRole> addApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.AddApprovalSettingReviewerRoleAsync(
                    invalidApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    addApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
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
            ApprovalSettingReviewerRole someApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();

            var unauthorizedApprovalSettingReviewerRoleException = new UnauthorizedApprovalSettingReviewerRoleException(
                message: "The current user is not authenticated.");

            var expectedApprovalSettingReviewerRoleValidationException = new ApprovalSettingReviewerRoleValidationException(
                message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalSettingReviewerRoleException);

            // when
            ValueTask<ApprovalSettingReviewerRole> addApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.AddApprovalSettingReviewerRoleAsync(
                    someApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    addApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(NonAdminRoleSets))]
        public async Task ShouldThrowValidationExceptionOnAddIfUserIsNotAdminAndLogItAsync(
            string[] nonAdminRoles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonAdminRoles);
            ApprovalSettingReviewerRole someApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();

            var unauthorizedApprovalSettingReviewerRoleException = new UnauthorizedApprovalSettingReviewerRoleException(
                message: "The current user is not allowed to administer approval setting reviewer roles.");

            var expectedApprovalSettingReviewerRoleValidationException = new ApprovalSettingReviewerRoleValidationException(
                message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalSettingReviewerRoleException);

            // when
            ValueTask<ApprovalSettingReviewerRole> addApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.AddApprovalSettingReviewerRoleAsync(
                    someApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    addApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
