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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingRoles
{
    public partial class ApprovalSettingRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveAllIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutApprovalSettingRoleException =
                new TimeoutApprovalSettingRoleException(
                    message: "Failed approval setting role timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedApprovalSettingRoleDependencyException = new ApprovalSettingRoleDependencyException(
                message: "Approval setting role dependency error occurred, contact support.",
                innerException: timeoutApprovalSettingRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalSettingRolesAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<IQueryable<ApprovalSettingRole>> retrieveAllApprovalSettingRolesTask =
                this.approvalSettingRoleService.RetrieveAllApprovalSettingRolesAsync(
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleDependencyException actualApprovalSettingRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingRoleDependencyException>(
                    retrieveAllApprovalSettingRolesTask.AsTask);

            // then
            actualApprovalSettingRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingRolesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRetrieveAllIfCancellationRequestedAsync()
        {
            // given
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<IQueryable<ApprovalSettingRole>> retrieveAllApprovalSettingRolesTask =
                this.approvalSettingRoleService.RetrieveAllApprovalSettingRolesAsync(cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                retrieveAllApprovalSettingRolesTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnRetrieveAllIfSqlErrorOccursAndLogItAsync()
        {
            // given
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalSettingRoleException = new FailedStorageApprovalSettingRoleException(
                message: "Failed approval setting role storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalSettingRoleDependencyException = new ApprovalSettingRoleDependencyException(
                message: "Approval setting role dependency error occurred, contact support.",
                innerException: failedStorageApprovalSettingRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalSettingRolesAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<IQueryable<ApprovalSettingRole>> retrieveAllApprovalSettingRolesTask =
                this.approvalSettingRoleService.RetrieveAllApprovalSettingRolesAsync(
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleDependencyException actualApprovalSettingRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingRoleDependencyException>(
                    retrieveAllApprovalSettingRolesTask.AsTask);

            // then
            actualApprovalSettingRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingRolesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrieveAllIfServiceErrorOccursAndLogItAsync()
        {
            // given
            var serviceException = new Exception();

            var failedApprovalSettingRoleServiceException = new FailedApprovalSettingRoleServiceException(
                message: "Failed approval setting role service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalSettingRoleServiceException = new ApprovalSettingRoleServiceException(
                message: "Approval setting role service error occurred, contact support.",
                innerException: failedApprovalSettingRoleServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalSettingRolesAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<IQueryable<ApprovalSettingRole>> retrieveAllApprovalSettingRolesTask =
                this.approvalSettingRoleService.RetrieveAllApprovalSettingRolesAsync(
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleServiceException actualApprovalSettingRoleServiceException =
                await Assert.ThrowsAsync<ApprovalSettingRoleServiceException>(
                    retrieveAllApprovalSettingRolesTask.AsTask);

            // then
            actualApprovalSettingRoleServiceException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingRolesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
