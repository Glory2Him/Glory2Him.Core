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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    public partial class ApprovalSettingServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
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
            ValueTask<ApprovalSetting> hardRemoveApprovalSettingByIdTask =
                this.approvalSettingService.HardRemoveApprovalSettingByIdAsync(
                    invalidApprovalSettingId,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    hardRemoveApprovalSettingByIdTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfApprovalSettingNotFoundAndLogItAsync()
        {
            // given
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
            ValueTask<ApprovalSetting> hardRemoveApprovalSettingByIdTask =
                this.approvalSettingService.HardRemoveApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    hardRemoveApprovalSettingByIdTask.AsTask);

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
    }
}
