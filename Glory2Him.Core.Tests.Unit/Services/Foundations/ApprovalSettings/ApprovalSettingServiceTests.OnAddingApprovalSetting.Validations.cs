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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    public partial class ApprovalSettingServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingApprovalSettingEventWhenEnvelopeIsInvalidAsync()
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
            ValueTask<EventEnvelope<ApprovalSetting>?> onAddingTask =
                this.approvalSettingService.OnAddingApprovalSettingAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    onAddingTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnAddingApprovalSettingEventWhenMetadataIsNullAsync()
        {
            // given
            var invalidEnvelope = new EventEnvelope<ApprovalSetting>
            {
                Content = new ApprovalSetting { Id = Guid.NewGuid() },
                Metadata = null!
            };

            var invalidApprovalSettingEventException =
                new InvalidApprovalSettingEventException(
                    message: "Invalid approval setting event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalSettingValidationException =
                new ApprovalSettingValidationException(
                    message: "Approval setting validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingEventException);

            // when
            ValueTask<EventEnvelope<ApprovalSetting>?> onAddingTask =
                this.approvalSettingService.OnAddingApprovalSettingAsync(
                    invalidEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    onAddingTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnAddingApprovalSettingEventWhenContentIsNullAsync()
        {
            // given
            var invalidEnvelope = new EventEnvelope<ApprovalSetting>
            {
                Content = null!,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var invalidApprovalSettingEventException =
                new InvalidApprovalSettingEventException(
                    message: "Invalid approval setting event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalSettingValidationException =
                new ApprovalSettingValidationException(
                    message: "Approval setting validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingEventException);

            // when
            ValueTask<EventEnvelope<ApprovalSetting>?> onAddingTask =
                this.approvalSettingService.OnAddingApprovalSettingAsync(
                    invalidEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    onAddingTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnAddingApprovalSettingEventWhenIntegrityVerificationFailsAsync()
        {
            // given
            var forgedEnvelope = new EventEnvelope<ApprovalSetting>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new ApprovalSetting { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            string expectedEventName =
                $"{nameof(ApprovalSetting)}{ApprovalSettingEventOperation.Adding}";

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    forgedEnvelope,
                    expectedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(false);

            var invalidApprovalSettingEventException =
                new InvalidApprovalSettingEventException(
                    message: "Invalid approval setting event. Integrity verification failed.");

            var expectedApprovalSettingValidationException =
                new ApprovalSettingValidationException(
                    message: "Approval setting validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingEventException);

            // when
            ValueTask<EventEnvelope<ApprovalSetting>?> onAddingTask =
                this.approvalSettingService.OnAddingApprovalSettingAsync(
                    forgedEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    forgedEnvelope,
                    expectedEventName,
                    EnvelopeDirection.Request),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
