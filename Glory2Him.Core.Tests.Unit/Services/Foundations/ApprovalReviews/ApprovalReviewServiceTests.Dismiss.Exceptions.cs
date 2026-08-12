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
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviews
{
    public partial class ApprovalReviewServiceTests
    {
        // Dismiss loads the row FIRST, so SelectApprovalReviewByIdAsync is the first broker call
        // the operation reaches; the exception-wrapping guarantees are exercised by making it
        // throw. These tests are per-operation on purpose: the TryCatch categorization is
        // applied afresh to DismissApprovalReviewAsync, and its caller-cancellation guard is
        // code local to the method.

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnDismissIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            Guid someApprovalReviewId = Guid.NewGuid();

            var expectedApprovalReviewDependencyException = new ApprovalReviewDependencyException(
                message: "Approval review dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalReview> dismissApprovalReviewTask =
                this.approvalReviewService.DismissApprovalReviewAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewDependencyException actualApprovalReviewDependencyException =
                await Assert.ThrowsAsync<ApprovalReviewDependencyException>(
                    dismissApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewDependencyException.Should().BeEquivalentTo(
                expectedApprovalReviewDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewDependencyException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnDismissIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
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
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ApprovalReview> dismissApprovalReviewTask =
                this.approvalReviewService.DismissApprovalReviewAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewDependencyException actualApprovalReviewDependencyException =
                await Assert.ThrowsAsync<ApprovalReviewDependencyException>(
                    dismissApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewDependencyException.Should().BeEquivalentTo(
                expectedApprovalReviewDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewDependencyException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnDismissIfCancellationRequestedAsync()
        {
            // given: a caller-cancelled token must short-circuit the operation BEFORE any work —
            // this guards the cancellationToken.ThrowIfCancellationRequested() line that is local
            // to DismissApprovalReviewAsync.
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ApprovalReview> dismissApprovalReviewTask =
                this.approvalReviewService.DismissApprovalReviewAsync(
                    Guid.NewGuid(),
                    cancellationToken);

            // then: no row is even read
            await Assert.ThrowsAsync<OperationCanceledException>(
                dismissApprovalReviewTask.AsTask);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnDismissIfSqlErrorOccursAndLogItAsync()
        {
            // given
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
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<ApprovalReview> dismissApprovalReviewTask =
                this.approvalReviewService.DismissApprovalReviewAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewDependencyException actualApprovalReviewDependencyException =
                await Assert.ThrowsAsync<ApprovalReviewDependencyException>(
                    dismissApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewDependencyException.Should().BeEquivalentTo(
                expectedApprovalReviewDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewDependencyException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ModifyDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnDismissIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            Guid someApprovalReviewId = Guid.NewGuid();

            var expectedApprovalReviewDependencyValidationException =
                new ApprovalReviewDependencyValidationException(
                    message: "Approval review dependency validation error occurred, fix the errors and try again.",
                    innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalReview> dismissApprovalReviewTask =
                this.approvalReviewService.DismissApprovalReviewAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewDependencyValidationException actualApprovalReviewDependencyValidationException =
                await Assert.ThrowsAsync<ApprovalReviewDependencyValidationException>(
                    dismissApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewDependencyValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewDependencyValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewDependencyValidationException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnDismissIfServiceErrorOccursAndLogItAsync()
        {
            // given
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
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalReview> dismissApprovalReviewTask =
                this.approvalReviewService.DismissApprovalReviewAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewServiceException actualApprovalReviewServiceException =
                await Assert.ThrowsAsync<ApprovalReviewServiceException>(
                    dismissApprovalReviewTask.AsTask);

            // then
            actualApprovalReviewServiceException.Should().BeEquivalentTo(
                expectedApprovalReviewServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewServiceException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
