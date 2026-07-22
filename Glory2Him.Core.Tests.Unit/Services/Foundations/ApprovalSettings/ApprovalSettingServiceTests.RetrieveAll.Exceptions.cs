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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    public partial class ApprovalSettingServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveAllIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
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
                broker.SelectAllApprovalSettingsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<IQueryable<ApprovalSetting>> retrieveAllApprovalSettingsTask =
                this.approvalSettingService.RetrieveAllApprovalSettingsAsync(
                    TestContext.Current.CancellationToken);

            ApprovalSettingDependencyException actualApprovalSettingDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingDependencyException>(
                    retrieveAllApprovalSettingsTask.AsTask);

            // then
            actualApprovalSettingDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingsAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldThrowOperationCanceledExceptionOnRetrieveAllIfCancellationRequestedAsync()
        {
            // given
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<IQueryable<ApprovalSetting>> retrieveAllApprovalSettingsTask =
                this.approvalSettingService.RetrieveAllApprovalSettingsAsync(cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                retrieveAllApprovalSettingsTask.AsTask);

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

            var failedStorageApprovalSettingException = new FailedStorageApprovalSettingException(
                message: "Failed approval setting storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalSettingDependencyException = new ApprovalSettingDependencyException(
                message: "Approval setting dependency error occurred, contact support.",
                innerException: failedStorageApprovalSettingException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalSettingsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<IQueryable<ApprovalSetting>> retrieveAllApprovalSettingsTask =
                this.approvalSettingService.RetrieveAllApprovalSettingsAsync(
                    TestContext.Current.CancellationToken);

            ApprovalSettingDependencyException actualApprovalSettingDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingDependencyException>(
                    retrieveAllApprovalSettingsTask.AsTask);

            // then
            actualApprovalSettingDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingsAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldThrowServiceExceptionOnRetrieveAllIfServiceErrorOccursAndLogItAsync()
        {
            // given
            var serviceException = new Exception();

            var failedApprovalSettingServiceException = new FailedApprovalSettingServiceException(
                message: "Failed approval setting service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalSettingServiceException = new ApprovalSettingServiceException(
                message: "Approval setting service error occurred, contact support.",
                innerException: failedApprovalSettingServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalSettingsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<IQueryable<ApprovalSetting>> retrieveAllApprovalSettingsTask =
                this.approvalSettingService.RetrieveAllApprovalSettingsAsync(
                    TestContext.Current.CancellationToken);

            ApprovalSettingServiceException actualApprovalSettingServiceException =
                await Assert.ThrowsAsync<ApprovalSettingServiceException>(
                    retrieveAllApprovalSettingsTask.AsTask);

            // then
            actualApprovalSettingServiceException.Should().BeEquivalentTo(
                expectedApprovalSettingServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingsAsync(It.IsAny<CancellationToken>()),
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
