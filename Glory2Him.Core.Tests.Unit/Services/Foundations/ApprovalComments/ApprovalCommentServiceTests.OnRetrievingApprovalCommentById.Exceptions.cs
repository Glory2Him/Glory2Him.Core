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
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalComments
{
    public partial class ApprovalCommentServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRetrievingApprovalCommentByIdEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<ApprovalComment> requestEnvelope = CreateRandomApprovalCommentRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<ApprovalComment>?> onRetrievingTask =
                this.approvalCommentService.OnRetrievingApprovalCommentByIdAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onRetrievingTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrievingApprovalCommentByIdEventIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ApprovalComment> requestEnvelope = CreateRandomApprovalCommentRequestEnvelope();
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
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<ApprovalComment>?> onRetrievingTask =
                this.approvalCommentService.OnRetrievingApprovalCommentByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalCommentDependencyException actualApprovalCommentDependencyException =
                await Assert.ThrowsAsync<ApprovalCommentDependencyException>(
                    onRetrievingTask.AsTask);

            // then: the nested retrieve categorizes the timeout and logs it exactly once —
            // the substrate wrapper must not double-wrap or re-log it.
            actualApprovalCommentDependencyException.Should().BeEquivalentTo(
                expectedApprovalCommentDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPassThroughDependencyExceptionOnRetrievingApprovalCommentByIdEventAsync()
        {
            // given
            EventEnvelope<ApprovalComment> requestEnvelope = CreateRandomApprovalCommentRequestEnvelope();
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
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<EventEnvelope<ApprovalComment>?> onRetrievingTask =
                this.approvalCommentService.OnRetrievingApprovalCommentByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalCommentDependencyException actualApprovalCommentDependencyException =
                await Assert.ThrowsAsync<ApprovalCommentDependencyException>(
                    onRetrievingTask.AsTask);

            // then: the nested retrieve's categorized exception surfaces unwrapped and is
            // logged exactly once — the substrate wrapper must not double-wrap or re-log it.
            actualApprovalCommentDependencyException.Should().BeEquivalentTo(
                expectedApprovalCommentDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrievingApprovalCommentByIdEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            var serviceException = new Exception();

            var requestEnvelope = new EventEnvelope<ApprovalComment>
            {
                Content = new ApprovalComment { Id = storageApprovalComment.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var failedApprovalCommentServiceException = new FailedApprovalCommentServiceException(
                message: "Failed approval comment service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalCommentServiceException = new ApprovalCommentServiceException(
                message: "Approval comment service error occurred, contact support.",
                innerException: failedApprovalCommentServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            this.eventEnvelopeFactoryMock.Setup(factory =>
                factory.CreateNextAsync(requestEnvelope, storageApprovalComment))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<ApprovalComment>?> onRetrievingTask =
                this.approvalCommentService.OnRetrievingApprovalCommentByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalCommentServiceException actualApprovalCommentServiceException =
                await Assert.ThrowsAsync<ApprovalCommentServiceException>(
                    onRetrievingTask.AsTask);

            // then
            actualApprovalCommentServiceException.Should().BeEquivalentTo(
                expectedApprovalCommentServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
