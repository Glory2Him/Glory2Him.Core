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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnModifyIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            Association someAssociation = CreateRandomAssociation();

            var expectedAssociationDependencyException = new AssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someAssociation, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    someAssociation,
                    TestContext.Current.CancellationToken);

            AssociationDependencyException actualAssociationDependencyException =
                await Assert.ThrowsAsync<AssociationDependencyException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationDependencyException.Should().BeEquivalentTo(
                expectedAssociationDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someAssociation, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowDependencyExceptionOnModifyIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            Association someAssociation = CreateRandomAssociation();
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

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someAssociation, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    someAssociation,
                    TestContext.Current.CancellationToken);

            AssociationDependencyException actualAssociationDependencyException =
                await Assert.ThrowsAsync<AssociationDependencyException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationDependencyException.Should().BeEquivalentTo(
                expectedAssociationDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someAssociation, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowOperationCanceledExceptionOnModifyIfCancellationRequestedAsync()
        {
            // given
            Association someAssociation = CreateRandomAssociation();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    someAssociation,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                modifyAssociationTask.AsTask);

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
            Association someAssociation = CreateRandomAssociation();
            SqlException sqlException = GetSqlException();

            var failedStorageAssociationException = new FailedStorageAssociationException(
                message: "Failed content item association storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedAssociationDependencyException = new AssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: failedStorageAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someAssociation, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    someAssociation,
                    TestContext.Current.CancellationToken);

            AssociationDependencyException actualAssociationDependencyException =
                await Assert.ThrowsAsync<AssociationDependencyException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationDependencyException.Should().BeEquivalentTo(
                expectedAssociationDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someAssociation, It.IsAny<SecurityContext>()),
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

        [Theory]
        [MemberData(nameof(ModifyDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnModifyIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            Association someAssociation = CreateRandomAssociation();

            var expectedAssociationDependencyValidationException =
                new AssociationDependencyValidationException(
                message: "Content item association dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someAssociation, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    someAssociation,
                    TestContext.Current.CancellationToken);

            AssociationDependencyValidationException
                actualAssociationDependencyValidationException =
                await Assert.ThrowsAsync<AssociationDependencyValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationDependencyValidationException.Should().BeEquivalentTo(
                expectedAssociationDependencyValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someAssociation, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowServiceExceptionOnModifyIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Association someAssociation = CreateRandomAssociation();
            var serviceException = new Exception();

            var failedAssociationServiceException = new FailedAssociationServiceException(
                message: "Failed content item association service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedAssociationServiceException = new AssociationServiceException(
                message: "Content item association service error occurred, contact support.",
                innerException: failedAssociationServiceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someAssociation, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    someAssociation,
                    TestContext.Current.CancellationToken);

            AssociationServiceException actualAssociationServiceException =
                await Assert.ThrowsAsync<AssociationServiceException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationServiceException.Should().BeEquivalentTo(
                expectedAssociationServiceException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someAssociation, It.IsAny<SecurityContext>()),
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
