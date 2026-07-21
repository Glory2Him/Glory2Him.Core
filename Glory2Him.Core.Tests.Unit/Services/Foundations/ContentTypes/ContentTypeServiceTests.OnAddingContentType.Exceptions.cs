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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Foundations.ContentTypes.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentTypes
{
    public partial class ContentTypeServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnAddingContentTypeEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<ContentType> requestEnvelope = CreateRandomContentTypeRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<ContentType>?> onAddingTask =
                this.contentTypeService.OnAddingContentTypeAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onAddingTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddingContentTypeEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<ContentType> requestEnvelope = CreateRandomContentTypeRequestEnvelope();

            var expectedContentTypeDependencyException = new ContentTypeDependencyException(
                message: "Content type dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentTypeOnAddingContentTypeSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<ContentType>?> onAddingTask =
                this.contentTypeService.OnAddingContentTypeAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentTypeDependencyException actualContentTypeDependencyException =
                await Assert.ThrowsAsync<ContentTypeDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualContentTypeDependencyException.Should().BeEquivalentTo(
                expectedContentTypeDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddingContentTypeEventIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ContentType> requestEnvelope = CreateRandomContentTypeRequestEnvelope();
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
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentTypeOnAddingContentTypeSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<ContentType>?> onAddingTask =
                this.contentTypeService.OnAddingContentTypeAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentTypeDependencyException actualContentTypeDependencyException =
                await Assert.ThrowsAsync<ContentTypeDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualContentTypeDependencyException.Should().BeEquivalentTo(
                expectedContentTypeDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentTypeOnAddingContentTypeSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnAddingContentTypeEventIfSqlErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ContentType> requestEnvelope = CreateRandomContentTypeRequestEnvelope();
            SqlException sqlException = GetSqlException();

            var failedStorageContentTypeException = new FailedStorageContentTypeException(
                message: "Failed content type storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedContentTypeDependencyException = new ContentTypeDependencyException(
                message: "Content type dependency error occurred, contact support.",
                innerException: failedStorageContentTypeException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentTypeOnAddingContentTypeSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<EventEnvelope<ContentType>?> onAddingTask =
                this.contentTypeService.OnAddingContentTypeAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentTypeDependencyException actualContentTypeDependencyException =
                await Assert.ThrowsAsync<ContentTypeDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualContentTypeDependencyException.Should().BeEquivalentTo(
                expectedContentTypeDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedContentTypeDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddingContentTypeEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<ContentType> requestEnvelope = CreateRandomContentTypeRequestEnvelope();

            var expectedContentTypeDependencyValidationException = new ContentTypeDependencyValidationException(
                message: "Content type dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentTypeOnAddingContentTypeSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<ContentType>?> onAddingTask =
                this.contentTypeService.OnAddingContentTypeAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentTypeDependencyValidationException actualContentTypeDependencyValidationException =
                await Assert.ThrowsAsync<ContentTypeDependencyValidationException>(
                    onAddingTask.AsTask);

            // then
            actualContentTypeDependencyValidationException.Should().BeEquivalentTo(
                expectedContentTypeDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnAddingContentTypeEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ContentType> requestEnvelope = CreateRandomContentTypeRequestEnvelope();
            var serviceException = new Exception();

            var failedContentTypeServiceException = new FailedContentTypeServiceException(
                message: "Failed content type service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedContentTypeServiceException = new ContentTypeServiceException(
                message: "Content type service error occurred, contact support.",
                innerException: failedContentTypeServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentTypeOnAddingContentTypeSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<ContentType>?> onAddingTask =
                this.contentTypeService.OnAddingContentTypeAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentTypeServiceException actualContentTypeServiceException =
                await Assert.ThrowsAsync<ContentTypeServiceException>(
                    onAddingTask.AsTask);

            // then
            actualContentTypeServiceException.Should().BeEquivalentTo(
                expectedContentTypeServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
