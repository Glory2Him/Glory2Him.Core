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
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingReviewerRoles
{
    public partial class ApprovalSettingReviewerRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidApprovalSettingReviewerRoleId = Guid.Empty;

            var invalidApprovalSettingReviewerRoleException = new InvalidApprovalSettingReviewerRoleException(
                message: "Approval setting reviewer role is invalid, fix the errors and try again.");

            invalidApprovalSettingReviewerRoleException.UpsertDataList(
                key: nameof(ApprovalSettingReviewerRole.Id),
                value: "Id is required");

            var expectedApprovalSettingReviewerRoleValidationException = new ApprovalSettingReviewerRoleValidationException(
                message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalSettingReviewerRoleException);

            // when
            ValueTask<ApprovalSettingReviewerRole> removeApprovalSettingReviewerRoleByIdTask =
                this.approvalSettingReviewerRoleService.RemoveApprovalSettingReviewerRoleByIdAsync(
                    invalidApprovalSettingReviewerRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    removeApprovalSettingReviewerRoleByIdTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfApprovalSettingReviewerRoleNotFoundAndLogItAsync()
        {
            // given
            Guid someApprovalSettingReviewerRoleId = Guid.NewGuid();
            ApprovalSettingReviewerRole noApprovalSettingReviewerRole = null;

            var notFoundApprovalSettingReviewerRoleException = new NotFoundApprovalSettingReviewerRoleException(
                message: $"Approval setting reviewer role not found with id: {someApprovalSettingReviewerRoleId}.");

            var expectedApprovalSettingReviewerRoleValidationException = new ApprovalSettingReviewerRoleValidationException(
                message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                innerException: notFoundApprovalSettingReviewerRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noApprovalSettingReviewerRole);

            // when
            ValueTask<ApprovalSettingReviewerRole> removeApprovalSettingReviewerRoleByIdTask =
                this.approvalSettingReviewerRoleService.RemoveApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    removeApprovalSettingReviewerRoleByIdTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
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
