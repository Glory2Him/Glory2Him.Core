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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyingContentItemEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<ContentItem>? nullEnvelope = null;

            var invalidContentItemEventException =
                new InvalidContentItemEventException(
                    message: "Invalid content item event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemEventException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onModifyingTask =
                this.contentItemService.OnModifyingContentItemAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    onModifyingTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyingContentItemEventWhenContentItemNotFoundAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentItem inputContentItem = CreateRandomModifyContentItem(randomDateTimeOffset, randomUserId);
            ContentItem noContentItem = null!;

            var requestEnvelope = new EventEnvelope<ContentItem>
            {
                Content = inputContentItem,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var notFoundContentItemException = new NotFoundContentItemException(
                message: $"Content item not found with id: {inputContentItem.Id}.");

            var expectedContentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: notFoundContentItemException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemOnModifyingContentItemSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputContentItem, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputContentItem);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    inputContentItem.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noContentItem);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onModifyingTask =
                this.contentItemService.OnModifyingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    onModifyingTask.AsTask);

            // then: the raw not-found from the shared do-work is categorized the same way
            // the non-event path categorizes it — the event path must not degrade it to a
            // service exception.
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(
                    inputContentItem.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
