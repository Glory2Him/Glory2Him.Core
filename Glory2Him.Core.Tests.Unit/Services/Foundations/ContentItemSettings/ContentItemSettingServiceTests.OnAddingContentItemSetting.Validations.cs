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
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.ContentItemSettings.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemSettings
{
    public partial class ContentItemSettingServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingContentItemSettingEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<ContentItemSetting>? nullEnvelope = null;

            var invalidContentItemSettingEventException =
                new InvalidContentItemSettingEventException(
                    message: "Invalid content item setting event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemSettingEventException);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>?> onAddingTask =
                this.contentItemSettingService.OnAddingContentItemSettingAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    onAddingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingContentItemSettingEventWhenMetadataIsNullAsync()
        {
            // given
            var invalidEnvelope = new EventEnvelope<ContentItemSetting>
            {
                Content = new ContentItemSetting { Id = Guid.NewGuid() },
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin),
                Metadata = null!
            };

            var invalidContentItemSettingEventException =
                new InvalidContentItemSettingEventException(
                    message: "Invalid content item setting event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemSettingEventException);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>?> onAddingTask =
                this.contentItemSettingService.OnAddingContentItemSettingAsync(
                    invalidEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    onAddingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingContentItemSettingEventWhenContentIsNullAsync()
        {
            // given
            var invalidEnvelope = new EventEnvelope<ContentItemSetting>
            {
                Content = null!,
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var invalidContentItemSettingEventException =
                new InvalidContentItemSettingEventException(
                    message: "Invalid content item setting event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemSettingEventException);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>?> onAddingTask =
                this.contentItemSettingService.OnAddingContentItemSettingAsync(
                    invalidEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    onAddingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingContentItemSettingEventWhenIntegrityVerificationFailsAsync()
        {
            // given
            var forgedEnvelope = new EventEnvelope<ContentItemSetting>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new ContentItemSetting { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            string expectedEventName =
                $"{nameof(ContentItemSetting)}{ContentItemSettingEventOperation.Adding}";

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    forgedEnvelope,
                    expectedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(false);

            var invalidContentItemSettingEventException =
                new InvalidContentItemSettingEventException(
                    message: "Invalid content item setting event. Integrity verification failed.");

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemSettingEventException);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>?> onAddingTask =
                this.contentItemSettingService.OnAddingContentItemSettingAsync(
                    forgedEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    onAddingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    forgedEnvelope,
                    expectedEventName,
                    EnvelopeDirection.Request),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
