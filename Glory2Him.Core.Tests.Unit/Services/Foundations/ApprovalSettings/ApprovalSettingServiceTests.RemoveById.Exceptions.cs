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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    public partial class ApprovalSettingServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRemoveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            Guid someApprovalSettingId = Guid.NewGuid();

            var expectedApprovalSettingDependencyException = new ApprovalSettingDependencyException(
                message: "Approval setting dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalSetting> removeApprovalSettingByIdTask =
                this.approvalSettingService.RemoveApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingDependencyException actualApprovalSettingDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingDependencyException>(
                    removeApprovalSettingByIdTask.AsTask);

            // then
            actualApprovalSettingDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingDependencyException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            Guid someApprovalSettingId = Guid.NewGuid();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutApprovalSettingException =
                new TimeoutApprovalSettingException(
                    message: "Failed approval setting timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedApprovalSettingDependencyException = new ApprovalSettingDependencyException(
                message: "Approval setting dependency error occurred, contact support.",
                innerException: timeoutApprovalSettingException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ApprovalSetting> removeApprovalSettingByIdTask =
                this.approvalSettingService.RemoveApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingDependencyException actualApprovalSettingDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingDependencyException>(
                    removeApprovalSettingByIdTask.AsTask);

            // then
            actualApprovalSettingDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingDependencyException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            Guid someApprovalSettingId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ApprovalSetting> removeApprovalSettingByIdTask =
                this.approvalSettingService.RemoveApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    cancellationToken: cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                removeApprovalSettingByIdTask.AsTask);

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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            Guid someApprovalSettingId = Guid.NewGuid();
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalSettingException = new FailedStorageApprovalSettingException(
                message: "Failed approval setting storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalSettingDependencyException = new ApprovalSettingDependencyException(
                message: "Approval setting dependency error occurred, contact support.",
                innerException: failedStorageApprovalSettingException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<ApprovalSetting> removeApprovalSettingByIdTask =
                this.approvalSettingService.RemoveApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingDependencyException actualApprovalSettingDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingDependencyException>(
                    removeApprovalSettingByIdTask.AsTask);

            // then
            actualApprovalSettingDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingDependencyException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            Guid someApprovalSettingId = Guid.NewGuid();
            ApprovalSetting someApprovalSetting = CreateRandomApprovalSetting();
            someApprovalSetting.IsDeleted = false;
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();

            var lockedApprovalSettingException = new LockedApprovalSettingException(
                message: "Locked approval setting record, please try again later.",
                innerException: dbUpdateConcurrencyException,
                data: dbUpdateConcurrencyException.Data);

            var expectedApprovalSettingDependencyValidationException = new ApprovalSettingDependencyValidationException(
                message: "Approval setting dependency validation error occurred, fix the errors and try again.",
                innerException: lockedApprovalSettingException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(someApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(someApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(someApprovalSetting);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingAsync(
                    someApprovalSetting,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(dbUpdateConcurrencyException);

            // when
            ValueTask<ApprovalSetting> removeApprovalSettingByIdTask =
                this.approvalSettingService.RemoveApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingDependencyValidationException actualApprovalSettingDependencyValidationException =
                await Assert.ThrowsAsync<ApprovalSettingDependencyValidationException>(
                    removeApprovalSettingByIdTask.AsTask);

            // then
            actualApprovalSettingDependencyValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingDependencyValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(someApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalSettingAsync(
                    someApprovalSetting,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingDependencyValidationException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            Guid someApprovalSettingId = Guid.NewGuid();
            var serviceException = new Exception();

            var failedApprovalSettingServiceException = new FailedApprovalSettingServiceException(
                message: "Failed approval setting service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalSettingServiceException = new ApprovalSettingServiceException(
                message: "Approval setting service error occurred, contact support.",
                innerException: failedApprovalSettingServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalSetting> removeApprovalSettingByIdTask =
                this.approvalSettingService.RemoveApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalSettingServiceException actualApprovalSettingServiceException =
                await Assert.ThrowsAsync<ApprovalSettingServiceException>(
                    removeApprovalSettingByIdTask.AsTask);

            // then
            actualApprovalSettingServiceException.Should().BeEquivalentTo(
                expectedApprovalSettingServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    someApprovalSettingId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
