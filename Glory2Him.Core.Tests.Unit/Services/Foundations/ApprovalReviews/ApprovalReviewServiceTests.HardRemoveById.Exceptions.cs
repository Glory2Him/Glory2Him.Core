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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Glory2Him.Core.Models.Securities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviews
{
    public partial class ApprovalReviewServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnHardRemoveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someApprovalReviewId = Guid.NewGuid();

            var expectedApprovalReviewDependencyException = new ApprovalReviewDependencyException(
                message: "Approval review dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalReview> hardRemoveApprovalReviewByIdTask =
                this.approvalReviewService.HardRemoveApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewDependencyException actualApprovalReviewDependencyException =
                await Assert.ThrowsAsync<ApprovalReviewDependencyException>(
                    hardRemoveApprovalReviewByIdTask.AsTask);

            // then
            actualApprovalReviewDependencyException.Should().BeEquivalentTo(
                expectedApprovalReviewDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken),
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
        public async Task ShouldThrowDependencyExceptionOnHardRemoveByIdIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someApprovalReviewId = Guid.NewGuid();
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
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ApprovalReview> hardRemoveApprovalReviewByIdTask =
                this.approvalReviewService.HardRemoveApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewDependencyException actualApprovalReviewDependencyException =
                await Assert.ThrowsAsync<ApprovalReviewDependencyException>(
                    hardRemoveApprovalReviewByIdTask.AsTask);

            // then
            actualApprovalReviewDependencyException.Should().BeEquivalentTo(
                expectedApprovalReviewDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken),
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
        public async Task ShouldThrowOperationCanceledExceptionOnHardRemoveByIdIfCancellationRequestedAsync()
        {
            // given
            Guid someApprovalReviewId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ApprovalReview> hardRemoveApprovalReviewByIdTask =
                this.approvalReviewService.HardRemoveApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                hardRemoveApprovalReviewByIdTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnHardRemoveByIdIfSqlErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someApprovalReviewId = Guid.NewGuid();
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalReviewException = new FailedStorageApprovalReviewException(
                message: "Failed approval review storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalReviewDependencyException = new ApprovalReviewDependencyException(
                message: "Approval review dependency error occurred, contact support.",
                innerException: failedStorageApprovalReviewException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<ApprovalReview> hardRemoveApprovalReviewByIdTask =
                this.approvalReviewService.HardRemoveApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewDependencyException actualApprovalReviewDependencyException =
                await Assert.ThrowsAsync<ApprovalReviewDependencyException>(
                    hardRemoveApprovalReviewByIdTask.AsTask);

            // then
            actualApprovalReviewDependencyException.Should().BeEquivalentTo(
                expectedApprovalReviewDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken),
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
        public async Task ShouldThrowDependencyValidationExceptionOnHardRemoveByIdIfDbUpdateConcurrencyExceptionOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someApprovalReviewId = Guid.NewGuid();
            ApprovalReview someApprovalReview = CreateRandomApprovalReview();
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();

            var lockedApprovalReviewException = new LockedApprovalReviewException(
                message: "Locked approval review record, please try again later.",
                innerException: dbUpdateConcurrencyException,
                data: dbUpdateConcurrencyException.Data);

            var expectedApprovalReviewDependencyValidationException = new ApprovalReviewDependencyValidationException(
                message: "Approval review dependency validation error occurred, fix the errors and try again.",
                innerException: lockedApprovalReviewException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(someApprovalReview);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteApprovalReviewAsync(
                    someApprovalReview,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(dbUpdateConcurrencyException);

            // when
            ValueTask<ApprovalReview> hardRemoveApprovalReviewByIdTask =
                this.approvalReviewService.HardRemoveApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewDependencyValidationException actualApprovalReviewDependencyValidationException =
                await Assert.ThrowsAsync<ApprovalReviewDependencyValidationException>(
                    hardRemoveApprovalReviewByIdTask.AsTask);

            // then
            actualApprovalReviewDependencyValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewDependencyValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteApprovalReviewAsync(
                    someApprovalReview,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnHardRemoveByIdIfServiceErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someApprovalReviewId = Guid.NewGuid();
            var serviceException = new Exception();

            var failedApprovalReviewServiceException = new FailedApprovalReviewServiceException(
                message: "Failed approval review service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalReviewServiceException = new ApprovalReviewServiceException(
                message: "Approval review service error occurred, contact support.",
                innerException: failedApprovalReviewServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalReview> hardRemoveApprovalReviewByIdTask =
                this.approvalReviewService.HardRemoveApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewServiceException actualApprovalReviewServiceException =
                await Assert.ThrowsAsync<ApprovalReviewServiceException>(
                    hardRemoveApprovalReviewByIdTask.AsTask);

            // then
            actualApprovalReviewServiceException.Should().BeEquivalentTo(
                expectedApprovalReviewServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken),
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
