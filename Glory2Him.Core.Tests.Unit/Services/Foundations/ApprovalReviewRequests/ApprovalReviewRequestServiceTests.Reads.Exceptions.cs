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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Securities;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviewRequests
{
    /// <summary>
    /// The two read paths' failure taxonomy. <c>RetrieveAll</c> matters disproportionately here:
    /// it is the only caller of the <c>IQueryable</c> <c>TryCatch</c> overload, so without these
    /// tests that whole overload — a third of the exceptions partial — has no coverage at all.
    /// </summary>
    public partial class ApprovalReviewRequestServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveAllIfOperationCanceledOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            var operationCanceledException = new OperationCanceledException();
            var timeoutException = new TimeoutException("The dependency operation timed out.");

            var timeoutApprovalReviewRequestException =
                new TimeoutApprovalReviewRequestException(
                    message: "Failed approval review request timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedApprovalReviewRequestDependencyException =
                new ApprovalReviewRequestDependencyException(
                    message: "Approval review request dependency error occurred, contact support.",
                    innerException: timeoutApprovalReviewRequestException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalReviewRequestsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<IQueryable<ApprovalReviewRequest>> retrieveAllTask =
                this.approvalReviewRequestService.RetrieveAllApprovalReviewRequestsAsync(
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestDependencyException>(
                    retrieveAllTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestDependencyException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnRetrieveAllIfSqlErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalReviewRequestException =
                new FailedStorageApprovalReviewRequestException(
                    message: "Failed approval review request storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

            var expectedApprovalReviewRequestDependencyException =
                new ApprovalReviewRequestDependencyException(
                    message: "Approval review request dependency error occurred, contact support.",
                    innerException: failedStorageApprovalReviewRequestException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalReviewRequestsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<IQueryable<ApprovalReviewRequest>> retrieveAllTask =
                this.approvalReviewRequestService.RetrieveAllApprovalReviewRequestsAsync(
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestDependencyException>(
                    retrieveAllTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestDependencyException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrieveAllIfServiceErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            var serviceException = new Exception();

            var failedApprovalReviewRequestServiceException =
                new FailedApprovalReviewRequestServiceException(
                    message: "Failed approval review request service error occurred, " +
                        "please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedApprovalReviewRequestServiceException =
                new ApprovalReviewRequestServiceException(
                    message: "Approval review request service error occurred, contact support.",
                    innerException: failedApprovalReviewRequestServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalReviewRequestsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<IQueryable<ApprovalReviewRequest>> retrieveAllTask =
                this.approvalReviewRequestService.RetrieveAllApprovalReviewRequestsAsync(
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestServiceException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestServiceException>(
                    retrieveAllTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestServiceException))),
                Times.Once);
        }

        /// <summary>
        /// A GENUINE cancellation — the caller's token — passes straight through rather than
        /// being categorized as a timeout. The distinction is the `when` clause on the first
        /// catch: a dependency that gave up looks identical to a caller who walked away unless
        /// the token is consulted, and reporting an abandoned request as a storage fault would
        /// page somebody over nothing.
        /// </summary>
        [Fact]
        public async Task ShouldRethrowOnRetrieveAllIfCancellationIsRequestedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<IQueryable<ApprovalReviewRequest>> retrieveAllTask =
                this.approvalReviewRequestService.RetrieveAllApprovalReviewRequestsAsync(
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAnyAsync<OperationCanceledException>(retrieveAllTask.AsTask);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalReviewRequestsAsync(It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRethrowOnRetrieveByIdIfCancellationIsRequestedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<ApprovalReviewRequest> retrieveTask =
                this.approvalReviewRequestService.RetrieveApprovalReviewRequestByIdAsync(
                    Guid.NewGuid(),
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAnyAsync<OperationCanceledException>(retrieveTask.AsTask);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRetrieveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeptions.Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid someApprovalReviewRequestId = Guid.NewGuid();

            var expectedApprovalReviewRequestDependencyException =
                new ApprovalReviewRequestDependencyException(
                    message: "Approval review request dependency error occurred, contact support.",
                    innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalReviewRequest> retrieveTask =
                this.approvalReviewRequestService.RetrieveApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestDependencyException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestDependencyException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrieveByIdIfServiceErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid someApprovalReviewRequestId = Guid.NewGuid();
            var serviceException = new Exception();

            var failedApprovalReviewRequestServiceException =
                new FailedApprovalReviewRequestServiceException(
                    message: "Failed approval review request service error occurred, " +
                        "please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedApprovalReviewRequestServiceException =
                new ApprovalReviewRequestServiceException(
                    message: "Approval review request service error occurred, contact support.",
                    innerException: failedApprovalReviewRequestServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalReviewRequest> retrieveTask =
                this.approvalReviewRequestService.RetrieveApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestServiceException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestServiceException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestServiceException))),
                Times.Once);
        }
    }
}
