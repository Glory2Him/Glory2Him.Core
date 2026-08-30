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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    public partial class ApprovalSettingServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalSettingIsNullAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            ApprovalSetting nullApprovalSetting = null;

            var nullApprovalSettingException =
                new NullApprovalSettingException(message: "Approval setting is null.");

            var expectedApprovalSettingValidationException =
                new ApprovalSettingValidationException(
                    message: "Approval setting validation error occurred, fix the errors and try again.",
                    innerException: nullApprovalSettingException);

            // when
            ValueTask<ApprovalSetting> modifyApprovalSettingTask =
                this.approvalSettingService.ModifyApprovalSettingAsync(
                    nullApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    modifyApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalSettingIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidApprovalSetting = new ApprovalSetting
            {
                Id = Guid.Empty,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidApprovalSettingException =
                new InvalidApprovalSettingException(
                    message: "Approval setting is invalid, fix the errors and try again.");

            invalidApprovalSettingException.AddData(
                key: nameof(ApprovalSetting.Id),
                values: "Id is required");

            invalidApprovalSettingException.AddData(
                key: nameof(ApprovalSetting.CreatedBy),
                values: "Text is required");

            invalidApprovalSettingException.AddData(
                key: nameof(ApprovalSetting.UpdatedBy),
                values: "Text is required");

            invalidApprovalSettingException.AddData(
                key: nameof(ApprovalSetting.CreatedWhen),
                values: "Date is required");

            invalidApprovalSettingException.AddData(
                key: nameof(ApprovalSetting.UpdatedWhen),
                values: new[]
                {
                    "Date is required",
                    "Date is the same as CreatedWhen",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            var expectedApprovalSettingValidationException =
                new ApprovalSettingValidationException(
                    message: "Approval setting validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSetting> modifyApprovalSettingTask =
                this.approvalSettingService.ModifyApprovalSettingAsync(
                    invalidApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    modifyApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalSettingNotFoundAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ApprovalSetting randomApprovalSetting =
                CreateRandomModifyApprovalSetting(randomDateTimeOffset, randomUserId);
            ApprovalSetting nonExistentApprovalSetting = randomApprovalSetting;
            ApprovalSetting noApprovalSetting = null;

            var notFoundApprovalSettingException = new NotFoundApprovalSettingException(
                message: $"Approval setting not found with id: {nonExistentApprovalSetting.Id}.");

            var expectedApprovalSettingValidationException = new ApprovalSettingValidationException(
                message: "Approval setting validation error occurred, fix the errors and try again.",
                innerException: notFoundApprovalSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(nonExistentApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    nonExistentApprovalSetting.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noApprovalSetting);

            // when
            ValueTask<ApprovalSetting> modifyApprovalSettingTask =
                this.approvalSettingService.ModifyApprovalSettingAsync(
                    nonExistentApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    modifyApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    nonExistentApprovalSetting.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ApprovalSetting randomApprovalSetting =
                CreateRandomModifyApprovalSetting(randomDateTimeOffset, randomUserId);
            ApprovalSetting invalidApprovalSetting = randomApprovalSetting;
            ApprovalSetting storageApprovalSetting = randomApprovalSetting.DeepClone();
            storageApprovalSetting.CreatedWhen = GetRandomDateTimeOffset();
            storageApprovalSetting.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidApprovalSettingException = new InvalidApprovalSettingException(
                message: "Approval setting is invalid, fix the errors and try again.");

            invalidApprovalSettingException.AddData(
                key: nameof(ApprovalSetting.CreatedWhen),
                values: $"Date is not the same as {nameof(ApprovalSetting.CreatedWhen)}");

            var expectedApprovalSettingValidationException = new ApprovalSettingValidationException(
                message: "Approval setting validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    invalidApprovalSetting.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalSetting,
                    storageApprovalSetting))
                        .ReturnsAsync(invalidApprovalSetting);

            // when
            ValueTask<ApprovalSetting> modifyApprovalSettingTask =
                this.approvalSettingService.ModifyApprovalSettingAsync(
                    invalidApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    modifyApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    invalidApprovalSetting.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalSetting,
                    storageApprovalSetting),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ApprovalSetting randomApprovalSetting =
                CreateRandomModifyApprovalSetting(randomDateTimeOffset, randomUserId);
            ApprovalSetting invalidApprovalSetting = randomApprovalSetting;
            ApprovalSetting storageApprovalSetting = randomApprovalSetting.DeepClone();
            storageApprovalSetting.CreatedBy = GetRandomString();
            storageApprovalSetting.UpdatedWhen = storageApprovalSetting.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidApprovalSettingException =
                new InvalidApprovalSettingException(
                    message: "Approval setting is invalid, fix the errors and try again.");

            invalidApprovalSettingException.AddData(
                key: nameof(ApprovalSetting.CreatedBy),
                values: $"Text is not the same as {nameof(ApprovalSetting.CreatedBy)}");

            var expectedApprovalSettingValidationException =
                new ApprovalSettingValidationException(
                    message: "Approval setting validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    invalidApprovalSetting.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalSetting,
                    storageApprovalSetting))
                        .ReturnsAsync(invalidApprovalSetting);

            // when
            ValueTask<ApprovalSetting> modifyApprovalSettingTask =
                this.approvalSettingService.ModifyApprovalSettingAsync(
                    invalidApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    modifyApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    invalidApprovalSetting.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalSetting,
                    storageApprovalSetting),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ApprovalSetting randomApprovalSetting =
                CreateRandomModifyApprovalSetting(randomDateTimeOffset, randomUserId);
            ApprovalSetting invalidApprovalSetting = randomApprovalSetting;
            ApprovalSetting storageApprovalSetting = randomApprovalSetting.DeepClone();

            var invalidApprovalSettingException =
                new InvalidApprovalSettingException(
                    message: "Approval setting is invalid, fix the errors and try again.");

            invalidApprovalSettingException.AddData(
                key: nameof(ApprovalSetting.UpdatedWhen),
                values: $"Date is the same as {nameof(ApprovalSetting.UpdatedWhen)}");

            var expectedApprovalSettingValidationException =
                new ApprovalSettingValidationException(
                    message: "Approval setting validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    invalidApprovalSetting.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalSetting,
                    storageApprovalSetting))
                        .ReturnsAsync(invalidApprovalSetting);

            // when
            ValueTask<ApprovalSetting> modifyApprovalSettingTask =
                this.approvalSettingService.ModifyApprovalSettingAsync(
                    invalidApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    modifyApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    invalidApprovalSetting.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalSetting,
                    storageApprovalSetting),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            string randomUserId = GetRandomString();
            string differentUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSetting randomApprovalSetting =
                CreateRandomModifyApprovalSetting(randomDateTimeOffset, randomUserId);
            ApprovalSetting invalidApprovalSetting = randomApprovalSetting;
            invalidApprovalSetting.UpdatedBy = differentUserId;

            var invalidApprovalSettingException =
                new InvalidApprovalSettingException(
                    message: "Approval setting is invalid, fix the errors and try again.");

            invalidApprovalSettingException.AddData(
                key: nameof(ApprovalSetting.UpdatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedApprovalSettingValidationException =
                new ApprovalSettingValidationException(
                    message: "Approval setting validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSetting> modifyApprovalSettingTask =
                this.approvalSettingService.ModifyApprovalSettingAsync(
                    invalidApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    modifyApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSetting randomApprovalSetting =
                CreateRandomModifyApprovalSetting(randomDateTimeOffset, randomUserId);
            ApprovalSetting invalidApprovalSetting = randomApprovalSetting;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidApprovalSetting.UpdatedWhen = invalidApprovalSetting.CreatedWhen;

            var invalidApprovalSettingException =
                new InvalidApprovalSettingException(
                    message: "Approval setting is invalid, fix the errors and try again.");

            invalidApprovalSettingException.AddData(
                key: nameof(ApprovalSetting.UpdatedWhen),
                values: new[]
                {
                    $"Date is the same as {nameof(ApprovalSetting.CreatedWhen)}",
                    $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                        $"but found {invalidApprovalSetting.UpdatedWhen}"
                });

            var expectedApprovalSettingValidationException =
                new ApprovalSettingValidationException(
                    message: "Approval setting validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSetting> modifyApprovalSettingTask =
                this.approvalSettingService.ModifyApprovalSettingAsync(
                    invalidApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    modifyApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSetting randomApprovalSetting =
                CreateRandomModifyApprovalSetting(randomDateTimeOffset, randomUserId);
            ApprovalSetting invalidApprovalSetting = randomApprovalSetting;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidApprovalSetting.UpdatedWhen = randomDateTimeOffset.AddMinutes(minutes);

            var invalidApprovalSettingException =
                new InvalidApprovalSettingException(
                    message: "Approval setting is invalid, fix the errors and try again.");

            invalidApprovalSettingException.AddData(
                key: nameof(ApprovalSetting.UpdatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidApprovalSetting.UpdatedWhen}");

            var expectedApprovalSettingValidationException =
                new ApprovalSettingValidationException(
                    message: "Approval setting validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<ApprovalSetting> modifyApprovalSettingTask =
                this.approvalSettingService.ModifyApprovalSettingAsync(
                    invalidApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    modifyApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalSettingExceedsMaxLengthAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSetting invalidApprovalSetting =
                CreateRandomModifyApprovalSetting(randomDateTimeOffset, randomUserId);

            var invalidApprovalSettingException =
                new InvalidApprovalSettingException(
                    message: "Approval setting is invalid, fix the errors and try again.");

            invalidApprovalSettingException.AddData(
                key: nameof(ApprovalSetting.CreatedBy),
                values: $"Text exceed max length of {invalidApprovalSetting.CreatedBy.Length - 1} characters");

            invalidApprovalSettingException.AddData(
                key: nameof(ApprovalSetting.UpdatedBy),
                values: $"Text exceed max length of {invalidApprovalSetting.UpdatedBy.Length - 1} characters");

            var expectedApprovalSettingValidationException =
                new ApprovalSettingValidationException(
                    message: "Approval setting validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSetting> modifyApprovalSettingTask =
                this.approvalSettingService.ModifyApprovalSettingAsync(
                    invalidApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    modifyApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
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
            ApprovalSetting someApprovalSetting = CreateRandomApprovalSetting();

            var unauthorizedApprovalSettingException = new UnauthorizedApprovalSettingException(
                message: "The current user is not authenticated.");

            var expectedApprovalSettingValidationException = new ApprovalSettingValidationException(
                message: "Approval setting validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalSettingException);

            // when
            ValueTask<ApprovalSetting> modifyApprovalSettingTask =
                this.approvalSettingService.ModifyApprovalSettingAsync(
                    someApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    modifyApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(NonAdminRoleSets))]
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsNotAdminAndLogItAsync(
            string[] nonAdminRoles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonAdminRoles);
            ApprovalSetting someApprovalSetting = CreateRandomApprovalSetting();

            var unauthorizedApprovalSettingException = new UnauthorizedApprovalSettingException(
                message: "The current user is not allowed to administer approval settings.");

            var expectedApprovalSettingValidationException = new ApprovalSettingValidationException(
                message: "Approval setting validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalSettingException);

            // when
            ValueTask<ApprovalSetting> modifyApprovalSettingTask =
                this.approvalSettingService.ModifyApprovalSettingAsync(
                    someApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    modifyApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}