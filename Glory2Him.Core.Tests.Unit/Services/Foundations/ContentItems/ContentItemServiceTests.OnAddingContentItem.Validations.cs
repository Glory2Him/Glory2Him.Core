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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingContentItemEventWhenEnvelopeIsInvalidAsync()
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
            ValueTask<EventEnvelope<ContentItem>?> onAddingTask =
                this.contentItemService.OnAddingContentItemAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    onAddingTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnAddingContentItemEventWhenMetadataIsNullAsync()
        {
            // given
            var invalidEnvelope = new EventEnvelope<ContentItem>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new ContentItem { Id = Guid.NewGuid() },
                Metadata = null!
            };

            var invalidContentItemEventException =
                new InvalidContentItemEventException(
                    message: "Invalid content item event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemEventException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onAddingTask =
                this.contentItemService.OnAddingContentItemAsync(
                    invalidEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    onAddingTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnAddingContentItemEventWhenContentIsNullAsync()
        {
            // given
            var invalidEnvelope = new EventEnvelope<ContentItem>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = null!,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var invalidContentItemEventException =
                new InvalidContentItemEventException(
                    message: "Invalid content item event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemEventException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onAddingTask =
                this.contentItemService.OnAddingContentItemAsync(
                    invalidEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    onAddingTask.AsTask);

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
    }
}
