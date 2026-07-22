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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.BibleReferences.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.BibleReferences
{
    public partial class BibleReferenceServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnModifyIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            BibleReference someBibleReference = CreateRandomBibleReference();

            var expectedBibleReferenceDependencyException = new BibleReferenceDependencyException(
                message: "Bible reference dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someBibleReference, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<BibleReference> modifyBibleReferenceTask =
                this.bibleReferenceService.ModifyBibleReferenceAsync(
                    someBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceDependencyException actualBibleReferenceDependencyException =
                await Assert.ThrowsAsync<BibleReferenceDependencyException>(
                    modifyBibleReferenceTask.AsTask);

            // then
            actualBibleReferenceDependencyException.Should().BeEquivalentTo(
                expectedBibleReferenceDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someBibleReference, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowDependencyExceptionOnModifyIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            BibleReference someBibleReference = CreateRandomBibleReference();
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

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someBibleReference, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<BibleReference> modifyBibleReferenceTask =
                this.bibleReferenceService.ModifyBibleReferenceAsync(
                    someBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceDependencyException actualBibleReferenceDependencyException =
                await Assert.ThrowsAsync<BibleReferenceDependencyException>(
                    modifyBibleReferenceTask.AsTask);

            // then
            actualBibleReferenceDependencyException.Should().BeEquivalentTo(
                expectedBibleReferenceDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someBibleReference, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowOperationCanceledExceptionOnModifyIfCancellationRequestedAsync()
        {
            // given
            BibleReference someBibleReference = CreateRandomBibleReference();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<BibleReference> modifyBibleReferenceTask =
                this.bibleReferenceService.ModifyBibleReferenceAsync(
                    someBibleReference,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                modifyBibleReferenceTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnModifyIfSqlErrorOccursAndLogItAsync()
        {
            // given
            BibleReference someBibleReference = CreateRandomBibleReference();
            SqlException sqlException = GetSqlException();

            var failedStorageBibleReferenceException = new FailedStorageBibleReferenceException(
                message: "Failed bible reference storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedBibleReferenceDependencyException = new BibleReferenceDependencyException(
                message: "Bible reference dependency error occurred, contact support.",
                innerException: failedStorageBibleReferenceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someBibleReference, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<BibleReference> modifyBibleReferenceTask =
                this.bibleReferenceService.ModifyBibleReferenceAsync(
                    someBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceDependencyException actualBibleReferenceDependencyException =
                await Assert.ThrowsAsync<BibleReferenceDependencyException>(
                    modifyBibleReferenceTask.AsTask);

            // then
            actualBibleReferenceDependencyException.Should().BeEquivalentTo(
                expectedBibleReferenceDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someBibleReference, It.IsAny<SecurityContext>()),
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

        [Theory]
        [MemberData(nameof(ModifyDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnModifyIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            BibleReference someBibleReference = CreateRandomBibleReference();

            var expectedBibleReferenceDependencyValidationException = new BibleReferenceDependencyValidationException(
                message: "Bible reference dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someBibleReference, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<BibleReference> modifyBibleReferenceTask =
                this.bibleReferenceService.ModifyBibleReferenceAsync(
                    someBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceDependencyValidationException actualBibleReferenceDependencyValidationException =
                await Assert.ThrowsAsync<BibleReferenceDependencyValidationException>(
                    modifyBibleReferenceTask.AsTask);

            // then
            actualBibleReferenceDependencyValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceDependencyValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someBibleReference, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnModifyIfServiceErrorOccursAndLogItAsync()
        {
            // given
            BibleReference someBibleReference = CreateRandomBibleReference();
            var serviceException = new Exception();

            var failedBibleReferenceServiceException = new FailedBibleReferenceServiceException(
                message: "Failed bible reference service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedBibleReferenceServiceException = new BibleReferenceServiceException(
                message: "Bible reference service error occurred, contact support.",
                innerException: failedBibleReferenceServiceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someBibleReference, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<BibleReference> modifyBibleReferenceTask =
                this.bibleReferenceService.ModifyBibleReferenceAsync(
                    someBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceServiceException actualBibleReferenceServiceException =
                await Assert.ThrowsAsync<BibleReferenceServiceException>(
                    modifyBibleReferenceTask.AsTask);

            // then
            actualBibleReferenceServiceException.Should().BeEquivalentTo(
                expectedBibleReferenceServiceException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someBibleReference, It.IsAny<SecurityContext>()),
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
