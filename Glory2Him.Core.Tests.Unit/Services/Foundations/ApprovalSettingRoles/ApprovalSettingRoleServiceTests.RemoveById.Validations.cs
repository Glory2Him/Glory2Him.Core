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
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingRoles
{
    public partial class ApprovalSettingRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidApprovalSettingRoleId = Guid.Empty;

            var invalidApprovalSettingRoleException = new InvalidApprovalSettingRoleException(
                message: "Approval setting role is invalid, fix the errors and try again.");

            invalidApprovalSettingRoleException.UpsertDataList(
                key: nameof(ApprovalSettingRole.Id),
                value: "Id is required");

            var expectedApprovalSettingRoleValidationException = new ApprovalSettingRoleValidationException(
                message: "Approval setting role validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalSettingRoleException);

            // when
            ValueTask<ApprovalSettingRole> removeApprovalSettingRoleByIdTask =
                this.approvalSettingRoleService.RemoveApprovalSettingRoleByIdAsync(
                    invalidApprovalSettingRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingRoleValidationException actualApprovalSettingRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingRoleValidationException>(
                    removeApprovalSettingRoleByIdTask.AsTask);

            // then
            actualApprovalSettingRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfApprovalSettingRoleNotFoundAndLogItAsync()
        {
            // given
            Guid someApprovalSettingRoleId = Guid.NewGuid();
            ApprovalSettingRole noApprovalSettingRole = null;

            var notFoundApprovalSettingRoleException = new NotFoundApprovalSettingRoleException(
                message: $"Approval setting role not found with id: {someApprovalSettingRoleId}.");

            var expectedApprovalSettingRoleValidationException = new ApprovalSettingRoleValidationException(
                message: "Approval setting role validation error occurred, fix the errors and try again.",
                innerException: notFoundApprovalSettingRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noApprovalSettingRole);

            // when
            ValueTask<ApprovalSettingRole> removeApprovalSettingRoleByIdTask =
                this.approvalSettingRoleService.RemoveApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingRoleValidationException actualApprovalSettingRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingRoleValidationException>(
                    removeApprovalSettingRoleByIdTask.AsTask);

            // then
            actualApprovalSettingRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

    }
}
