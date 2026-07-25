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
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingPublisherRoles
{
    public partial class ApprovalSettingPublisherRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidApprovalSettingPublisherRoleId = Guid.Empty;

            var invalidApprovalSettingPublisherRoleException = new InvalidApprovalSettingPublisherRoleException(
                message: "Approval setting publisher role is invalid, fix the errors and try again.");

            invalidApprovalSettingPublisherRoleException.UpsertDataList(
                key: "Id",
                value: "Id is required");

            var expectedApprovalSettingPublisherRoleValidationException = new ApprovalSettingPublisherRoleValidationException(
                message: "Approval setting publisher role validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalSettingPublisherRoleException);

            // when
            ValueTask<Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles.ApprovalSettingPublisherRole> retrieveApprovalSettingPublisherRoleByIdTask =
                this.approvalSettingPublisherRoleService.RetrieveApprovalSettingPublisherRoleByIdAsync(
                    invalidApprovalSettingPublisherRoleId,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleValidationException actualApprovalSettingPublisherRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleValidationException>(
                    retrieveApprovalSettingPublisherRoleByIdTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfApprovalSettingPublisherRoleNotFoundAndLogItAsync()
        {
            // given
            Guid someApprovalSettingPublisherRoleId = Guid.NewGuid();
            ApprovalSettingPublisherRole nullApprovalSettingPublisherRole = null;

            var notFoundApprovalSettingPublisherRoleException =
                new NotFoundApprovalSettingPublisherRoleException(
                    message: $"Approval setting publisher role not found with id: {someApprovalSettingPublisherRoleId}.");

            var expectedApprovalSettingPublisherRoleValidationException =
                new ApprovalSettingPublisherRoleValidationException(
                    message: "Approval setting publisher role validation error occurred, fix the errors and try again.",
                    innerException: notFoundApprovalSettingPublisherRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(nullApprovalSettingPublisherRole);

            // when
            ValueTask<ApprovalSettingPublisherRole> retrieveApprovalSettingPublisherRoleByIdTask =
                this.approvalSettingPublisherRoleService.RetrieveApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleValidationException actualApprovalSettingPublisherRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleValidationException>(
                    retrieveApprovalSettingPublisherRoleByIdTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
