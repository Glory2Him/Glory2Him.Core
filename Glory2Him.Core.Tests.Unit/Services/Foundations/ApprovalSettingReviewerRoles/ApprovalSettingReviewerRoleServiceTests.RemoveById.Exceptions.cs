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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingReviewerRoles
{
    public partial class ApprovalSettingReviewerRoleServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRemoveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someApprovalSettingReviewerRoleId = Guid.NewGuid();

            var expectedApprovalSettingReviewerRoleDependencyException = new ApprovalSettingReviewerRoleDependencyException(
                message: "Approval setting reviewer role dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalSettingReviewerRole> removeApprovalSettingReviewerRoleByIdTask =
                this.approvalSettingReviewerRoleService.RemoveApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleDependencyException actualApprovalSettingReviewerRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleDependencyException>(
                    removeApprovalSettingReviewerRoleByIdTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRemoveByIdIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someApprovalSettingReviewerRoleId = Guid.NewGuid();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutApprovalSettingReviewerRoleException =
                new TimeoutApprovalSettingReviewerRoleException(
                    message: "Failed approval setting reviewer role timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedApprovalSettingReviewerRoleDependencyException = new ApprovalSettingReviewerRoleDependencyException(
                message: "Approval setting reviewer role dependency error occurred, contact support.",
                innerException: timeoutApprovalSettingReviewerRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ApprovalSettingReviewerRole> removeApprovalSettingReviewerRoleByIdTask =
                this.approvalSettingReviewerRoleService.RemoveApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleDependencyException actualApprovalSettingReviewerRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleDependencyException>(
                    removeApprovalSettingReviewerRoleByIdTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRemoveByIdIfCancellationRequestedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someApprovalSettingReviewerRoleId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ApprovalSettingReviewerRole> removeApprovalSettingReviewerRoleByIdTask =
                this.approvalSettingReviewerRoleService.RemoveApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    cancellationToken: cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                removeApprovalSettingReviewerRoleByIdTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnRemoveByIdIfSqlErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someApprovalSettingReviewerRoleId = Guid.NewGuid();
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalSettingReviewerRoleException = new FailedStorageApprovalSettingReviewerRoleException(
                message: "Failed approval setting reviewer role storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalSettingReviewerRoleDependencyException = new ApprovalSettingReviewerRoleDependencyException(
                message: "Approval setting reviewer role dependency error occurred, contact support.",
                innerException: failedStorageApprovalSettingReviewerRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<ApprovalSettingReviewerRole> removeApprovalSettingReviewerRoleByIdTask =
                this.approvalSettingReviewerRoleService.RemoveApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleDependencyException actualApprovalSettingReviewerRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleDependencyException>(
                    removeApprovalSettingReviewerRoleByIdTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyValidationExceptionOnRemoveByIdIfDbUpdateConcurrencyExceptionOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someApprovalSettingReviewerRoleId = Guid.NewGuid();
            ApprovalSettingReviewerRole someApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();
            someApprovalSettingReviewerRole.IsDeleted = false;
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();

            var lockedApprovalSettingReviewerRoleException = new LockedApprovalSettingReviewerRoleException(
                message: "Locked approval setting reviewer role record, please try again later.",
                innerException: dbUpdateConcurrencyException,
                data: dbUpdateConcurrencyException.Data);

            var expectedApprovalSettingReviewerRoleDependencyValidationException = new ApprovalSettingReviewerRoleDependencyValidationException(
                message: "Approval setting reviewer role dependency validation error occurred, fix the errors and try again.",
                innerException: lockedApprovalSettingReviewerRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(someApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(someApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(someApprovalSettingReviewerRole);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingReviewerRoleAsync(
                    someApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(dbUpdateConcurrencyException);

            // when
            ValueTask<ApprovalSettingReviewerRole> removeApprovalSettingReviewerRoleByIdTask =
                this.approvalSettingReviewerRoleService.RemoveApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleDependencyValidationException actualApprovalSettingReviewerRoleDependencyValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleDependencyValidationException>(
                    removeApprovalSettingReviewerRoleByIdTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleDependencyValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleDependencyValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(someApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalSettingReviewerRoleAsync(
                    someApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRemoveByIdIfServiceErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someApprovalSettingReviewerRoleId = Guid.NewGuid();
            var serviceException = new Exception();

            var failedApprovalSettingReviewerRoleServiceException = new FailedApprovalSettingReviewerRoleServiceException(
                message: "Failed approval setting reviewer role service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalSettingReviewerRoleServiceException = new ApprovalSettingReviewerRoleServiceException(
                message: "Approval setting reviewer role service error occurred, contact support.",
                innerException: failedApprovalSettingReviewerRoleServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalSettingReviewerRole> removeApprovalSettingReviewerRoleByIdTask =
                this.approvalSettingReviewerRoleService.RemoveApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleServiceException actualApprovalSettingReviewerRoleServiceException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleServiceException>(
                    removeApprovalSettingReviewerRoleByIdTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleServiceException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
