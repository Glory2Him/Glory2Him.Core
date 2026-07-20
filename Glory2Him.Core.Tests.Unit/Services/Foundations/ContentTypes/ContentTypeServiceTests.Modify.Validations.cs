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
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Foundations.ContentTypes.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentTypes
{
    public partial class ContentTypeServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentTypeIsNullAndLogItAsync()
        {
            // given
            ContentType nullContentType = null;

            var nullContentTypeException =
                new NullContentTypeException(message: "Content type is null.");

            var expectedContentTypeValidationException =
                new ContentTypeValidationException(
                    message: "Content type validation error occurred, fix the errors and try again.",
                    innerException: nullContentTypeException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(nullContentType, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(nullContentType);

            // when
            ValueTask<ContentType> modifyContentTypeTask =
                this.contentTypeService.ModifyContentTypeAsync(
                    nullContentType,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    modifyContentTypeTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(nullContentType, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfContentTypeIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            var invalidContentType = new ContentType
            {
                Id = Guid.Empty,
                Name = invalidText,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidContentTypeException =
                new InvalidContentTypeException(
                    message: "Content type is invalid, fix the errors and try again.");

            invalidContentTypeException.AddData(
                key: nameof(ContentType.Id),
                values: "Id is required");

            invalidContentTypeException.AddData(
                key: nameof(ContentType.Name),
                values: "Text is required");

            invalidContentTypeException.AddData(
                key: nameof(ContentType.CreatedBy),
                values: "Text is required");

            invalidContentTypeException.AddData(
                key: nameof(ContentType.UpdatedBy),
                values: "Text is required");

            invalidContentTypeException.AddData(
                key: nameof(ContentType.CreatedWhen),
                values: "Date is required");

            invalidContentTypeException.AddData(
                key: nameof(ContentType.UpdatedWhen),
                values: new[] { "Date is required", "Date is the same as CreatedWhen" });

            var expectedContentTypeValidationException =
                new ContentTypeValidationException(
                    message: "Content type validation error occurred, fix the errors and try again.",
                    innerException: invalidContentTypeException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentType, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentType);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(default(DateTimeOffset));

            // when
            ValueTask<ContentType> modifyContentTypeTask =
                this.contentTypeService.ModifyContentTypeAsync(
                    invalidContentType,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    modifyContentTypeTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentType, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentTypeNotFoundAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ContentType randomContentType = CreateRandomModifyContentType(randomDateTimeOffset, randomUserId);
            ContentType nonExistentContentType = randomContentType;
            ContentType noContentType = null;

            var notFoundContentTypeException = new NotFoundContentTypeException(
                message: $"Content type not found with id: {nonExistentContentType.Id}.");

            var expectedContentTypeValidationException = new ContentTypeValidationException(
                message: "Content type validation error occurred, fix the errors and try again.",
                innerException: notFoundContentTypeException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentContentType, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(nonExistentContentType);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    nonExistentContentType.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noContentType);

            // when
            ValueTask<ContentType> modifyContentTypeTask =
                this.contentTypeService.ModifyContentTypeAsync(
                    nonExistentContentType,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    modifyContentTypeTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentContentType, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    nonExistentContentType.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
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
            ContentType randomContentType = CreateRandomModifyContentType(randomDateTimeOffset, randomUserId);
            ContentType invalidContentType = randomContentType;
            ContentType storageContentType = randomContentType.DeepClone();
            storageContentType.CreatedWhen = GetRandomDateTimeOffset();
            storageContentType.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidContentTypeException = new InvalidContentTypeException(
                message: "Content type is invalid, fix the errors and try again.");

            invalidContentTypeException.AddData(
                key: nameof(ContentType.CreatedWhen),
                values: $"Date is not the same as {nameof(ContentType.CreatedWhen)}");

            var expectedContentTypeValidationException = new ContentTypeValidationException(
                message: "Content type validation error occurred, fix the errors and try again.",
                innerException: invalidContentTypeException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentType, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentType);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    invalidContentType.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentType);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentType,
                    storageContentType))
                        .ReturnsAsync(invalidContentType);

            // when
            ValueTask<ContentType> modifyContentTypeTask =
                this.contentTypeService.ModifyContentTypeAsync(
                    invalidContentType,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    modifyContentTypeTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentType, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    invalidContentType.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentType,
                    storageContentType),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
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
            ContentType randomContentType = CreateRandomModifyContentType(randomDateTimeOffset, randomUserId);
            ContentType invalidContentType = randomContentType;
            ContentType storageContentType = randomContentType.DeepClone();
            storageContentType.CreatedBy = GetRandomString();
            storageContentType.UpdatedWhen = storageContentType.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidContentTypeException =
                new InvalidContentTypeException(
                    message: "Content type is invalid, fix the errors and try again.");

            invalidContentTypeException.AddData(
                key: nameof(ContentType.CreatedBy),
                values: $"Text is not the same as {nameof(ContentType.CreatedBy)}");

            var expectedContentTypeValidationException =
                new ContentTypeValidationException(
                    message: "Content type validation error occurred, fix the errors and try again.",
                    innerException: invalidContentTypeException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentType, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentType);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    invalidContentType.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentType);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentType,
                    storageContentType))
                        .ReturnsAsync(invalidContentType);

            // when
            ValueTask<ContentType> modifyContentTypeTask =
                this.contentTypeService.ModifyContentTypeAsync(
                    invalidContentType,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    modifyContentTypeTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentType, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    invalidContentType.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentType,
                    storageContentType),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
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
            ContentType randomContentType = CreateRandomModifyContentType(randomDateTimeOffset, randomUserId);
            ContentType invalidContentType = randomContentType;
            ContentType storageContentType = randomContentType.DeepClone();

            var invalidContentTypeException =
                new InvalidContentTypeException(
                    message: "Content type is invalid, fix the errors and try again.");

            invalidContentTypeException.AddData(
                key: nameof(ContentType.UpdatedWhen),
                values: $"Date is the same as {nameof(ContentType.UpdatedWhen)}");

            var expectedContentTypeValidationException =
                new ContentTypeValidationException(
                    message: "Content type validation error occurred, fix the errors and try again.",
                    innerException: invalidContentTypeException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentType, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentType);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    invalidContentType.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentType);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentType,
                    storageContentType))
                        .ReturnsAsync(invalidContentType);

            // when
            ValueTask<ContentType> modifyContentTypeTask =
                this.contentTypeService.ModifyContentTypeAsync(
                    invalidContentType,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    modifyContentTypeTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentType, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    invalidContentType.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentType,
                    storageContentType),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
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
            ContentType randomContentType = CreateRandomModifyContentType(randomDateTimeOffset, randomUserId);
            ContentType invalidContentType = randomContentType;
            invalidContentType.UpdatedBy = differentUserId;

            var invalidContentTypeException =
                new InvalidContentTypeException(
                    message: "Content type is invalid, fix the errors and try again.");

            invalidContentTypeException.AddData(
                key: nameof(ContentType.UpdatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedContentTypeValidationException =
                new ContentTypeValidationException(
                    message: "Content type validation error occurred, fix the errors and try again.",
                    innerException: invalidContentTypeException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentType, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentType);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentType> modifyContentTypeTask =
                this.contentTypeService.ModifyContentTypeAsync(
                    invalidContentType,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    modifyContentTypeTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentType, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
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
            ContentType randomContentType = CreateRandomModifyContentType(randomDateTimeOffset, randomUserId);
            ContentType invalidContentType = randomContentType;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidContentType.UpdatedWhen = invalidContentType.CreatedWhen;

            var invalidContentTypeException =
                new InvalidContentTypeException(
                    message: "Content type is invalid, fix the errors and try again.");

            invalidContentTypeException.AddData(
                key: nameof(ContentType.UpdatedWhen),
                values: new[]
                {
                    $"Date is the same as {nameof(ContentType.CreatedWhen)}",
                    $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                        $"but found {invalidContentType.UpdatedWhen}"
                });

            var expectedContentTypeValidationException =
                new ContentTypeValidationException(
                    message: "Content type validation error occurred, fix the errors and try again.",
                    innerException: invalidContentTypeException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentType, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentType);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentType> modifyContentTypeTask =
                this.contentTypeService.ModifyContentTypeAsync(
                    invalidContentType,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    modifyContentTypeTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentType, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
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
            ContentType randomContentType = CreateRandomModifyContentType(randomDateTimeOffset, randomUserId);
            ContentType invalidContentType = randomContentType;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidContentType.UpdatedWhen = randomDateTimeOffset.AddMinutes(minutes);

            var invalidContentTypeException =
                new InvalidContentTypeException(
                    message: "Content type is invalid, fix the errors and try again.");

            invalidContentTypeException.AddData(
                key: nameof(ContentType.UpdatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidContentType.UpdatedWhen}");

            var expectedContentTypeValidationException =
                new ContentTypeValidationException(
                    message: "Content type validation error occurred, fix the errors and try again.",
                    innerException: invalidContentTypeException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentType, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentType);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<ContentType> modifyContentTypeTask =
                this.contentTypeService.ModifyContentTypeAsync(
                    invalidContentType,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    modifyContentTypeTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentType, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentTypeExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentType invalidContentType = CreateRandomModifyContentType(randomDateTimeOffset, randomUserId);

            var invalidContentTypeException =
                new InvalidContentTypeException(
                    message: "Content type is invalid, fix the errors and try again.");

            invalidContentTypeException.AddData(
                key: nameof(ContentType.CreatedBy),
                values: $"Text exceed max length of {invalidContentType.CreatedBy.Length - 1} characters");

            invalidContentTypeException.AddData(
                key: nameof(ContentType.UpdatedBy),
                values: $"Text exceed max length of {invalidContentType.UpdatedBy.Length - 1} characters");

            var expectedContentTypeValidationException =
                new ContentTypeValidationException(
                    message: "Content type validation error occurred, fix the errors and try again.",
                    innerException: invalidContentTypeException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentType, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentType);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentType> modifyContentTypeTask =
                this.contentTypeService.ModifyContentTypeAsync(
                    invalidContentType,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    modifyContentTypeTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentType, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}