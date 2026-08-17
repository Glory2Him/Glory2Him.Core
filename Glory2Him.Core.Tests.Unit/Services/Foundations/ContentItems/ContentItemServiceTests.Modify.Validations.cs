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
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemIsNullAndLogItAsync()
        {
            // given
            ContentItem nullContentItem = null;

            var nullContentItemException =
                new NullContentItemException(message: "Content item is null.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: nullContentItemException);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    nullContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidContentItem = new ContentItem
            {
                Id = Guid.Empty,
                GroupId = Guid.Empty,
                Content = invalidText,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidContentItemException =
                new InvalidContentItemException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.Id),
                values: "Id is required");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.GroupId),
                values: "Id is required");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.Content),
                values: "Text is required");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.CreatedBy),
                values: "Text is required");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.UpdatedBy),
                values: "Text is required");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.CreatedWhen),
                values: "Date is required");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.UpdatedWhen),
                values: new[]
                {
                    "Date is required",
                    "Date is the same as CreatedWhen",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemNotFoundAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ContentItem randomContentItem = CreateRandomModifyContentItem(randomDateTimeOffset, randomUserId);
            ContentItem nonExistentContentItem = randomContentItem;
            ContentItem noContentItem = null;

            var notFoundContentItemException = new NotFoundContentItemException(
                message: $"Content item not found with id: {nonExistentContentItem.Id}.");

            var expectedContentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: notFoundContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(nonExistentContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    nonExistentContentItem.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noContentItem);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    nonExistentContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentContentItem, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(
                    nonExistentContentItem.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
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
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ContentItem randomContentItem = CreateRandomModifyContentItem(randomDateTimeOffset, randomUserId);
            ContentItem invalidContentItem = randomContentItem;
            invalidContentItem.ContentType = (ContentType)int.MaxValue;

            var invalidContentItemException = new InvalidContentItemException(
                message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.ContentType),
                values: "Value is not a supported content type");

            var expectedContentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: invalidContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
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
            ContentItem randomContentItem = CreateRandomModifyContentItem(randomDateTimeOffset, randomUserId);
            ContentItem invalidContentItem = randomContentItem;
            ContentItem storageContentItem = randomContentItem.DeepClone();
            storageContentItem.CreatedWhen = GetRandomDateTimeOffset();
            storageContentItem.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidContentItemException = new InvalidContentItemException(
                message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.CreatedWhen),
                values: $"Date is not the same as {nameof(ContentItem.CreatedWhen)}");

            var expectedContentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: invalidContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    invalidContentItem.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItem,
                    storageContentItem))
                        .ReturnsAsync(invalidContentItem);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(
                    invalidContentItem.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItem,
                    storageContentItem),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
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
            ContentItem randomContentItem = CreateRandomModifyContentItem(randomDateTimeOffset, randomUserId);
            ContentItem invalidContentItem = randomContentItem;
            ContentItem storageContentItem = randomContentItem.DeepClone();
            storageContentItem.CreatedBy = GetRandomString();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidContentItemException =
                new InvalidContentItemException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.CreatedBy),
                values: $"Text is not the same as {nameof(ContentItem.CreatedBy)}");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    invalidContentItem.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItem,
                    storageContentItem))
                        .ReturnsAsync(invalidContentItem);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(
                    invalidContentItem.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItem,
                    storageContentItem),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
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
            ContentItem randomContentItem = CreateRandomModifyContentItem(randomDateTimeOffset, randomUserId);
            ContentItem invalidContentItem = randomContentItem;
            ContentItem storageContentItem = randomContentItem.DeepClone();

            var invalidContentItemException =
                new InvalidContentItemException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.UpdatedWhen),
                values: $"Date is the same as {nameof(ContentItem.UpdatedWhen)}");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    invalidContentItem.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItem,
                    storageContentItem))
                        .ReturnsAsync(invalidContentItem);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(
                    invalidContentItem.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItem,
                    storageContentItem),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
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
            ContentItem randomContentItem = CreateRandomModifyContentItem(randomDateTimeOffset, randomUserId);
            ContentItem invalidContentItem = randomContentItem;
            invalidContentItem.UpdatedBy = differentUserId;

            var invalidContentItemException =
                new InvalidContentItemException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.UpdatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
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
            ContentItem randomContentItem = CreateRandomModifyContentItem(randomDateTimeOffset, randomUserId);
            ContentItem invalidContentItem = randomContentItem;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidContentItem.UpdatedWhen = invalidContentItem.CreatedWhen;

            var invalidContentItemException =
                new InvalidContentItemException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.UpdatedWhen),
                values: new[]
                {
                    $"Date is the same as {nameof(ContentItem.CreatedWhen)}",
                    $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                        $"but found {invalidContentItem.UpdatedWhen}"
                });

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
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
            ContentItem randomContentItem = CreateRandomModifyContentItem(randomDateTimeOffset, randomUserId);
            ContentItem invalidContentItem = randomContentItem;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidContentItem.UpdatedWhen = randomDateTimeOffset.AddMinutes(minutes);

            var invalidContentItemException =
                new InvalidContentItemException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.UpdatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidContentItem.UpdatedWhen}");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentItem invalidContentItem = CreateRandomModifyContentItem(randomDateTimeOffset, randomUserId);

            var invalidContentItemException =
                new InvalidContentItemException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.CreatedBy),
                values: $"Text exceed max length of {invalidContentItem.CreatedBy.Length - 1} characters");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.UpdatedBy),
                values: $"Text exceed max length of {invalidContentItem.UpdatedBy.Length - 1} characters");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
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
            ContentItem someContentItem = CreateRandomContentItem();

            var unauthorizedContentItemException = new UnauthorizedContentItemException(
                message: "The current user is not authenticated.");

            var expectedContentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: unauthorizedContentItemException);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    someContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.ContentItemReadOnly)]
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsBlockedFromContributingAndLogItAsync(
            string blockedRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(blockedRole);
            ContentItem someContentItem = CreateRandomContentItem();

            var unauthorizedContentItemException = new UnauthorizedContentItemException(
                message: "The current user is blocked from contributing content items.");

            var expectedContentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: unauthorizedContentItemException);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    someContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
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
            ContentItem randomContentItem = CreateRandomModifyContentItem(randomDateTimeOffset, randomUserId);
            ContentItem inputContentItem = randomContentItem;
            ContentItem storageContentItem = randomContentItem.DeepClone();
            storageContentItem.CreatedBy = GetRandomString();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var unauthorizedContentItemException = new UnauthorizedContentItemException(
                message: "The current user is not allowed to modify this content item.");

            var expectedContentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: unauthorizedContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    inputContentItem.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItem);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(inputContentItem, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(
                    inputContentItem.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
        [Theory]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageIsTerminalAndLogItAsync(
            ApprovalStatus terminalStatus)
        {
            // given: THE case the status pin never covered. The caller amends an approved row and
            // echoes the STORED status back unchanged, so IsNotAPermittedStatusChangeOnModify —
            // whose condition is guarded by inputStatus != storageStatus — passes, and the content
            // is written through with IsPublished and PublishDate still at their approved values.
            // The edit then goes public with no re-review.
            //
            // The owner is used here because it is the least privileged party who can reach this
            // path at all; the roles that could also reach it are covered below.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ContentItem randomContentItem = CreateRandomModifyContentItem(randomDateTimeOffset, randomUserId);
            ContentItem invalidContentItem = randomContentItem;
            ContentItem storageContentItem = randomContentItem.DeepClone();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            // both sides terminal and IDENTICAL, so nothing else in the modify can refuse it
            invalidContentItem.ApprovalStatus = terminalStatus;
            storageContentItem.ApprovalStatus = terminalStatus;

            var invalidContentItemException =
                new InvalidContentItemException(
                    message: "Content item cannot be modified from status " +
                        $"{terminalStatus}.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    invalidContentItem.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItem);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<ContentItemEventOperation>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
                Times.Once);
        }

        [Theory]
        [InlineData(Roles.Publisher)]
        [InlineData(Roles.Admin)]
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageIsTerminalForPrivilegedRolesAndLogItAsync(
            string role)
        {
            // given: terminal means terminal for EVERY role (§3.4 rules 7 and 16). An Admin in
            // particular used to have an in-place carve-out here; it is withdrawn, because a state
            // one role can edit out of is not terminal. The override verb is the only route, and
            // it changes status without touching content.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(role);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ContentItem randomContentItem = CreateRandomModifyContentItem(randomDateTimeOffset, randomUserId);
            ContentItem invalidContentItem = randomContentItem;
            ContentItem storageContentItem = randomContentItem.DeepClone();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            storageContentItem.CreatedBy = GetRandomString();

            invalidContentItem.ApprovalStatus = ApprovalStatus.Approved;
            storageContentItem.ApprovalStatus = ApprovalStatus.Approved;
            invalidContentItem.CreatedBy = storageContentItem.CreatedBy;

            var invalidContentItemException =
                new InvalidContentItemException(
                    message: "Content item cannot be modified from status " +
                        $"{ApprovalStatus.Approved}.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    invalidContentItem.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItem);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Submitted)]
        public async Task ShouldModifyIfStorageIsNotTerminalAsync(
            ApprovalStatus nonTerminalStatus)
        {
            // given: the other half of the rule, and the one a refusal written too broadly would
            // break — a Draft or Submitted row still modifies exactly as it did before.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ContentItem randomContentItem = CreateRandomModifyContentItem(randomDateTimeOffset, randomUserId);
            ContentItem inputContentItem = randomContentItem;
            ContentItem storageContentItem = randomContentItem.DeepClone();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            inputContentItem.ApprovalStatus = nonTerminalStatus;
            storageContentItem.ApprovalStatus = nonTerminalStatus;

            ContentItem updatedContentItem = inputContentItem.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    inputContentItem.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    inputContentItem,
                    storageContentItem))
                        .ReturnsAsync(inputContentItem);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAsync(
                    inputContentItem,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedContentItem);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    ContentItemEventOperation.Modified))
                        .Returns(new ValueTask<EventPublishResult<ContentItem>>(
                            new EventPublishResult<ContentItem>()));

            // when
            ContentItem actualContentItem = await this.contentItemService.ModifyContentItemAsync(
                inputContentItem,
                TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(updatedContentItem);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemAsync(
                    inputContentItem,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
