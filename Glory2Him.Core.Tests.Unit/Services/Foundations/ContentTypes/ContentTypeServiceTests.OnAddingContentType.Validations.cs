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
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Foundations.ContentTypes.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentTypes
{
    public partial class ContentTypeServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingContentTypeEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<ContentType>? nullEnvelope = null;

            var invalidContentTypeEventException =
                new InvalidContentTypeEventException(
                    message: "Invalid content type event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedContentTypeValidationException =
                new ContentTypeValidationException(
                    message: "Content type validation error occurred, fix the errors and try again.",
                    innerException: invalidContentTypeEventException);

            // when
            ValueTask<EventEnvelope<ContentType>?> onAddingTask =
                this.contentTypeService.OnAddingContentTypeAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    onAddingTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingContentTypeEventWhenMetadataIsNullAsync()
        {
            // given
            var invalidEnvelope = new EventEnvelope<ContentType>
            {
                Content = new ContentType { Id = Guid.NewGuid() },
                Metadata = null!
            };

            var invalidContentTypeEventException =
                new InvalidContentTypeEventException(
                    message: "Invalid content type event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedContentTypeValidationException =
                new ContentTypeValidationException(
                    message: "Content type validation error occurred, fix the errors and try again.",
                    innerException: invalidContentTypeEventException);

            // when
            ValueTask<EventEnvelope<ContentType>?> onAddingTask =
                this.contentTypeService.OnAddingContentTypeAsync(
                    invalidEnvelope,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    onAddingTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingContentTypeEventWhenContentIsNullAsync()
        {
            // given
            var invalidEnvelope = new EventEnvelope<ContentType>
            {
                Content = null!,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var invalidContentTypeEventException =
                new InvalidContentTypeEventException(
                    message: "Invalid content type event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedContentTypeValidationException =
                new ContentTypeValidationException(
                    message: "Content type validation error occurred, fix the errors and try again.",
                    innerException: invalidContentTypeEventException);

            // when
            ValueTask<EventEnvelope<ContentType>?> onAddingTask =
                this.contentTypeService.OnAddingContentTypeAsync(
                    invalidEnvelope,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    onAddingTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
