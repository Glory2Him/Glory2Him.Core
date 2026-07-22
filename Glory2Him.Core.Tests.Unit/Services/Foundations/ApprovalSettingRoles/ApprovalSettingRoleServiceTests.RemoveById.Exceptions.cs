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
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingRoles
{
    public partial class ApprovalSettingRoleServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRemoveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            Guid someApprovalSettingRoleId = Guid.NewGuid();

            var expectedApprovalSettingRoleDependencyException = new ApprovalSettingRoleDependencyException(
                message: "Approval setting role dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalSettingRole> removeApprovalSettingRoleByIdTask =
                this.approvalSettingRoleService.RemoveApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingRoleDependencyException actualApprovalSettingRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingRoleDependencyException>(
                    removeApprovalSettingRoleByIdTask.AsTask);

            // then
            actualApprovalSettingRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    TestContext.Current.CancellationToken),
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
        public async Task ShouldThrowDependencyExceptionOnRemoveByIdIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            Guid someApprovalSettingRoleId = Guid.NewGuid();
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
                broker.SelectApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ApprovalSettingRole> removeApprovalSettingRoleByIdTask =
                this.approvalSettingRoleService.RemoveApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingRoleDependencyException actualApprovalSettingRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingRoleDependencyException>(
                    removeApprovalSettingRoleByIdTask.AsTask);

            // then
            actualApprovalSettingRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    TestContext.Current.CancellationToken),
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
        public async Task ShouldThrowOperationCanceledExceptionOnRemoveByIdIfCancellationRequestedAsync()
        {
            // given
            Guid someApprovalSettingRoleId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ApprovalSettingRole> removeApprovalSettingRoleByIdTask =
                this.approvalSettingRoleService.RemoveApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    cancellationToken: cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                removeApprovalSettingRoleByIdTask.AsTask);

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
            Guid someApprovalSettingRoleId = Guid.NewGuid();
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalSettingRoleException = new FailedStorageApprovalSettingRoleException(
                message: "Failed approval setting role storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalSettingRoleDependencyException = new ApprovalSettingRoleDependencyException(
                message: "Approval setting role dependency error occurred, contact support.",
                innerException: failedStorageApprovalSettingRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<ApprovalSettingRole> removeApprovalSettingRoleByIdTask =
                this.approvalSettingRoleService.RemoveApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingRoleDependencyException actualApprovalSettingRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingRoleDependencyException>(
                    removeApprovalSettingRoleByIdTask.AsTask);

            // then
            actualApprovalSettingRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    TestContext.Current.CancellationToken),
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
        public async Task ShouldThrowDependencyValidationExceptionOnRemoveByIdIfDbUpdateConcurrencyExceptionOccursAndLogItAsync()
        {
            // given
            Guid someApprovalSettingRoleId = Guid.NewGuid();
            ApprovalSettingRole someApprovalSettingRole = CreateRandomApprovalSettingRole();
            someApprovalSettingRole.IsDeleted = false;
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();

            var lockedApprovalSettingRoleException = new LockedApprovalSettingRoleException(
                message: "Locked approval setting role record, please try again later.",
                innerException: dbUpdateConcurrencyException,
                data: dbUpdateConcurrencyException.Data);

            var expectedApprovalSettingRoleDependencyValidationException = new ApprovalSettingRoleDependencyValidationException(
                message: "Approval setting role dependency validation error occurred, fix the errors and try again.",
                innerException: lockedApprovalSettingRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(someApprovalSettingRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(someApprovalSettingRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(someApprovalSettingRole);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingRoleAsync(
                    someApprovalSettingRole,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(dbUpdateConcurrencyException);

            // when
            ValueTask<ApprovalSettingRole> removeApprovalSettingRoleByIdTask =
                this.approvalSettingRoleService.RemoveApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingRoleDependencyValidationException actualApprovalSettingRoleDependencyValidationException =
                await Assert.ThrowsAsync<ApprovalSettingRoleDependencyValidationException>(
                    removeApprovalSettingRoleByIdTask.AsTask);

            // then
            actualApprovalSettingRoleDependencyValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleDependencyValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(someApprovalSettingRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalSettingRoleAsync(
                    someApprovalSettingRole,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleDependencyValidationException))),
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
            Guid someApprovalSettingRoleId = Guid.NewGuid();
            var serviceException = new Exception();

            var failedApprovalSettingRoleServiceException = new FailedApprovalSettingRoleServiceException(
                message: "Failed approval setting role service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalSettingRoleServiceException = new ApprovalSettingRoleServiceException(
                message: "Approval setting role service error occurred, contact support.",
                innerException: failedApprovalSettingRoleServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalSettingRole> removeApprovalSettingRoleByIdTask =
                this.approvalSettingRoleService.RemoveApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingRoleServiceException actualApprovalSettingRoleServiceException =
                await Assert.ThrowsAsync<ApprovalSettingRoleServiceException>(
                    removeApprovalSettingRoleByIdTask.AsTask);

            // then
            actualApprovalSettingRoleServiceException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    TestContext.Current.CancellationToken),
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
