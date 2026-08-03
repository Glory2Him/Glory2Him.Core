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
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingReviewerRoles
{
    public partial class ApprovalSettingReviewerRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidApprovalSettingReviewerRoleId = Guid.Empty;

            var invalidApprovalSettingReviewerRoleException = new InvalidApprovalSettingReviewerRoleException(
                message: "Approval setting reviewer role is invalid, fix the errors and try again.");

            invalidApprovalSettingReviewerRoleException.UpsertDataList(
                key: "Id",
                value: "Id is required");

            var expectedApprovalSettingReviewerRoleValidationException = new ApprovalSettingReviewerRoleValidationException(
                message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalSettingReviewerRoleException);

            // when
            ValueTask<Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles.ApprovalSettingReviewerRole> retrieveApprovalSettingReviewerRoleByIdTask =
                this.approvalSettingReviewerRoleService.RetrieveApprovalSettingReviewerRoleByIdAsync(
                    invalidApprovalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    retrieveApprovalSettingReviewerRoleByIdTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfApprovalSettingReviewerRoleNotFoundAndLogItAsync()
        {
            // given
            Guid someApprovalSettingReviewerRoleId = Guid.NewGuid();
            ApprovalSettingReviewerRole nullApprovalSettingReviewerRole = null;

            var notFoundApprovalSettingReviewerRoleException =
                new NotFoundApprovalSettingReviewerRoleException(
                    message: $"Approval setting reviewer role not found with id: {someApprovalSettingReviewerRoleId}.");

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: notFoundApprovalSettingReviewerRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(nullApprovalSettingReviewerRole);

            // when
            ValueTask<ApprovalSettingReviewerRole> retrieveApprovalSettingReviewerRoleByIdTask =
                this.approvalSettingReviewerRoleService.RetrieveApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    retrieveApprovalSettingReviewerRoleByIdTask.AsTask);

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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfApprovalSettingReviewerRoleIsSoftDeletedAndLogItAsync()
        {
            // given: even an Admin caller gets not-found for a soft-deleted row —
            // deleted beats privilege
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ApprovalSettingReviewerRole storageApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();
            storageApprovalSettingReviewerRole.IsDeleted = true;
            Guid approvalSettingReviewerRoleId = storageApprovalSettingReviewerRole.Id;

            var notFoundApprovalSettingReviewerRoleException = new NotFoundApprovalSettingReviewerRoleException(
                message: $"Approval setting reviewer role not found with id: {approvalSettingReviewerRoleId}.");

            var expectedApprovalSettingReviewerRoleValidationException = new ApprovalSettingReviewerRoleValidationException(
                message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                innerException: notFoundApprovalSettingReviewerRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    approvalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalSettingReviewerRole);

            // when
            ValueTask<ApprovalSettingReviewerRole> retrieveApprovalSettingReviewerRoleByIdTask =
                this.approvalSettingReviewerRoleService.RetrieveApprovalSettingReviewerRoleByIdAsync(
                    approvalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    retrieveApprovalSettingReviewerRoleByIdTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    approvalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(
                    "Approval setting reviewer role read denied. Approval setting reviewer role " +
                        $"{approvalSettingReviewerRoleId} is soft-deleted; reported to the caller as not found."),
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

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given: approval policy has no public face — an anonymous caller is told
            // not-found, never unauthorized
            this.ambientSecurityContext = invalidSecurityContext;
            ApprovalSettingReviewerRole storageApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();
            storageApprovalSettingReviewerRole.IsDeleted = false;
            Guid approvalSettingReviewerRoleId = storageApprovalSettingReviewerRole.Id;

            var notFoundApprovalSettingReviewerRoleException = new NotFoundApprovalSettingReviewerRoleException(
                message: $"Approval setting reviewer role not found with id: {approvalSettingReviewerRoleId}.");

            var expectedApprovalSettingReviewerRoleValidationException = new ApprovalSettingReviewerRoleValidationException(
                message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                innerException: notFoundApprovalSettingReviewerRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    approvalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalSettingReviewerRole);

            // when
            ValueTask<ApprovalSettingReviewerRole> retrieveApprovalSettingReviewerRoleByIdTask =
                this.approvalSettingReviewerRoleService.RetrieveApprovalSettingReviewerRoleByIdAsync(
                    approvalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    retrieveApprovalSettingReviewerRoleByIdTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    approvalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    "Approval setting reviewer role read denied. Approval setting reviewer role " +
                        $"{approvalSettingReviewerRoleId} is only readable by an authenticated caller and the " +
                        "caller is not authenticated; reported to the caller as not found."),
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
