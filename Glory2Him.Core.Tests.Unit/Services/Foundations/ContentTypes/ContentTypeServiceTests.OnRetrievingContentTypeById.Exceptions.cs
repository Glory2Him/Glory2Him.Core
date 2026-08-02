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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Foundations.ContentTypes.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentTypes
{
    public partial class ContentTypeServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRetrievingContentTypeByIdEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<ContentType> requestEnvelope = CreateRandomContentTypeRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<ContentType>?> onRetrievingTask =
                this.contentTypeService.OnRetrievingContentTypeByIdAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onRetrievingTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrievingContentTypeByIdEventIfOperationCanceledExceptionOccursAndLogItAsync()
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
                broker.SelectContentTypeByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<ContentType>?> onRetrievingTask =
                this.contentTypeService.OnRetrievingContentTypeByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentTypeDependencyException actualContentTypeDependencyException =
                await Assert.ThrowsAsync<ContentTypeDependencyException>(
                    onRetrievingTask.AsTask);

            // then: the nested retrieve categorizes the timeout and logs it exactly once —
            // the substrate wrapper must not double-wrap or re-log it.
            actualContentTypeDependencyException.Should().BeEquivalentTo(
                expectedContentTypeDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()),
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
        public async Task ShouldPassThroughDependencyExceptionOnRetrievingContentTypeByIdEventAsync()
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
                broker.SelectContentTypeByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<EventEnvelope<ContentType>?> onRetrievingTask =
                this.contentTypeService.OnRetrievingContentTypeByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentTypeDependencyException actualContentTypeDependencyException =
                await Assert.ThrowsAsync<ContentTypeDependencyException>(
                    onRetrievingTask.AsTask);

            // then: the nested retrieve's categorized exception surfaces unwrapped and is
            // logged exactly once — the substrate wrapper must not double-wrap or re-log it.
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

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrievingContentTypeByIdEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            ContentType storageContentType = CreateRandomContentType();
            storageContentType.IsDeleted = false;
            storageContentType.ApprovalStatus = ApprovalStatus.Approved;
            storageContentType.IsPublished = true;
            storageContentType.PublishDate = null;
            var serviceException = new Exception();

            var requestEnvelope = new EventEnvelope<ContentType>
            {
                Content = new ContentType { Id = storageContentType.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var failedContentTypeServiceException = new FailedContentTypeServiceException(
                message: "Failed content type service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedContentTypeServiceException = new ContentTypeServiceException(
                message: "Content type service error occurred, contact support.",
                innerException: failedContentTypeServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    storageContentType.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentType);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(requestEnvelope, storageContentType))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<ContentType>?> onRetrievingTask =
                this.contentTypeService.OnRetrievingContentTypeByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentTypeServiceException actualContentTypeServiceException =
                await Assert.ThrowsAsync<ContentTypeServiceException>(
                    onRetrievingTask.AsTask);

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
