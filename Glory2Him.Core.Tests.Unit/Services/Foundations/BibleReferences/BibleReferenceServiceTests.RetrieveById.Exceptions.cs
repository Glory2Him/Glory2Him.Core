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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.BibleReferences.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.BibleReferences
{
    public partial class BibleReferenceServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveByIdIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            Guid someBibleReferenceId = Guid.NewGuid();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutBibleReferenceException =
                new TimeoutBibleReferenceException(
                    message: "Failed bible reference timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedBibleReferenceDependencyException = new BibleReferenceDependencyException(
                message: "Bible reference dependency error occurred, contact support.",
                innerException: timeoutBibleReferenceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<BibleReference> retrieveBibleReferenceByIdTask =
                this.bibleReferenceService.RetrieveBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    TestContext.Current.CancellationToken);

            BibleReferenceDependencyException actualBibleReferenceDependencyException =
                await Assert.ThrowsAsync<BibleReferenceDependencyException>(
                    retrieveBibleReferenceByIdTask.AsTask);

            // then
            actualBibleReferenceDependencyException.Should().BeEquivalentTo(
                expectedBibleReferenceDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnRetrieveByIdIfSqlErrorOccursAndLogItAsync()
        {
            // given
            Guid someBibleReferenceId = Guid.NewGuid();
            SqlException sqlException = GetSqlException();

            var failedStorageBibleReferenceException = new FailedStorageBibleReferenceException(
                message: "Failed bible reference storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedBibleReferenceDependencyException = new BibleReferenceDependencyException(
                message: "Bible reference dependency error occurred, contact support.",
                innerException: failedStorageBibleReferenceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<BibleReference> retrieveBibleReferenceByIdTask =
                this.bibleReferenceService.RetrieveBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    TestContext.Current.CancellationToken);

            BibleReferenceDependencyException actualBibleReferenceDependencyException =
                await Assert.ThrowsAsync<BibleReferenceDependencyException>(
                    retrieveBibleReferenceByIdTask.AsTask);

            // then
            actualBibleReferenceDependencyException.Should().BeEquivalentTo(
                expectedBibleReferenceDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRetrieveByIdIfCancellationRequestedAsync()
        {
            // given
            Guid someBibleReferenceId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<BibleReference> retrieveBibleReferenceByIdTask =
                this.bibleReferenceService.RetrieveBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                retrieveBibleReferenceByIdTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrieveByIdIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Guid someBibleReferenceId = Guid.NewGuid();
            var serviceException = new Exception();

            var failedBibleReferenceServiceException = new FailedBibleReferenceServiceException(
                message: "Failed bible reference service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedBibleReferenceServiceException = new BibleReferenceServiceException(
                message: "Bible reference service error occurred, contact support.",
                innerException: failedBibleReferenceServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<BibleReference> retrieveBibleReferenceByIdTask =
                this.bibleReferenceService.RetrieveBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    TestContext.Current.CancellationToken);

            BibleReferenceServiceException actualBibleReferenceServiceException =
                await Assert.ThrowsAsync<BibleReferenceServiceException>(
                    retrieveBibleReferenceByIdTask.AsTask);

            // then
            actualBibleReferenceServiceException.Should().BeEquivalentTo(
                expectedBibleReferenceServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
