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
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalComments
{
    public partial class ApprovalCommentServiceTests
    {
        // Resolve loads the row FIRST, so SelectApprovalCommentByIdAsync is the first broker call
        // the operation reaches; the exception-wrapping guarantees are exercised by making it
        // throw. These tests are per-operation on purpose: the TryCatch categorization is applied
        // afresh to ResolveApprovalCommentAsync, and its caller-cancellation guard is code local
        // to the method.

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnResolveIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            Guid someApprovalCommentId = Guid.NewGuid();

            var expectedApprovalCommentDependencyException = new ApprovalCommentDependencyException(
                message: "Approval comment dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalComment> resolveApprovalCommentTask =
                this.approvalCommentService.ResolveApprovalCommentAsync(
                    someApprovalCommentId,
                    isResolved: true,
                    TestContext.Current.CancellationToken);

            ApprovalCommentDependencyException actualApprovalCommentDependencyException =
                await Assert.ThrowsAsync<ApprovalCommentDependencyException>(
                    resolveApprovalCommentTask.AsTask);

            // then
            actualApprovalCommentDependencyException.Should().BeEquivalentTo(
                expectedApprovalCommentDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentDependencyException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnResolveIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            Guid someApprovalCommentId = Guid.NewGuid();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutApprovalCommentException =
                new TimeoutApprovalCommentException(
                    message: "Failed approval comment timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedApprovalCommentDependencyException = new ApprovalCommentDependencyException(
                message: "Approval comment dependency error occurred, contact support.",
                innerException: timeoutApprovalCommentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ApprovalComment> resolveApprovalCommentTask =
                this.approvalCommentService.ResolveApprovalCommentAsync(
                    someApprovalCommentId,
                    isResolved: true,
                    TestContext.Current.CancellationToken);

            ApprovalCommentDependencyException actualApprovalCommentDependencyException =
                await Assert.ThrowsAsync<ApprovalCommentDependencyException>(
                    resolveApprovalCommentTask.AsTask);

            // then
            actualApprovalCommentDependencyException.Should().BeEquivalentTo(
                expectedApprovalCommentDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentDependencyException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnResolveIfCancellationRequestedAsync()
        {
            // given: a caller-cancelled token must short-circuit the operation BEFORE any work —
            // this guards the cancellationToken.ThrowIfCancellationRequested() line that is local
            // to ResolveApprovalCommentAsync.
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ApprovalComment> resolveApprovalCommentTask =
                this.approvalCommentService.ResolveApprovalCommentAsync(
                    Guid.NewGuid(),
                    isResolved: true,
                    cancellationToken);

            // then: no row is even read
            await Assert.ThrowsAsync<OperationCanceledException>(
                resolveApprovalCommentTask.AsTask);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
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
        public async Task ShouldThrowCriticalDependencyExceptionOnResolveIfSqlErrorOccursAndLogItAsync()
        {
            // given
            Guid someApprovalCommentId = Guid.NewGuid();
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalCommentException = new FailedStorageApprovalCommentException(
                message: "Failed approval comment storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalCommentDependencyException = new ApprovalCommentDependencyException(
                message: "Approval comment dependency error occurred, contact support.",
                innerException: failedStorageApprovalCommentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<ApprovalComment> resolveApprovalCommentTask =
                this.approvalCommentService.ResolveApprovalCommentAsync(
                    someApprovalCommentId,
                    isResolved: true,
                    TestContext.Current.CancellationToken);

            ApprovalCommentDependencyException actualApprovalCommentDependencyException =
                await Assert.ThrowsAsync<ApprovalCommentDependencyException>(
                    resolveApprovalCommentTask.AsTask);

            // then
            actualApprovalCommentDependencyException.Should().BeEquivalentTo(
                expectedApprovalCommentDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentDependencyException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ModifyDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnResolveIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            Guid someApprovalCommentId = Guid.NewGuid();

            var expectedApprovalCommentDependencyValidationException =
                new ApprovalCommentDependencyValidationException(
                    message: "Approval comment dependency validation error occurred, fix the errors and try again.",
                    innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalComment> resolveApprovalCommentTask =
                this.approvalCommentService.ResolveApprovalCommentAsync(
                    someApprovalCommentId,
                    isResolved: true,
                    TestContext.Current.CancellationToken);

            ApprovalCommentDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalCommentDependencyValidationException>(
                    resolveApprovalCommentTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(
                expectedApprovalCommentDependencyValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentDependencyValidationException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnResolveIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Guid someApprovalCommentId = Guid.NewGuid();
            var serviceException = new Exception();

            var failedApprovalCommentServiceException = new FailedApprovalCommentServiceException(
                message: "Failed approval comment service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalCommentServiceException = new ApprovalCommentServiceException(
                message: "Approval comment service error occurred, contact support.",
                innerException: failedApprovalCommentServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalComment> resolveApprovalCommentTask =
                this.approvalCommentService.ResolveApprovalCommentAsync(
                    someApprovalCommentId,
                    isResolved: true,
                    TestContext.Current.CancellationToken);

            ApprovalCommentServiceException actualApprovalCommentServiceException =
                await Assert.ThrowsAsync<ApprovalCommentServiceException>(
                    resolveApprovalCommentTask.AsTask);

            // then
            actualApprovalCommentServiceException.Should().BeEquivalentTo(
                expectedApprovalCommentServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentServiceException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
