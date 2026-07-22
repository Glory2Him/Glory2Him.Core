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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviews
{
    public partial class ApprovalReviewServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveAllIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutApprovalReviewException =
                new TimeoutApprovalReviewException(
                    message: "Failed approval review timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedApprovalReviewDependencyException = new ApprovalReviewDependencyException(
                message: "Approval review dependency error occurred, contact support.",
                innerException: timeoutApprovalReviewException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalReviewsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<IQueryable<ApprovalReview>> retrieveAllApprovalReviewsTask =
                this.approvalReviewService.RetrieveAllApprovalReviewsAsync(
                    TestContext.Current.CancellationToken);

            ApprovalReviewDependencyException actualApprovalReviewDependencyException =
                await Assert.ThrowsAsync<ApprovalReviewDependencyException>(
                    retrieveAllApprovalReviewsTask.AsTask);

            // then
            actualApprovalReviewDependencyException.Should().BeEquivalentTo(
                expectedApprovalReviewDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalReviewsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewDependencyException))),
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
            ValueTask<IQueryable<ApprovalReview>> retrieveAllApprovalReviewsTask =
                this.approvalReviewService.RetrieveAllApprovalReviewsAsync(cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                retrieveAllApprovalReviewsTask.AsTask);

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

            var failedStorageApprovalReviewException = new FailedStorageApprovalReviewException(
                message: "Failed approval review storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalReviewDependencyException = new ApprovalReviewDependencyException(
                message: "Approval review dependency error occurred, contact support.",
                innerException: failedStorageApprovalReviewException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalReviewsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<IQueryable<ApprovalReview>> retrieveAllApprovalReviewsTask =
                this.approvalReviewService.RetrieveAllApprovalReviewsAsync(
                    TestContext.Current.CancellationToken);

            ApprovalReviewDependencyException actualApprovalReviewDependencyException =
                await Assert.ThrowsAsync<ApprovalReviewDependencyException>(
                    retrieveAllApprovalReviewsTask.AsTask);

            // then
            actualApprovalReviewDependencyException.Should().BeEquivalentTo(
                expectedApprovalReviewDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalReviewsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewDependencyException))),
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

            var failedApprovalReviewServiceException = new FailedApprovalReviewServiceException(
                message: "Failed approval review service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalReviewServiceException = new ApprovalReviewServiceException(
                message: "Approval review service error occurred, contact support.",
                innerException: failedApprovalReviewServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalReviewsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<IQueryable<ApprovalReview>> retrieveAllApprovalReviewsTask =
                this.approvalReviewService.RetrieveAllApprovalReviewsAsync(
                    TestContext.Current.CancellationToken);

            ApprovalReviewServiceException actualApprovalReviewServiceException =
                await Assert.ThrowsAsync<ApprovalReviewServiceException>(
                    retrieveAllApprovalReviewsTask.AsTask);

            // then
            actualApprovalReviewServiceException.Should().BeEquivalentTo(
                expectedApprovalReviewServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalReviewsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
