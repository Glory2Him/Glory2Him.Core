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
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingPublisherRoles
{
    public partial class ApprovalSettingPublisherRoleServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRemoveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someApprovalSettingPublisherRoleId = Guid.NewGuid();

            var expectedApprovalSettingPublisherRoleDependencyException = new ApprovalSettingPublisherRoleDependencyException(
                message: "Approval setting publisher role dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalSettingPublisherRole> removeApprovalSettingPublisherRoleByIdTask =
                this.approvalSettingPublisherRoleService.RemoveApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleDependencyException actualApprovalSettingPublisherRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleDependencyException>(
                    removeApprovalSettingPublisherRoleByIdTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleDependencyException))),
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
            Guid someApprovalSettingPublisherRoleId = Guid.NewGuid();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutApprovalSettingPublisherRoleException =
                new TimeoutApprovalSettingPublisherRoleException(
                    message: "Failed approval setting publisher role timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedApprovalSettingPublisherRoleDependencyException = new ApprovalSettingPublisherRoleDependencyException(
                message: "Approval setting publisher role dependency error occurred, contact support.",
                innerException: timeoutApprovalSettingPublisherRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ApprovalSettingPublisherRole> removeApprovalSettingPublisherRoleByIdTask =
                this.approvalSettingPublisherRoleService.RemoveApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleDependencyException actualApprovalSettingPublisherRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleDependencyException>(
                    removeApprovalSettingPublisherRoleByIdTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleDependencyException))),
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
            Guid someApprovalSettingPublisherRoleId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ApprovalSettingPublisherRole> removeApprovalSettingPublisherRoleByIdTask =
                this.approvalSettingPublisherRoleService.RemoveApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    cancellationToken: cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                removeApprovalSettingPublisherRoleByIdTask.AsTask);

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
            Guid someApprovalSettingPublisherRoleId = Guid.NewGuid();
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalSettingPublisherRoleException = new FailedStorageApprovalSettingPublisherRoleException(
                message: "Failed approval setting publisher role storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalSettingPublisherRoleDependencyException = new ApprovalSettingPublisherRoleDependencyException(
                message: "Approval setting publisher role dependency error occurred, contact support.",
                innerException: failedStorageApprovalSettingPublisherRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<ApprovalSettingPublisherRole> removeApprovalSettingPublisherRoleByIdTask =
                this.approvalSettingPublisherRoleService.RemoveApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleDependencyException actualApprovalSettingPublisherRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleDependencyException>(
                    removeApprovalSettingPublisherRoleByIdTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleDependencyException))),
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
            Guid someApprovalSettingPublisherRoleId = Guid.NewGuid();
            ApprovalSettingPublisherRole someApprovalSettingPublisherRole = CreateRandomApprovalSettingPublisherRole();
            someApprovalSettingPublisherRole.IsDeleted = false;
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();

            var lockedApprovalSettingPublisherRoleException = new LockedApprovalSettingPublisherRoleException(
                message: "Locked approval setting publisher role record, please try again later.",
                innerException: dbUpdateConcurrencyException,
                data: dbUpdateConcurrencyException.Data);

            var expectedApprovalSettingPublisherRoleDependencyValidationException = new ApprovalSettingPublisherRoleDependencyValidationException(
                message: "Approval setting publisher role dependency validation error occurred, fix the errors and try again.",
                innerException: lockedApprovalSettingPublisherRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(someApprovalSettingPublisherRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(someApprovalSettingPublisherRole);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingPublisherRoleAsync(
                    someApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(dbUpdateConcurrencyException);

            // when
            ValueTask<ApprovalSettingPublisherRole> removeApprovalSettingPublisherRoleByIdTask =
                this.approvalSettingPublisherRoleService.RemoveApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleDependencyValidationException actualApprovalSettingPublisherRoleDependencyValidationException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleDependencyValidationException>(
                    removeApprovalSettingPublisherRoleByIdTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleDependencyValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleDependencyValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalSettingPublisherRoleAsync(
                    someApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleDependencyValidationException))),
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
            Guid someApprovalSettingPublisherRoleId = Guid.NewGuid();
            var serviceException = new Exception();

            var failedApprovalSettingPublisherRoleServiceException = new FailedApprovalSettingPublisherRoleServiceException(
                message: "Failed approval setting publisher role service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalSettingPublisherRoleServiceException = new ApprovalSettingPublisherRoleServiceException(
                message: "Approval setting publisher role service error occurred, contact support.",
                innerException: failedApprovalSettingPublisherRoleServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalSettingPublisherRole> removeApprovalSettingPublisherRoleByIdTask =
                this.approvalSettingPublisherRoleService.RemoveApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleServiceException actualApprovalSettingPublisherRoleServiceException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleServiceException>(
                    removeApprovalSettingPublisherRoleByIdTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleServiceException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
