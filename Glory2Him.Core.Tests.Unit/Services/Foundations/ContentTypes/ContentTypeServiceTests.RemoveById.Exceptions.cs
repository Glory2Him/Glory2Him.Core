// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Foundations.ContentTypes.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentTypes
{
    public partial class ContentTypeServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRemoveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            Guid someContentTypeId = Guid.NewGuid();

            var expectedContentTypeDependencyException = new ContentTypeDependencyException(
                message: "Content type dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ContentType> removeContentTypeByIdTask =
                this.contentTypeService.RemoveContentTypeByIdAsync(
                    someContentTypeId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentTypeDependencyException actualContentTypeDependencyException =
                await Assert.ThrowsAsync<ContentTypeDependencyException>(
                    removeContentTypeByIdTask.AsTask);

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
        public async Task ShouldThrowOperationCanceledExceptionOnRemoveByIdIfCancellationRequestedAndLogItAsync()
        {
            // given
            Guid someContentTypeId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ContentType> removeContentTypeByIdTask =
                this.contentTypeService.RemoveContentTypeByIdAsync(
                    someContentTypeId,
                    cancellationToken: cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                removeContentTypeByIdTask.AsTask);

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
            ValueTask<ContentType> removeContentTypeByIdTask =
                this.contentTypeService.RemoveContentTypeByIdAsync(
                    someContentTypeId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentTypeDependencyException actualContentTypeDependencyException =
                await Assert.ThrowsAsync<ContentTypeDependencyException>(
                    removeContentTypeByIdTask.AsTask);

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
        public async Task ShouldThrowDependencyValidationExceptionOnRemoveByIdIfDbUpdateConcurrencyExceptionOccursAndLogItAsync()
        {
            // given
            Guid someContentTypeId = Guid.NewGuid();
            ContentType someContentType = CreateRandomContentType();
            someContentType.IsDeleted = false;
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();

            var lockedContentTypeException = new LockedContentTypeException(
                message: "Locked content type record, please try again later.",
                innerException: dbUpdateConcurrencyException,
                data: dbUpdateConcurrencyException.Data);

            var expectedContentTypeDependencyValidationException = new ContentTypeDependencyValidationException(
                message: "Content type dependency validation error occurred, fix the errors and try again.",
                innerException: lockedContentTypeException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(someContentType);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(someContentType))
                    .ReturnsAsync(someContentType);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentTypeAsync(
                    someContentType,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(dbUpdateConcurrencyException);

            // when
            ValueTask<ContentType> removeContentTypeByIdTask =
                this.contentTypeService.RemoveContentTypeByIdAsync(
                    someContentTypeId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentTypeDependencyValidationException actualContentTypeDependencyValidationException =
                await Assert.ThrowsAsync<ContentTypeDependencyValidationException>(
                    removeContentTypeByIdTask.AsTask);

            // then
            actualContentTypeDependencyValidationException.Should().BeEquivalentTo(
                expectedContentTypeDependencyValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(someContentType),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentTypeAsync(
                    someContentType,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeDependencyValidationException))),
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
            ValueTask<ContentType> removeContentTypeByIdTask =
                this.contentTypeService.RemoveContentTypeByIdAsync(
                    someContentTypeId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentTypeServiceException actualContentTypeServiceException =
                await Assert.ThrowsAsync<ContentTypeServiceException>(
                    removeContentTypeByIdTask.AsTask);

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
