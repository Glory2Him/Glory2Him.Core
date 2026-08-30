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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    public partial class ApprovalSettingServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            var invalidApprovalSettingId = Guid.Empty;

            var invalidApprovalSettingException = new InvalidApprovalSettingException(
                message: "Approval setting is invalid, fix the errors and try again.");

            invalidApprovalSettingException.UpsertDataList(
                key: nameof(ApprovalSetting.Id),
                value: "Id is required");

            var expectedApprovalSettingValidationException = new ApprovalSettingValidationException(
                message: "Approval setting validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalSettingException);

            // when
            ValueTask<ApprovalSetting> removeApprovalSettingByIdTask =
                this.approvalSettingService.RemoveApprovalSettingByIdAsync(
                    invalidApprovalSettingId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    removeApprovalSettingByIdTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfDeletionReasonExceedsMaxLengthAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            Guid someApprovalSettingId = Guid.NewGuid();
            string invalidDeletionReason = GetRandomStringWithLengthOf(501);

            var invalidApprovalSettingException = new InvalidApprovalSettingException(
                message: "Approval setting is invalid, fix the errors and try again.");

            invalidApprovalSettingException.UpsertDataList(
                key: nameof(ApprovalSetting.DeletionReason),
                value: $"Text exceed max length of {invalidDeletionReason.Length - 1} characters");

            var expectedApprovalSettingValidationException = new ApprovalSettingValidationException(
                message: "Approval setting validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalSettingException);

            // when
            ValueTask<ApprovalSetting> removeApprovalSettingByIdTask =
                this.approvalSettingService.RemoveApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    deletionReason: invalidDeletionReason,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    removeApprovalSettingByIdTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfApprovalSettingNotFoundAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            Guid someApprovalSettingId = Guid.NewGuid();
            ApprovalSetting noApprovalSetting = null;

            var notFoundApprovalSettingException = new NotFoundApprovalSettingException(
                message: $"Approval setting not found with id: {someApprovalSettingId}.");

            var expectedApprovalSettingValidationException = new ApprovalSettingValidationException(
                message: "Approval setting validation error occurred, fix the errors and try again.",
                innerException: notFoundApprovalSettingException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noApprovalSetting);

            // when
            ValueTask<ApprovalSetting> removeApprovalSettingByIdTask =
                this.approvalSettingService.RemoveApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    removeApprovalSettingByIdTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }


        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            Guid someApprovalSettingId = Guid.NewGuid();

            var unauthorizedApprovalSettingException = new UnauthorizedApprovalSettingException(
                message: "The current user is not authenticated.");

            var expectedApprovalSettingValidationException = new ApprovalSettingValidationException(
                message: "Approval setting validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalSettingException);

            // when
            ValueTask<ApprovalSetting> removeApprovalSettingByIdTask =
                this.approvalSettingService.RemoveApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    removeApprovalSettingByIdTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(NonAdminRoleSets))]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserIsNotAdminAndLogItAsync(
            string[] nonAdminRoles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonAdminRoles);
            Guid someApprovalSettingId = Guid.NewGuid();

            var unauthorizedApprovalSettingException = new UnauthorizedApprovalSettingException(
                message: "The current user is not allowed to administer approval settings.");

            var expectedApprovalSettingValidationException = new ApprovalSettingValidationException(
                message: "Approval setting validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalSettingException);

            // when
            ValueTask<ApprovalSetting> removeApprovalSettingByIdTask =
                this.approvalSettingService.RemoveApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    removeApprovalSettingByIdTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
