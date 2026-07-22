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
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingRoles
{
    public partial class ApprovalSettingRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingApprovalSettingRoleEventWhenEnvelopeIsInvalidAsync()
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
            ValueTask<EventEnvelope<ApprovalSettingRole>?> onAddingTask =
                this.approvalSettingRoleService.OnAddingApprovalSettingRoleAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleValidationException actualApprovalSettingRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingRoleValidationException>(
                    onAddingTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnAddingApprovalSettingRoleEventWhenMetadataIsNullAsync()
        {
            // given
            var invalidEnvelope = new EventEnvelope<ApprovalSettingRole>
            {
                Content = new ApprovalSettingRole { Id = Guid.NewGuid() },
                Metadata = null!
            };

            var invalidApprovalSettingRoleEventException =
                new InvalidApprovalSettingRoleEventException(
                    message: "Invalid approval setting role event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalSettingRoleValidationException =
                new ApprovalSettingRoleValidationException(
                    message: "Approval setting role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingRoleEventException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingRole>?> onAddingTask =
                this.approvalSettingRoleService.OnAddingApprovalSettingRoleAsync(
                    invalidEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleValidationException actualApprovalSettingRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingRoleValidationException>(
                    onAddingTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnAddingApprovalSettingRoleEventWhenContentIsNullAsync()
        {
            // given
            var invalidEnvelope = new EventEnvelope<ApprovalSettingRole>
            {
                Content = null!,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var invalidApprovalSettingRoleEventException =
                new InvalidApprovalSettingRoleEventException(
                    message: "Invalid approval setting role event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalSettingRoleValidationException =
                new ApprovalSettingRoleValidationException(
                    message: "Approval setting role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingRoleEventException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingRole>?> onAddingTask =
                this.approvalSettingRoleService.OnAddingApprovalSettingRoleAsync(
                    invalidEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleValidationException actualApprovalSettingRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingRoleValidationException>(
                    onAddingTask.AsTask);

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
    }
}
