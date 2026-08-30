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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.ContentItemSettings.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemSettings
{
    public partial class ContentItemSettingServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemSettingIsNullAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            ContentItemSetting nullContentItemSetting = null;

            var nullContentItemSettingException =
                new NullContentItemSettingException(message: "Content item setting is null.");

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: nullContentItemSettingException);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    nullContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemSettingIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidContentItemSetting = new ContentItemSetting
            {
                Id = Guid.Empty,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidContentItemSettingException =
                new InvalidContentItemSettingException(
                    message: "Content item setting is invalid, fix the errors and try again.");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.Id),
                values: "Id is required");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.CreatedBy),
                values: "Text is required");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.UpdatedBy),
                values: "Text is required");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.CreatedWhen),
                values: "Date is required");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.UpdatedWhen),
                values: new[]
                {
                    "Date is required",
                    "Date is the same as CreatedWhen",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    invalidContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemSettingNotFoundAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ContentItemSetting randomContentItemSetting =
                CreateRandomModifyContentItemSetting(randomDateTimeOffset, randomUserId);
            ContentItemSetting nonExistentContentItemSetting = randomContentItemSetting;
            ContentItemSetting noContentItemSetting = null;

            var notFoundContentItemSettingException = new NotFoundContentItemSettingException(
                message: $"Content item setting not found with id: {nonExistentContentItemSetting.Id}.");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: notFoundContentItemSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(nonExistentContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    nonExistentContentItemSetting.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noContentItemSetting);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    nonExistentContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    nonExistentContentItemSetting.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentTypeIsInvalidAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ContentItemSetting randomContentItemSetting =
                CreateRandomModifyContentItemSetting(randomDateTimeOffset, randomUserId);
            ContentItemSetting invalidContentItemSetting = randomContentItemSetting;
            invalidContentItemSetting.ContentType = (ContentType)int.MaxValue;

            var invalidContentItemSettingException =
                new InvalidContentItemSettingException(
                    message: "Content item setting is invalid, fix the errors and try again.");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.ContentType),
                values: "Value is not a supported content type");

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    invalidContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
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
            ContentItemSetting randomContentItemSetting =
                CreateRandomModifyContentItemSetting(randomDateTimeOffset, randomUserId);
            ContentItemSetting invalidContentItemSetting = randomContentItemSetting;
            ContentItemSetting storageContentItemSetting = randomContentItemSetting.DeepClone();
            storageContentItemSetting.CreatedWhen = GetRandomDateTimeOffset();
            storageContentItemSetting.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidContentItemSettingException = new InvalidContentItemSettingException(
                message: "Content item setting is invalid, fix the errors and try again.");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.CreatedWhen),
                values: $"Date is not the same as {nameof(ContentItemSetting.CreatedWhen)}");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: invalidContentItemSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    invalidContentItemSetting.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItemSetting,
                    storageContentItemSetting))
                        .ReturnsAsync(invalidContentItemSetting);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    invalidContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    invalidContentItemSetting.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItemSetting,
                    storageContentItemSetting),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
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
            ContentItemSetting randomContentItemSetting =
                CreateRandomModifyContentItemSetting(randomDateTimeOffset, randomUserId);
            ContentItemSetting invalidContentItemSetting = randomContentItemSetting;
            ContentItemSetting storageContentItemSetting = randomContentItemSetting.DeepClone();
            storageContentItemSetting.CreatedBy = GetRandomString();
            storageContentItemSetting.UpdatedWhen =
                storageContentItemSetting.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidContentItemSettingException =
                new InvalidContentItemSettingException(
                    message: "Content item setting is invalid, fix the errors and try again.");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.CreatedBy),
                values: $"Text is not the same as {nameof(ContentItemSetting.CreatedBy)}");

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    invalidContentItemSetting.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItemSetting,
                    storageContentItemSetting))
                        .ReturnsAsync(invalidContentItemSetting);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    invalidContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    invalidContentItemSetting.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItemSetting,
                    storageContentItemSetting),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
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
            ContentItemSetting randomContentItemSetting =
                CreateRandomModifyContentItemSetting(randomDateTimeOffset, randomUserId);
            ContentItemSetting invalidContentItemSetting = randomContentItemSetting;
            ContentItemSetting storageContentItemSetting = randomContentItemSetting.DeepClone();

            var invalidContentItemSettingException =
                new InvalidContentItemSettingException(
                    message: "Content item setting is invalid, fix the errors and try again.");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.UpdatedWhen),
                values: $"Date is the same as {nameof(ContentItemSetting.UpdatedWhen)}");

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    invalidContentItemSetting.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItemSetting,
                    storageContentItemSetting))
                        .ReturnsAsync(invalidContentItemSetting);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    invalidContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    invalidContentItemSetting.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItemSetting,
                    storageContentItemSetting),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
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
            ContentItemSetting randomContentItemSetting =
                CreateRandomModifyContentItemSetting(randomDateTimeOffset, randomUserId);
            ContentItemSetting invalidContentItemSetting = randomContentItemSetting;
            invalidContentItemSetting.UpdatedBy = differentUserId;

            var invalidContentItemSettingException =
                new InvalidContentItemSettingException(
                    message: "Content item setting is invalid, fix the errors and try again.");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.UpdatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    invalidContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
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
            ContentItemSetting randomContentItemSetting =
                CreateRandomModifyContentItemSetting(randomDateTimeOffset, randomUserId);
            ContentItemSetting invalidContentItemSetting = randomContentItemSetting;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidContentItemSetting.UpdatedWhen = invalidContentItemSetting.CreatedWhen;

            var invalidContentItemSettingException =
                new InvalidContentItemSettingException(
                    message: "Content item setting is invalid, fix the errors and try again.");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.UpdatedWhen),
                values: new[]
                {
                    $"Date is the same as {nameof(ContentItemSetting.CreatedWhen)}",
                    $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                        $"but found {invalidContentItemSetting.UpdatedWhen}"
                });

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    invalidContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
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
            ContentItemSetting randomContentItemSetting =
                CreateRandomModifyContentItemSetting(randomDateTimeOffset, randomUserId);
            ContentItemSetting invalidContentItemSetting = randomContentItemSetting;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidContentItemSetting.UpdatedWhen = randomDateTimeOffset.AddMinutes(minutes);

            var invalidContentItemSettingException =
                new InvalidContentItemSettingException(
                    message: "Content item setting is invalid, fix the errors and try again.");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.UpdatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidContentItemSetting.UpdatedWhen}");

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    invalidContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemSettingExceedsMaxLengthAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentItemSetting invalidContentItemSetting =
                CreateRandomModifyContentItemSetting(randomDateTimeOffset, randomUserId);

            var invalidContentItemSettingException =
                new InvalidContentItemSettingException(
                    message: "Content item setting is invalid, fix the errors and try again.");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.CreatedBy),
                values: $"Text exceed max length of {invalidContentItemSetting.CreatedBy.Length - 1} characters");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.UpdatedBy),
                values: $"Text exceed max length of {invalidContentItemSetting.UpdatedBy.Length - 1} characters");

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    invalidContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentTypeDescriptionExceedsMaxLengthAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string invalidContentTypeDescription = GetRandomStringWithLengthOf(501);

            ContentItemSetting randomContentItemSetting =
                CreateRandomModifyContentItemSetting(randomDateTimeOffset, randomUserId);
            ContentItemSetting invalidContentItemSetting = randomContentItemSetting;
            invalidContentItemSetting.ContentTypeDescription = invalidContentTypeDescription;

            var invalidContentItemSettingException =
                new InvalidContentItemSettingException(
                    message: "Content item setting is invalid, fix the errors and try again.");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.ContentTypeDescription),
                values: $"Text exceed max length of {invalidContentTypeDescription.Length - 1} characters");

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    invalidContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentTypeNameExceedsMaxLengthAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string invalidContentTypeName = GetRandomStringWithLengthOf(51);

            ContentItemSetting randomContentItemSetting =
                CreateRandomModifyContentItemSetting(randomDateTimeOffset, randomUserId);
            ContentItemSetting invalidContentItemSetting = randomContentItemSetting;
            invalidContentItemSetting.ContentTypeName = invalidContentTypeName;

            var invalidContentItemSettingException =
                new InvalidContentItemSettingException(
                    message: "Content item setting is invalid, fix the errors and try again.");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.ContentTypeName),
                values: $"Text exceed max length of {invalidContentTypeName.Length - 1} characters");

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    invalidContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfSortOrderIsNegativeAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            int negativeSortOrder = GetRandomNegativeNumber();

            ContentItemSetting randomContentItemSetting =
                CreateRandomModifyContentItemSetting(randomDateTimeOffset, randomUserId);
            ContentItemSetting invalidContentItemSetting = randomContentItemSetting;
            invalidContentItemSetting.SortOrder = negativeSortOrder;

            var invalidContentItemSettingException =
                new InvalidContentItemSettingException(
                    message: "Content item setting is invalid, fix the errors and try again.");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.SortOrder),
                values: "Value is less than the minimum of 0");

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    invalidContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
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
            ContentItemSetting someContentItemSetting = CreateRandomContentItemSetting();
            var unauthorizedContentItemSettingException = new UnauthorizedContentItemSettingException(
                message: "The current user is not authenticated.");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: unauthorizedContentItemSettingException);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    someContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
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
            ContentItemSetting someContentItemSetting = CreateRandomContentItemSetting();
            var unauthorizedContentItemSettingException = new UnauthorizedContentItemSettingException(
                message: "The current user is not allowed to administer content item settings.");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: unauthorizedContentItemSettingException);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    someContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}