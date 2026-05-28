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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfContentItemIsNullAndLogItAsync()
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
                broker.ApplyAddAuditValuesAsync(nullContentItem))
                    .ReturnsAsync(nullContentItem);

            // when
            ValueTask<ContentItem> addContentItemTask =
                this.contentItemService.AddContentItemAsync(
                    nullContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    addContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(nullContentItem),
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
        public async Task ShouldThrowValidationExceptionOnAddIfContentItemIsInvalidAndLogItAsync(
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
                broker.ApplyAddAuditValuesAsync(invalidContentItem))
                    .ReturnsAsync(invalidContentItem);

            // when
            ValueTask<ContentItem> addContentItemTask =
                this.contentItemService.AddContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    addContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidContentItem),
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
