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
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemAssociations
{
    public partial class ContentItemAssociationServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRemoveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            Guid someContentItemAssociationId = Guid.NewGuid();

            var expectedContentItemAssociationDependencyException = new ContentItemAssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ContentItemAssociation> removeContentItemAssociationByIdTask =
                this.contentItemAssociationService.RemoveContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentItemAssociationDependencyException actualContentItemAssociationDependencyException =
                await Assert.ThrowsAsync<ContentItemAssociationDependencyException>(
                    removeContentItemAssociationByIdTask.AsTask);

            // then
            actualContentItemAssociationDependencyException.Should().BeEquivalentTo(
                expectedContentItemAssociationDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRemoveByIdIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            Guid someContentItemAssociationId = Guid.NewGuid();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutContentItemAssociationException =
                new TimeoutContentItemAssociationException(
                    message: "Failed content item association timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedContentItemAssociationDependencyException = new ContentItemAssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: timeoutContentItemAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ContentItemAssociation> removeContentItemAssociationByIdTask =
                this.contentItemAssociationService.RemoveContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentItemAssociationDependencyException actualContentItemAssociationDependencyException =
                await Assert.ThrowsAsync<ContentItemAssociationDependencyException>(
                    removeContentItemAssociationByIdTask.AsTask);

            // then
            actualContentItemAssociationDependencyException.Should().BeEquivalentTo(
                expectedContentItemAssociationDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRemoveByIdIfCancellationRequestedAsync()
        {
            // given
            Guid someContentItemAssociationId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ContentItemAssociation> removeContentItemAssociationByIdTask =
                this.contentItemAssociationService.RemoveContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    cancellationToken: cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                removeContentItemAssociationByIdTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnRemoveByIdIfSqlErrorOccursAndLogItAsync()
        {
            // given
            Guid someContentItemAssociationId = Guid.NewGuid();
            SqlException sqlException = GetSqlException();

            var failedStorageContentItemAssociationException = new FailedStorageContentItemAssociationException(
                message: "Failed content item association storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedContentItemAssociationDependencyException = new ContentItemAssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: failedStorageContentItemAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<ContentItemAssociation> removeContentItemAssociationByIdTask =
                this.contentItemAssociationService.RemoveContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentItemAssociationDependencyException actualContentItemAssociationDependencyException =
                await Assert.ThrowsAsync<ContentItemAssociationDependencyException>(
                    removeContentItemAssociationByIdTask.AsTask);

            // then
            actualContentItemAssociationDependencyException.Should().BeEquivalentTo(
                expectedContentItemAssociationDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyValidationExceptionOnRemoveByIdIfDbUpdateConcurrencyExceptionOccursAndLogItAsync()
        {
            // given
            Guid someContentItemAssociationId = Guid.NewGuid();
            ContentItemAssociation someContentItemAssociation = CreateRandomContentItemAssociation();
            someContentItemAssociation.IsDeleted = false;
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();

            var lockedContentItemAssociationException = new LockedContentItemAssociationException(
                message: "Locked content item association record, please try again later.",
                innerException: dbUpdateConcurrencyException,
                data: dbUpdateConcurrencyException.Data);

            var expectedContentItemAssociationDependencyValidationException =
                new ContentItemAssociationDependencyValidationException(
                message: "Content item association dependency validation error occurred, fix the errors and try again.",
                innerException: lockedContentItemAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(someContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(someContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(someContentItemAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAssociationAsync(
                    someContentItemAssociation,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(dbUpdateConcurrencyException);

            // when
            ValueTask<ContentItemAssociation> removeContentItemAssociationByIdTask =
                this.contentItemAssociationService.RemoveContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentItemAssociationDependencyValidationException
                actualContentItemAssociationDependencyValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationDependencyValidationException>(
                    removeContentItemAssociationByIdTask.AsTask);

            // then
            actualContentItemAssociationDependencyValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationDependencyValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(someContentItemAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemAssociationAsync(
                    someContentItemAssociation,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRemoveByIdIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Guid someContentItemAssociationId = Guid.NewGuid();
            var serviceException = new Exception();

            var failedContentItemAssociationServiceException = new FailedContentItemAssociationServiceException(
                message: "Failed content item association service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedContentItemAssociationServiceException = new ContentItemAssociationServiceException(
                message: "Content item association service error occurred, contact support.",
                innerException: failedContentItemAssociationServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ContentItemAssociation> removeContentItemAssociationByIdTask =
                this.contentItemAssociationService.RemoveContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentItemAssociationServiceException actualContentItemAssociationServiceException =
                await Assert.ThrowsAsync<ContentItemAssociationServiceException>(
                    removeContentItemAssociationByIdTask.AsTask);

            // then
            actualContentItemAssociationServiceException.Should().BeEquivalentTo(
                expectedContentItemAssociationServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
