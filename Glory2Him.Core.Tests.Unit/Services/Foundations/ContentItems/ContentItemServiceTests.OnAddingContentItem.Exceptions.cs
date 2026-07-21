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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnAddingContentItemEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<ContentItem> requestEnvelope = CreateRandomContentItemRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onAddingTask =
                this.contentItemService.OnAddingContentItemAsync(
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
        public async Task ShouldThrowDependencyExceptionOnAddingContentItemEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<ContentItem> requestEnvelope = CreateRandomContentItemRequestEnvelope();

            var expectedContentItemDependencyException = new ContentItemDependencyException(
                message: "Content item dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemOnAddingContentItemSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onAddingTask =
                this.contentItemService.OnAddingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemDependencyException actualContentItemDependencyException =
                await Assert.ThrowsAsync<ContentItemDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualContentItemDependencyException.Should().BeEquivalentTo(
                expectedContentItemDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddingContentItemEventIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ContentItem> requestEnvelope = CreateRandomContentItemRequestEnvelope();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutContentItemException =
                new TimeoutContentItemException(
                    message: "Failed content item timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedContentItemDependencyException = new ContentItemDependencyException(
                message: "Content item dependency error occurred, contact support.",
                innerException: timeoutContentItemException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemOnAddingContentItemSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onAddingTask =
                this.contentItemService.OnAddingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemDependencyException actualContentItemDependencyException =
                await Assert.ThrowsAsync<ContentItemDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualContentItemDependencyException.Should().BeEquivalentTo(
                expectedContentItemDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemOnAddingContentItemSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnAddingContentItemEventIfSqlErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ContentItem> requestEnvelope = CreateRandomContentItemRequestEnvelope();
            SqlException sqlException = GetSqlException();

            var failedStorageContentItemException = new FailedStorageContentItemException(
                message: "Failed content item storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedContentItemDependencyException = new ContentItemDependencyException(
                message: "Content item dependency error occurred, contact support.",
                innerException: failedStorageContentItemException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemOnAddingContentItemSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onAddingTask =
                this.contentItemService.OnAddingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemDependencyException actualContentItemDependencyException =
                await Assert.ThrowsAsync<ContentItemDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualContentItemDependencyException.Should().BeEquivalentTo(
                expectedContentItemDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedContentItemDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddingContentItemEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<ContentItem> requestEnvelope = CreateRandomContentItemRequestEnvelope();

            var expectedContentItemDependencyValidationException = new ContentItemDependencyValidationException(
                message: "Content item dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemOnAddingContentItemSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onAddingTask =
                this.contentItemService.OnAddingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemDependencyValidationException actualContentItemDependencyValidationException =
                await Assert.ThrowsAsync<ContentItemDependencyValidationException>(
                    onAddingTask.AsTask);

            // then
            actualContentItemDependencyValidationException.Should().BeEquivalentTo(
                expectedContentItemDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnAddingContentItemEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ContentItem> requestEnvelope = CreateRandomContentItemRequestEnvelope();
            var serviceException = new Exception();

            var failedContentItemServiceException = new FailedContentItemServiceException(
                message: "Failed content item service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedContentItemServiceException = new ContentItemServiceException(
                message: "Content item service error occurred, contact support.",
                innerException: failedContentItemServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemOnAddingContentItemSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onAddingTask =
                this.contentItemService.OnAddingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemServiceException actualContentItemServiceException =
                await Assert.ThrowsAsync<ContentItemServiceException>(
                    onAddingTask.AsTask);

            // then
            actualContentItemServiceException.Should().BeEquivalentTo(
                expectedContentItemServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
