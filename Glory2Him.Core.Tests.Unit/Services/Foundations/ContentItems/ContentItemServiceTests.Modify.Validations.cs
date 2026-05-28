// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
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

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(nullContentItem))
                    .ReturnsAsync(nullContentItem);

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

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(nullContentItem),
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
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            var invalidContentItem = new ContentItem
            {
                Id = Guid.Empty,
                ContentTypeId = Guid.Empty,
                ContentItemGroupId = Guid.Empty,
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
                key: nameof(ContentItem.ContentTypeId),
                values: "Id is required");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.ContentItemGroupId),
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
                values: "Date is required");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem))
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
                broker.ApplyModifyAuditValuesAsync(invalidContentItem),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemNotFoundAndLogItAsync()        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentItem randomContentItem = CreateRandomModifyContentItem(randomDateTimeOffset);
            ContentItem nonExistentContentItem = randomContentItem;
            ContentItem noContentItem = null;

            var notFoundContentItemException = new NotFoundContentItemException(
                message: $"Content item not found with id: {nonExistentContentItem.Id}.");

            var expectedContentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: notFoundContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentContentItem))
                    .ReturnsAsync(nonExistentContentItem);

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
                broker.ApplyModifyAuditValuesAsync(nonExistentContentItem),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageCreatedWhenNotSameAsInputAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentItem randomContentItem = CreateRandomModifyContentItem(randomDateTimeOffset);
            ContentItem invalidContentItem = randomContentItem;
            ContentItem storageContentItem = randomContentItem.DeepClone();
            storageContentItem.CreatedWhen = GetRandomDateTimeOffset();
            storageContentItem.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidContentItemException = new InvalidContentItemException(
                message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.CreatedWhen),
                values: $"Date is not the same as: {storageContentItem.CreatedWhen}");

            var expectedContentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: invalidContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItem))
                    .ReturnsAsync(invalidContentItem);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    invalidContentItem.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
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
                broker.ApplyModifyAuditValuesAsync(invalidContentItem),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(
                    invalidContentItem.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
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
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentItem randomContentItem = CreateRandomModifyContentItem(randomDateTimeOffset);
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
                broker.ApplyModifyAuditValuesAsync(invalidContentItem))
                    .ReturnsAsync(invalidContentItem);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    invalidContentItem.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
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
                broker.ApplyModifyAuditValuesAsync(invalidContentItem),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(
                    invalidContentItem.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
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
    }
}
