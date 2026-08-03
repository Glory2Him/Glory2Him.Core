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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingReviewerRoles
{
    public partial class ApprovalSettingReviewerRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingApprovalSettingReviewerRoleEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<ApprovalSettingReviewerRole>? nullEnvelope = null;

            var invalidApprovalSettingReviewerRoleEventException =
                new InvalidApprovalSettingReviewerRoleEventException(
                    message: "Invalid approval setting reviewer role event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingReviewerRoleEventException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?> onAddingTask =
                this.approvalSettingReviewerRoleService.OnAddingApprovalSettingReviewerRoleAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingApprovalSettingReviewerRoleEventWhenMetadataIsNullAsync()
        {
            // given
            var invalidEnvelope = new EventEnvelope<ApprovalSettingReviewerRole>
            {
                Content = new ApprovalSettingReviewerRole { Id = Guid.NewGuid() },
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin),
                Metadata = null!
            };

            var invalidApprovalSettingReviewerRoleEventException =
                new InvalidApprovalSettingReviewerRoleEventException(
                    message: "Invalid approval setting reviewer role event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingReviewerRoleEventException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?> onAddingTask =
                this.approvalSettingReviewerRoleService.OnAddingApprovalSettingReviewerRoleAsync(
                    invalidEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingApprovalSettingReviewerRoleEventWhenContentIsNullAsync()
        {
            // given
            var invalidEnvelope = new EventEnvelope<ApprovalSettingReviewerRole>
            {
                Content = null!,
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var invalidApprovalSettingReviewerRoleEventException =
                new InvalidApprovalSettingReviewerRoleEventException(
                    message: "Invalid approval setting reviewer role event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingReviewerRoleEventException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?> onAddingTask =
                this.approvalSettingReviewerRoleService.OnAddingApprovalSettingReviewerRoleAsync(
                    invalidEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(NonAdminRoleSets))]
        public async Task ShouldThrowValidationExceptionOnAddingApprovalSettingReviewerRoleEventWhenUserIsNotAdminAsync(
            string[] nonAdminRoles)
        {
            // given: the event path shares the write gate — the request envelope's
            // caller is the one gated, not the ambient caller
            EventEnvelope<ApprovalSettingReviewerRole> requestEnvelope =
                CreateRandomApprovalSettingReviewerRoleRequestEnvelope(
                    securityContext: CreateAuthenticatedSecurityContext(nonAdminRoles));

            var unauthorizedApprovalSettingReviewerRoleException = new UnauthorizedApprovalSettingReviewerRoleException(
                message: "The current user is not allowed to administer approval setting reviewer roles.");

            var expectedApprovalSettingReviewerRoleValidationException = new ApprovalSettingReviewerRoleValidationException(
                message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalSettingReviewerRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnAddingApprovalSettingReviewerRoleSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            // when
            ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?> onAddingTask =
                this.approvalSettingReviewerRoleService.OnAddingApprovalSettingReviewerRoleAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    onAddingTask.AsTask);

            // then: the row is never inserted
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnAddingApprovalSettingReviewerRoleSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
