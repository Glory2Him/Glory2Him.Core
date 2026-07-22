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
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemAssociations
{
    public partial class ContentItemAssociationServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            ContentItemAssociation someContentItemAssociation = CreateRandomContentItemAssociation();
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

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ContentItemAssociation> addContentItemAssociationTask =
                this.contentItemAssociationService.AddContentItemAssociationAsync(
                    someContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationDependencyException actualContentItemAssociationDependencyException =
                await Assert.ThrowsAsync<ContentItemAssociationDependencyException>(
                    addContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationDependencyException.Should().BeEquivalentTo(
                expectedContentItemAssociationDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someContentItemAssociation, It.IsAny<SecurityContext>()),
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

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            ContentItemAssociation someContentItemAssociation = CreateRandomContentItemAssociation();

            var expectedContentItemAssociationDependencyException = new ContentItemAssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<ContentItemAssociation> addContentItemAssociationTask =
                this.contentItemAssociationService.AddContentItemAssociationAsync(
                    someContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationDependencyException actualContentItemAssociationDependencyException =
                await Assert.ThrowsAsync<ContentItemAssociationDependencyException>(
                    addContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationDependencyException.Should().BeEquivalentTo(
                expectedContentItemAssociationDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someContentItemAssociation, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowOperationCanceledExceptionOnAddIfCancellationRequestedAsync()
        {
            // given
            ContentItemAssociation someContentItemAssociation = CreateRandomContentItemAssociation();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ContentItemAssociation> addContentItemAssociationTask =
                this.contentItemAssociationService.AddContentItemAssociationAsync(
                    someContentItemAssociation,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                addContentItemAssociationTask.AsTask);

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
            ContentItemAssociation someContentItemAssociation = CreateRandomContentItemAssociation();
            SqlException sqlException = GetSqlException();

            var failedStorageContentItemAssociationException = new FailedStorageContentItemAssociationException(
                message: "Failed content item association storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedContentItemAssociationDependencyException = new ContentItemAssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: failedStorageContentItemAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<ContentItemAssociation> addContentItemAssociationTask =
                this.contentItemAssociationService.AddContentItemAssociationAsync(
                    someContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationDependencyException actualContentItemAssociationDependencyException =
                await Assert.ThrowsAsync<ContentItemAssociationDependencyException>(
                    addContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationDependencyException.Should().BeEquivalentTo(
                expectedContentItemAssociationDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someContentItemAssociation, It.IsAny<SecurityContext>()),
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

        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            ContentItemAssociation someContentItemAssociation = CreateRandomContentItemAssociation();

            var expectedContentItemAssociationDependencyValidationException =
                new ContentItemAssociationDependencyValidationException(
                message: "Content item association dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<ContentItemAssociation> addContentItemAssociationTask =
                this.contentItemAssociationService.AddContentItemAssociationAsync(
                    someContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationDependencyValidationException
                actualContentItemAssociationDependencyValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationDependencyValidationException>(
                    addContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationDependencyValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationDependencyValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someContentItemAssociation, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowServiceExceptionOnAddIfServiceErrorOccursAndLogItAsync()
        {
            // given
            ContentItemAssociation someContentItemAssociation = CreateRandomContentItemAssociation();
            var serviceException = new Exception();

            var failedContentItemAssociationServiceException = new FailedContentItemAssociationServiceException(
                message: "Failed content item association service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedContentItemAssociationServiceException = new ContentItemAssociationServiceException(
                message: "Content item association service error occurred, contact support.",
                innerException: failedContentItemAssociationServiceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<ContentItemAssociation> addContentItemAssociationTask =
                this.contentItemAssociationService.AddContentItemAssociationAsync(
                    someContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationServiceException actualContentItemAssociationServiceException =
                await Assert.ThrowsAsync<ContentItemAssociationServiceException>(
                    addContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationServiceException.Should().BeEquivalentTo(
                expectedContentItemAssociationServiceException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someContentItemAssociation, It.IsAny<SecurityContext>()),
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
