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
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ContentTypes.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentTypes
{
    public partial class ContentTypeServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnModifyIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ContentType someContentType = CreateRandomContentType();

            var expectedContentTypeDependencyException = new ContentTypeDependencyException(
                message: "Content type dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someContentType, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<ContentType> modifyContentTypeTask =
                this.contentTypeService.ModifyContentTypeAsync(
                    someContentType,
                    TestContext.Current.CancellationToken);

            ContentTypeDependencyException actualContentTypeDependencyException =
                await Assert.ThrowsAsync<ContentTypeDependencyException>(
                    modifyContentTypeTask.AsTask);

            // then
            actualContentTypeDependencyException.Should().BeEquivalentTo(
                expectedContentTypeDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someContentType, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowDependencyExceptionOnModifyIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ContentType someContentType = CreateRandomContentType();
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

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someContentType, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ContentType> modifyContentTypeTask =
                this.contentTypeService.ModifyContentTypeAsync(
                    someContentType,
                    TestContext.Current.CancellationToken);

            ContentTypeDependencyException actualContentTypeDependencyException =
                await Assert.ThrowsAsync<ContentTypeDependencyException>(
                    modifyContentTypeTask.AsTask);

            // then
            actualContentTypeDependencyException.Should().BeEquivalentTo(
                expectedContentTypeDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someContentType, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowOperationCanceledExceptionOnModifyIfCancellationRequestedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ContentType someContentType = CreateRandomContentType();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ContentType> modifyContentTypeTask =
                this.contentTypeService.ModifyContentTypeAsync(
                    someContentType,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                modifyContentTypeTask.AsTask);

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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ContentType someContentType = CreateRandomContentType();
            SqlException sqlException = GetSqlException();

            var failedStorageContentTypeException = new FailedStorageContentTypeException(
                message: "Failed content type storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedContentTypeDependencyException = new ContentTypeDependencyException(
                message: "Content type dependency error occurred, contact support.",
                innerException: failedStorageContentTypeException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someContentType, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<ContentType> modifyContentTypeTask =
                this.contentTypeService.ModifyContentTypeAsync(
                    someContentType,
                    TestContext.Current.CancellationToken);

            ContentTypeDependencyException actualContentTypeDependencyException =
                await Assert.ThrowsAsync<ContentTypeDependencyException>(
                    modifyContentTypeTask.AsTask);

            // then
            actualContentTypeDependencyException.Should().BeEquivalentTo(
                expectedContentTypeDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someContentType, It.IsAny<SecurityContext>()),
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

        [Theory]
        [MemberData(nameof(ModifyDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnModifyIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ContentType someContentType = CreateRandomContentType();

            var expectedContentTypeDependencyValidationException = new ContentTypeDependencyValidationException(
                message: "Content type dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someContentType, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<ContentType> modifyContentTypeTask =
                this.contentTypeService.ModifyContentTypeAsync(
                    someContentType,
                    TestContext.Current.CancellationToken);

            ContentTypeDependencyValidationException actualContentTypeDependencyValidationException =
                await Assert.ThrowsAsync<ContentTypeDependencyValidationException>(
                    modifyContentTypeTask.AsTask);

            // then
            actualContentTypeDependencyValidationException.Should().BeEquivalentTo(
                expectedContentTypeDependencyValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someContentType, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowServiceExceptionOnModifyIfServiceErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ContentType someContentType = CreateRandomContentType();
            var serviceException = new Exception();

            var failedContentTypeServiceException = new FailedContentTypeServiceException(
                message: "Failed content type service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedContentTypeServiceException = new ContentTypeServiceException(
                message: "Content type service error occurred, contact support.",
                innerException: failedContentTypeServiceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someContentType, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<ContentType> modifyContentTypeTask =
                this.contentTypeService.ModifyContentTypeAsync(
                    someContentType,
                    TestContext.Current.CancellationToken);

            ContentTypeServiceException actualContentTypeServiceException =
                await Assert.ThrowsAsync<ContentTypeServiceException>(
                    modifyContentTypeTask.AsTask);

            // then
            actualContentTypeServiceException.Should().BeEquivalentTo(
                expectedContentTypeServiceException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someContentType, It.IsAny<SecurityContext>()),
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
