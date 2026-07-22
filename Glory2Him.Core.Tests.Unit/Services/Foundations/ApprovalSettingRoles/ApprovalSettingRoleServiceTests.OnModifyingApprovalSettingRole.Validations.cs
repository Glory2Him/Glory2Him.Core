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
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingRoles
{
    public partial class ApprovalSettingRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyingApprovalSettingRoleEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<ApprovalSettingRole>? nullEnvelope = null;

            var invalidApprovalSettingRoleEventException =
                new InvalidApprovalSettingRoleEventException(
                    message: "Invalid approval setting role event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalSettingRoleValidationException =
                new ApprovalSettingRoleValidationException(
                    message: "Approval setting role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingRoleEventException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingRole>?> onModifyingTask =
                this.approvalSettingRoleService.OnModifyingApprovalSettingRoleAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleValidationException actualApprovalSettingRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingRoleValidationException>(
                    onModifyingTask.AsTask);

            // then
            actualApprovalSettingRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyingApprovalSettingRoleEventWhenApprovalSettingRoleNotFoundAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSettingRole inputApprovalSettingRole = CreateRandomModifyApprovalSettingRole(randomDateTimeOffset, randomUserId);
            ApprovalSettingRole noApprovalSettingRole = null!;

            var requestEnvelope = new EventEnvelope<ApprovalSettingRole>
            {
                Content = inputApprovalSettingRole,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var notFoundApprovalSettingRoleException = new NotFoundApprovalSettingRoleException(
                message: $"Approval setting role not found with id: {inputApprovalSettingRole.Id}.");

            var expectedApprovalSettingRoleValidationException = new ApprovalSettingRoleValidationException(
                message: "Approval setting role validation error occurred, fix the errors and try again.",
                innerException: notFoundApprovalSettingRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingRoleOnModifyingApprovalSettingRoleSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputApprovalSettingRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputApprovalSettingRole);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    inputApprovalSettingRole.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noApprovalSettingRole);

            // when
            ValueTask<EventEnvelope<ApprovalSettingRole>?> onModifyingTask =
                this.approvalSettingRoleService.OnModifyingApprovalSettingRoleAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleValidationException actualApprovalSettingRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingRoleValidationException>(
                    onModifyingTask.AsTask);

            // then: the raw not-found from the shared do-work is categorized the same way
            // the non-event path categorizes it — the event path must not degrade it to a
            // service exception.
            actualApprovalSettingRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    inputApprovalSettingRole.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleValidationException))),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
