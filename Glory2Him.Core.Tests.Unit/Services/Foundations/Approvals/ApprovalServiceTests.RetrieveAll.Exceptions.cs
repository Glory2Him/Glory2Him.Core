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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Approvals
{
    public partial class ApprovalServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveAllIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutApprovalException =
                new TimeoutApprovalException(
                    message: "Failed approval timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedApprovalDependencyException = new ApprovalDependencyException(
                message: "Approval dependency error occurred, contact support.",
                innerException: timeoutApprovalException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<IQueryable<Approval>> retrieveAllApprovalsTask =
                this.approvalService.RetrieveAllApprovalsAsync(
                    TestContext.Current.CancellationToken);

            ApprovalDependencyException actualApprovalDependencyException =
                await Assert.ThrowsAsync<ApprovalDependencyException>(
                    retrieveAllApprovalsTask.AsTask);

            // then
            actualApprovalDependencyException.Should().BeEquivalentTo(
                expectedApprovalDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalDependencyException))),
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
            ValueTask<IQueryable<Approval>> retrieveAllApprovalsTask =
                this.approvalService.RetrieveAllApprovalsAsync(cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                retrieveAllApprovalsTask.AsTask);

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

            var failedStorageApprovalException = new FailedStorageApprovalException(
                message: "Failed approval storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalDependencyException = new ApprovalDependencyException(
                message: "Approval dependency error occurred, contact support.",
                innerException: failedStorageApprovalException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<IQueryable<Approval>> retrieveAllApprovalsTask =
                this.approvalService.RetrieveAllApprovalsAsync(
                    TestContext.Current.CancellationToken);

            ApprovalDependencyException actualApprovalDependencyException =
                await Assert.ThrowsAsync<ApprovalDependencyException>(
                    retrieveAllApprovalsTask.AsTask);

            // then
            actualApprovalDependencyException.Should().BeEquivalentTo(
                expectedApprovalDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalDependencyException))),
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

            var failedApprovalServiceException = new FailedApprovalServiceException(
                message: "Failed approval service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalServiceException = new ApprovalServiceException(
                message: "Approval service error occurred, contact support.",
                innerException: failedApprovalServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<IQueryable<Approval>> retrieveAllApprovalsTask =
                this.approvalService.RetrieveAllApprovalsAsync(
                    TestContext.Current.CancellationToken);

            ApprovalServiceException actualApprovalServiceException =
                await Assert.ThrowsAsync<ApprovalServiceException>(
                    retrieveAllApprovalsTask.AsTask);

            // then
            actualApprovalServiceException.Should().BeEquivalentTo(
                expectedApprovalServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
