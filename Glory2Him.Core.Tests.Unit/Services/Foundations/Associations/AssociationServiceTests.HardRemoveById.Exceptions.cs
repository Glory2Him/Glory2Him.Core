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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnHardRemoveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someAssociationId = Guid.NewGuid();

            var expectedAssociationDependencyException = new AssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<Association> hardRemoveAssociationByIdTask =
                this.associationService.HardRemoveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationDependencyException actualAssociationDependencyException =
                await Assert.ThrowsAsync<AssociationDependencyException>(
                    hardRemoveAssociationByIdTask.AsTask);

            // then
            actualAssociationDependencyException.Should().BeEquivalentTo(
                expectedAssociationDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken),
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
        public async Task ShouldThrowDependencyExceptionOnHardRemoveByIdIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someAssociationId = Guid.NewGuid();
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
                broker.SelectAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<Association> hardRemoveAssociationByIdTask =
                this.associationService.HardRemoveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationDependencyException actualAssociationDependencyException =
                await Assert.ThrowsAsync<AssociationDependencyException>(
                    hardRemoveAssociationByIdTask.AsTask);

            // then
            actualAssociationDependencyException.Should().BeEquivalentTo(
                expectedAssociationDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken),
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
        public async Task ShouldThrowOperationCanceledExceptionOnHardRemoveByIdIfCancellationRequestedAsync()
        {
            // given
            Guid someAssociationId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<Association> hardRemoveAssociationByIdTask =
                this.associationService.HardRemoveAssociationByIdAsync(
                    someAssociationId,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                hardRemoveAssociationByIdTask.AsTask);

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
            Guid someAssociationId = Guid.NewGuid();
            SqlException sqlException = GetSqlException();

            var failedStorageAssociationException = new FailedStorageAssociationException(
                message: "Failed content item association storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedAssociationDependencyException = new AssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: failedStorageAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<Association> hardRemoveAssociationByIdTask =
                this.associationService.HardRemoveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationDependencyException actualAssociationDependencyException =
                await Assert.ThrowsAsync<AssociationDependencyException>(
                    hardRemoveAssociationByIdTask.AsTask);

            // then
            actualAssociationDependencyException.Should().BeEquivalentTo(
                expectedAssociationDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken),
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
        public async Task ShouldThrowDependencyValidationExceptionOnHardRemoveByIdIfDbUpdateConcurrencyExceptionOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someAssociationId = Guid.NewGuid();
            Association someAssociation = CreateRandomAssociation();
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();

            var lockedAssociationException = new LockedAssociationException(
                message: "Locked content item association record, please try again later.",
                innerException: dbUpdateConcurrencyException,
                data: dbUpdateConcurrencyException.Data);

            var expectedAssociationDependencyValidationException =
                new AssociationDependencyValidationException(
                message: "Content item association dependency validation error occurred, fix the errors and try again.",
                innerException: lockedAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(someAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteAssociationAsync(
                    someAssociation,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(dbUpdateConcurrencyException);

            // when
            ValueTask<Association> hardRemoveAssociationByIdTask =
                this.associationService.HardRemoveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationDependencyValidationException
                actualAssociationDependencyValidationException =
                await Assert.ThrowsAsync<AssociationDependencyValidationException>(
                    hardRemoveAssociationByIdTask.AsTask);

            // then
            actualAssociationDependencyValidationException.Should().BeEquivalentTo(
                expectedAssociationDependencyValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteAssociationAsync(
                    someAssociation,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationDependencyValidationException))),
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
            Guid someAssociationId = Guid.NewGuid();
            var serviceException = new Exception();

            var failedAssociationServiceException = new FailedAssociationServiceException(
                message: "Failed content item association service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedAssociationServiceException = new AssociationServiceException(
                message: "Content item association service error occurred, contact support.",
                innerException: failedAssociationServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<Association> hardRemoveAssociationByIdTask =
                this.associationService.HardRemoveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationServiceException actualAssociationServiceException =
                await Assert.ThrowsAsync<AssociationServiceException>(
                    hardRemoveAssociationByIdTask.AsTask);

            // then
            actualAssociationServiceException.Should().BeEquivalentTo(
                expectedAssociationServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken),
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
