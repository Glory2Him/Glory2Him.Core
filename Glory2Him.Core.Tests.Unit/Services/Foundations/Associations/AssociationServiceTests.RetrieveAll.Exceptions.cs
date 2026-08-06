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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveAllIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutAssociationException =
                new TimeoutAssociationException(
                    message: "Failed content item association timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedAssociationDependencyException = new AssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: timeoutAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<IQueryable<Association>> retrieveAllAssociationsTask =
                this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            AssociationDependencyException actualAssociationDependencyException =
                await Assert.ThrowsAsync<AssociationDependencyException>(
                    retrieveAllAssociationsTask.AsTask);

            // then
            actualAssociationDependencyException.Should().BeEquivalentTo(
                expectedAssociationDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationDependencyException))),
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
            ValueTask<IQueryable<Association>> retrieveAllAssociationsTask =
                this.associationService.RetrieveAllAssociationsAsync(cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                retrieveAllAssociationsTask.AsTask);

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

            var failedStorageAssociationException = new FailedStorageAssociationException(
                message: "Failed content item association storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedAssociationDependencyException = new AssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: failedStorageAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<IQueryable<Association>> retrieveAllAssociationsTask =
                this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            AssociationDependencyException actualAssociationDependencyException =
                await Assert.ThrowsAsync<AssociationDependencyException>(
                    retrieveAllAssociationsTask.AsTask);

            // then
            actualAssociationDependencyException.Should().BeEquivalentTo(
                expectedAssociationDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedAssociationDependencyException))),
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

            var failedAssociationServiceException = new FailedAssociationServiceException(
                message: "Failed content item association service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedAssociationServiceException = new AssociationServiceException(
                message: "Content item association service error occurred, contact support.",
                innerException: failedAssociationServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<IQueryable<Association>> retrieveAllAssociationsTask =
                this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            AssociationServiceException actualAssociationServiceException =
                await Assert.ThrowsAsync<AssociationServiceException>(
                    retrieveAllAssociationsTask.AsTask);

            // then
            actualAssociationServiceException.Should().BeEquivalentTo(
                expectedAssociationServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
