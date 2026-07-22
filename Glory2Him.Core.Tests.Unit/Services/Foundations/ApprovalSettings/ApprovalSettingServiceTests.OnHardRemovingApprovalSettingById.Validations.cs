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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    public partial class ApprovalSettingServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingApprovalSettingByIdEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<ApprovalSetting>? nullEnvelope = null;

            var invalidApprovalSettingEventException =
                new InvalidApprovalSettingEventException(
                    message: "Invalid approval setting event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalSettingValidationException =
                new ApprovalSettingValidationException(
                    message: "Approval setting validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingEventException);

            // when
            ValueTask<EventEnvelope<ApprovalSetting>?> onHardRemovingTask =
                this.approvalSettingService.OnHardRemovingApprovalSettingByIdAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    onHardRemovingTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingApprovalSettingByIdEventWhenIdIsInvalidAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ApprovalSetting>
            {
                Content = new ApprovalSetting { Id = Guid.Empty },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var invalidApprovalSettingException = new InvalidApprovalSettingException(
                message: "Approval setting is invalid, fix the errors and try again.");

            invalidApprovalSettingException.UpsertDataList(
                key: nameof(ApprovalSetting.Id),
                value: "Id is required");

            var expectedApprovalSettingValidationException = new ApprovalSettingValidationException(
                message: "Approval setting validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalSettingException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingOnHardRemovingApprovalSettingByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            // when
            ValueTask<EventEnvelope<ApprovalSetting>?> onHardRemovingTask =
                this.approvalSettingService.OnHardRemovingApprovalSettingByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    onHardRemovingTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingOnHardRemovingApprovalSettingByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingApprovalSettingByIdEventWhenApprovalSettingNotFoundAsync()
        {
            // given
            Guid someApprovalSettingId = Guid.NewGuid();
            ApprovalSetting noApprovalSetting = null!;

            var requestEnvelope = new EventEnvelope<ApprovalSetting>
            {
                Content = new ApprovalSetting { Id = someApprovalSettingId },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var notFoundApprovalSettingException = new NotFoundApprovalSettingException(
                message: $"Approval setting not found with id: {someApprovalSettingId}.");

            var expectedApprovalSettingValidationException = new ApprovalSettingValidationException(
                message: "Approval setting validation error occurred, fix the errors and try again.",
                innerException: notFoundApprovalSettingException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingOnHardRemovingApprovalSettingByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noApprovalSetting);

            // when
            ValueTask<EventEnvelope<ApprovalSetting>?> onHardRemovingTask =
                this.approvalSettingService.OnHardRemovingApprovalSettingByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    onHardRemovingTask.AsTask);

            // then: the raw not-found from the shared do-work is categorized the same way
            // the non-event path categorizes it — the event path must not degrade it to a
            // service exception.
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
