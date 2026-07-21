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
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Foundations.ContentTypes.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentTypes
{
    public partial class ContentTypeServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveByIdIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            Guid someContentTypeId = Guid.NewGuid();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutContentTypeException =
                new TimeoutContentTypeException(
                    message: "Failed content type timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedContentTypeDependencyException = new ContentTypeDependencyException(
                message: "Content type dependency error occurred, contact support.",
                innerException: timeoutContentTypeException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ContentType> retrieveContentTypeByIdTask =
                this.contentTypeService.RetrieveContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken);

            ContentTypeDependencyException actualContentTypeDependencyException =
                await Assert.ThrowsAsync<ContentTypeDependencyException>(
                    retrieveContentTypeByIdTask.AsTask);

            // then
            actualContentTypeDependencyException.Should().BeEquivalentTo(
                expectedContentTypeDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeDependencyException))),
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
            Guid someContentTypeId = Guid.NewGuid();
            SqlException sqlException = GetSqlException();

            var failedStorageContentTypeException = new FailedStorageContentTypeException(
                message: "Failed content type storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedContentTypeDependencyException = new ContentTypeDependencyException(
                message: "Content type dependency error occurred, contact support.",
                innerException: failedStorageContentTypeException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<ContentType> retrieveContentTypeByIdTask =
                this.contentTypeService.RetrieveContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken);

            ContentTypeDependencyException actualContentTypeDependencyException =
                await Assert.ThrowsAsync<ContentTypeDependencyException>(
                    retrieveContentTypeByIdTask.AsTask);

            // then
            actualContentTypeDependencyException.Should().BeEquivalentTo(
                expectedContentTypeDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedContentTypeDependencyException))),
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
            Guid someContentTypeId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ContentType> retrieveContentTypeByIdTask =
                this.contentTypeService.RetrieveContentTypeByIdAsync(
                    someContentTypeId,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                retrieveContentTypeByIdTask.AsTask);

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
            Guid someContentTypeId = Guid.NewGuid();
            var serviceException = new Exception();

            var failedContentTypeServiceException = new FailedContentTypeServiceException(
                message: "Failed content type service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedContentTypeServiceException = new ContentTypeServiceException(
                message: "Content type service error occurred, contact support.",
                innerException: failedContentTypeServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ContentType> retrieveContentTypeByIdTask =
                this.contentTypeService.RetrieveContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken);

            ContentTypeServiceException actualContentTypeServiceException =
                await Assert.ThrowsAsync<ContentTypeServiceException>(
                    retrieveContentTypeByIdTask.AsTask);

            // then
            actualContentTypeServiceException.Should().BeEquivalentTo(
                expectedContentTypeServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
