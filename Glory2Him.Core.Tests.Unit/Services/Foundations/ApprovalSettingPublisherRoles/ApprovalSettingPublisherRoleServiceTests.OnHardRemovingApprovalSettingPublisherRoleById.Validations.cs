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
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingPublisherRoles
{
    public partial class ApprovalSettingPublisherRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingApprovalSettingPublisherRoleByIdEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<ApprovalSettingPublisherRole>? nullEnvelope = null;

            var invalidApprovalSettingPublisherRoleEventException =
                new InvalidApprovalSettingPublisherRoleEventException(
                    message: "Invalid approval setting publisher role event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalSettingPublisherRoleValidationException =
                new ApprovalSettingPublisherRoleValidationException(
                    message: "Approval setting publisher role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingPublisherRoleEventException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?> onHardRemovingTask =
                this.approvalSettingPublisherRoleService.OnHardRemovingApprovalSettingPublisherRoleByIdAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleValidationException actualApprovalSettingPublisherRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleValidationException>(
                    onHardRemovingTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingApprovalSettingPublisherRoleByIdEventWhenIdIsInvalidAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ApprovalSettingPublisherRole>
            {
                Content = new ApprovalSettingPublisherRole { Id = Guid.Empty },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var invalidApprovalSettingPublisherRoleException = new InvalidApprovalSettingPublisherRoleException(
                message: "Approval setting publisher role is invalid, fix the errors and try again.");

            invalidApprovalSettingPublisherRoleException.UpsertDataList(
                key: nameof(ApprovalSettingPublisherRole.Id),
                value: "Id is required");

            var expectedApprovalSettingPublisherRoleValidationException = new ApprovalSettingPublisherRoleValidationException(
                message: "Approval setting publisher role validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalSettingPublisherRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnHardRemovingApprovalSettingPublisherRoleByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            // when
            ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?> onHardRemovingTask =
                this.approvalSettingPublisherRoleService.OnHardRemovingApprovalSettingPublisherRoleByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleValidationException actualApprovalSettingPublisherRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleValidationException>(
                    onHardRemovingTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnHardRemovingApprovalSettingPublisherRoleByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingApprovalSettingPublisherRoleByIdEventWhenApprovalSettingPublisherRoleNotFoundAsync()
        {
            // given
            Guid someApprovalSettingPublisherRoleId = Guid.NewGuid();
            ApprovalSettingPublisherRole noApprovalSettingPublisherRole = null!;

            var requestEnvelope = new EventEnvelope<ApprovalSettingPublisherRole>
            {
                Content = new ApprovalSettingPublisherRole { Id = someApprovalSettingPublisherRoleId },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var notFoundApprovalSettingPublisherRoleException = new NotFoundApprovalSettingPublisherRoleException(
                message: $"Approval setting publisher role not found with id: {someApprovalSettingPublisherRoleId}.");

            var expectedApprovalSettingPublisherRoleValidationException = new ApprovalSettingPublisherRoleValidationException(
                message: "Approval setting publisher role validation error occurred, fix the errors and try again.",
                innerException: notFoundApprovalSettingPublisherRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnHardRemovingApprovalSettingPublisherRoleByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noApprovalSettingPublisherRole);

            // when
            ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?> onHardRemovingTask =
                this.approvalSettingPublisherRoleService.OnHardRemovingApprovalSettingPublisherRoleByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleValidationException actualApprovalSettingPublisherRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleValidationException>(
                    onHardRemovingTask.AsTask);

            // then: the raw not-found from the shared do-work is categorized the same way
            // the non-event path categorizes it — the event path must not degrade it to a
            // service exception.
            actualApprovalSettingPublisherRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
