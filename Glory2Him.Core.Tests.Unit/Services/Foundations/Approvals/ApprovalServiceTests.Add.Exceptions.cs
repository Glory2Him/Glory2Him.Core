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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Approvals
{
    public partial class ApprovalServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            Approval someApproval = CreateRandomApproval();
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

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApproval, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<Approval> addApprovalTask =
                this.approvalService.AddApprovalAsync(
                    someApproval,
                    TestContext.Current.CancellationToken);

            ApprovalDependencyException actualApprovalDependencyException =
                await Assert.ThrowsAsync<ApprovalDependencyException>(
                    addApprovalTask.AsTask);

            // then
            actualApprovalDependencyException.Should().BeEquivalentTo(
                expectedApprovalDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApproval, It.IsAny<SecurityContext>()),
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

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            Approval someApproval = CreateRandomApproval();

            var expectedApprovalDependencyException = new ApprovalDependencyException(
                message: "Approval dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApproval, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<Approval> addApprovalTask =
                this.approvalService.AddApprovalAsync(
                    someApproval,
                    TestContext.Current.CancellationToken);

            ApprovalDependencyException actualApprovalDependencyException =
                await Assert.ThrowsAsync<ApprovalDependencyException>(
                    addApprovalTask.AsTask);

            // then
            actualApprovalDependencyException.Should().BeEquivalentTo(
                expectedApprovalDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApproval, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowOperationCanceledExceptionOnAddIfCancellationRequestedAsync()
        {
            // given
            Approval someApproval = CreateRandomApproval();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<Approval> addApprovalTask =
                this.approvalService.AddApprovalAsync(
                    someApproval,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                addApprovalTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnAddIfSqlErrorOccursAndLogItAsync()
        {
            // given
            Approval someApproval = CreateRandomApproval();
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalException = new FailedStorageApprovalException(
                message: "Failed approval storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalDependencyException = new ApprovalDependencyException(
                message: "Approval dependency error occurred, contact support.",
                innerException: failedStorageApprovalException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApproval, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<Approval> addApprovalTask =
                this.approvalService.AddApprovalAsync(
                    someApproval,
                    TestContext.Current.CancellationToken);

            ApprovalDependencyException actualApprovalDependencyException =
                await Assert.ThrowsAsync<ApprovalDependencyException>(
                    addApprovalTask.AsTask);

            // then
            actualApprovalDependencyException.Should().BeEquivalentTo(
                expectedApprovalDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApproval, It.IsAny<SecurityContext>()),
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

        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            Approval someApproval = CreateRandomApproval();

            var expectedApprovalDependencyValidationException = new ApprovalDependencyValidationException(
                message: "Approval dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApproval, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<Approval> addApprovalTask =
                this.approvalService.AddApprovalAsync(
                    someApproval,
                    TestContext.Current.CancellationToken);

            ApprovalDependencyValidationException actualApprovalDependencyValidationException =
                await Assert.ThrowsAsync<ApprovalDependencyValidationException>(
                    addApprovalTask.AsTask);

            // then
            actualApprovalDependencyValidationException.Should().BeEquivalentTo(
                expectedApprovalDependencyValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApproval, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnAddIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Approval someApproval = CreateRandomApproval();
            var serviceException = new Exception();

            var failedApprovalServiceException = new FailedApprovalServiceException(
                message: "Failed approval service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalServiceException = new ApprovalServiceException(
                message: "Approval service error occurred, contact support.",
                innerException: failedApprovalServiceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApproval, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<Approval> addApprovalTask =
                this.approvalService.AddApprovalAsync(
                    someApproval,
                    TestContext.Current.CancellationToken);

            ApprovalServiceException actualApprovalServiceException =
                await Assert.ThrowsAsync<ApprovalServiceException>(
                    addApprovalTask.AsTask);

            // then
            actualApprovalServiceException.Should().BeEquivalentTo(
                expectedApprovalServiceException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApproval, It.IsAny<SecurityContext>()),
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
