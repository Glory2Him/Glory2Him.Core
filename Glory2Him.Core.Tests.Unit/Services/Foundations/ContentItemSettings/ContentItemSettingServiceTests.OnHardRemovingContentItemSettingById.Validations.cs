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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.ContentItemSettings.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemSettings
{
    public partial class ContentItemSettingServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingContentItemSettingByIdEventWhenEnvelopeIsInvalidAsync()
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
            ValueTask<EventEnvelope<ContentItemSetting>?> onHardRemovingTask =
                this.contentItemSettingService.OnHardRemovingContentItemSettingByIdAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    onHardRemovingTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnHardRemovingContentItemSettingByIdEventWhenIdIsInvalidAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ContentItemSetting>
            {
                Content = new ContentItemSetting { Id = Guid.Empty },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var invalidContentItemSettingException = new InvalidContentItemSettingException(
                message: "Content item setting is invalid, fix the errors and try again.");

            invalidContentItemSettingException.UpsertDataList(
                key: nameof(ContentItemSetting.Id),
                value: "Id is required");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: invalidContentItemSettingException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemSettingOnHardRemovingContentItemSettingByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>?> onHardRemovingTask =
                this.contentItemSettingService.OnHardRemovingContentItemSettingByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    onHardRemovingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemSettingOnHardRemovingContentItemSettingByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

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
        public async Task ShouldThrowValidationExceptionOnHardRemovingContentItemSettingByIdEventWhenContentItemSettingNotFoundAsync()
        {
            // given
            Guid someContentItemSettingId = Guid.NewGuid();
            ContentItemSetting noContentItemSetting = null!;

            var requestEnvelope = new EventEnvelope<ContentItemSetting>
            {
                Content = new ContentItemSetting { Id = someContentItemSettingId },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var notFoundContentItemSettingException = new NotFoundContentItemSettingException(
                message: $"Content item setting not found with id: {someContentItemSettingId}.");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: notFoundContentItemSettingException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemSettingOnHardRemovingContentItemSettingByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noContentItemSetting);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>?> onHardRemovingTask =
                this.contentItemSettingService.OnHardRemovingContentItemSettingByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    onHardRemovingTask.AsTask);

            // then: the raw not-found from the shared do-work is categorized the same way
            // the non-event path categorizes it — the event path must not degrade it to a
            // service exception.
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
