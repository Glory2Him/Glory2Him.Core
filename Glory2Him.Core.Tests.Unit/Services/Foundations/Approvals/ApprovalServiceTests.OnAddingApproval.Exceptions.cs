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
using Glory2Him.Core.Models.Configurations;
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
        public async Task ShouldThrowOperationCanceledExceptionOnAddingApprovalEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<Approval> requestEnvelope = CreateRandomApprovalRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<Approval>?> onAddingTask =
                this.approvalService.OnAddingApprovalAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onAddingTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddingApprovalEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<Approval> requestEnvelope = CreateRandomApprovalRequestEnvelope();

            var expectedApprovalDependencyException = new ApprovalDependencyException(
                message: "Approval dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalOnAddingApprovalSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<Approval>?> onAddingTask =
                this.approvalService.OnAddingApprovalAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalDependencyException actualApprovalDependencyException =
                await Assert.ThrowsAsync<ApprovalDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalDependencyException.Should().BeEquivalentTo(
                expectedApprovalDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddingApprovalEventIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Approval> requestEnvelope = CreateRandomApprovalRequestEnvelope();
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
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalOnAddingApprovalSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<Approval>?> onAddingTask =
                this.approvalService.OnAddingApprovalAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalDependencyException actualApprovalDependencyException =
                await Assert.ThrowsAsync<ApprovalDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalDependencyException.Should().BeEquivalentTo(
                expectedApprovalDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalOnAddingApprovalSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnAddingApprovalEventIfSqlErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Approval> requestEnvelope = CreateRandomApprovalRequestEnvelope();
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalException = new FailedStorageApprovalException(
                message: "Failed approval storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalDependencyException = new ApprovalDependencyException(
                message: "Approval dependency error occurred, contact support.",
                innerException: failedStorageApprovalException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalOnAddingApprovalSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<EventEnvelope<Approval>?> onAddingTask =
                this.approvalService.OnAddingApprovalAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalDependencyException actualApprovalDependencyException =
                await Assert.ThrowsAsync<ApprovalDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalDependencyException.Should().BeEquivalentTo(
                expectedApprovalDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddingApprovalEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<Approval> requestEnvelope = CreateRandomApprovalRequestEnvelope();

            var expectedApprovalDependencyValidationException = new ApprovalDependencyValidationException(
                message: "Approval dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalOnAddingApprovalSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<Approval>?> onAddingTask =
                this.approvalService.OnAddingApprovalAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalDependencyValidationException actualApprovalDependencyValidationException =
                await Assert.ThrowsAsync<ApprovalDependencyValidationException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalDependencyValidationException.Should().BeEquivalentTo(
                expectedApprovalDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnAddingApprovalEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Approval> requestEnvelope = CreateRandomApprovalRequestEnvelope();
            var serviceException = new Exception();

            var failedApprovalServiceException = new FailedApprovalServiceException(
                message: "Failed approval service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalServiceException = new ApprovalServiceException(
                message: "Approval service error occurred, contact support.",
                innerException: failedApprovalServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalOnAddingApprovalSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<Approval>?> onAddingTask =
                this.approvalService.OnAddingApprovalAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalServiceException actualApprovalServiceException =
                await Assert.ThrowsAsync<ApprovalServiceException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalServiceException.Should().BeEquivalentTo(
                expectedApprovalServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
