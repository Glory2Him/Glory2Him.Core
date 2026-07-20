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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrieveByIdIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Guid someContentItemId = Guid.NewGuid();
            var serviceException = new Exception();

            var failedContentItemServiceException = new FailedContentItemServiceException(
                message: "Failed content item service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedContentItemServiceException = new ContentItemServiceException(
                message: "Content item service error occurred, contact support.",
                innerException: failedContentItemServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    someContentItemId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ContentItem> retrieveContentItemByIdTask =
                this.contentItemService.RetrieveContentItemByIdAsync(
                    someContentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemServiceException actualContentItemServiceException =
                await Assert.ThrowsAsync<ContentItemServiceException>(
                    retrieveContentItemByIdTask.AsTask);

            // then
            actualContentItemServiceException.Should().BeEquivalentTo(
                expectedContentItemServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(
                    someContentItemId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveByIdIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            Guid someContentItemId = Guid.NewGuid();
            var operationCanceledException = new OperationCanceledException();

            var timeoutContentItemException = new TimeoutContentItemException(
                message: "Content item timed out, contact support.",
                innerException: new TimeoutException(),
                data: operationCanceledException.Data);

            var expectedContentItemDependencyException = new ContentItemDependencyException(
                message: "Content item dependency error occurred, contact support.",
                innerException: timeoutContentItemException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    someContentItemId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ContentItem> retrieveContentItemByIdTask =
                this.contentItemService.RetrieveContentItemByIdAsync(
                    someContentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemDependencyException actualContentItemDependencyException =
                await Assert.ThrowsAsync<ContentItemDependencyException>(
                    retrieveContentItemByIdTask.AsTask);

            // then
            actualContentItemDependencyException.Should().BeEquivalentTo(
                expectedContentItemDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(
                    someContentItemId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemDependencyException))),
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
            Guid someContentItemId = Guid.NewGuid();
            SqlException sqlException = GetSqlException();

            var failedStorageContentItemException = new FailedStorageContentItemException(
                message: "Failed content item storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedContentItemDependencyException = new ContentItemDependencyException(
                message: "Content item dependency error occurred, contact support.",
                innerException: failedStorageContentItemException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    someContentItemId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<ContentItem> retrieveContentItemByIdTask =
                this.contentItemService.RetrieveContentItemByIdAsync(
                    someContentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemDependencyException actualContentItemDependencyException =
                await Assert.ThrowsAsync<ContentItemDependencyException>(
                    retrieveContentItemByIdTask.AsTask);

            // then
            actualContentItemDependencyException.Should().BeEquivalentTo(
                expectedContentItemDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(
                    someContentItemId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedContentItemDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRetrieveByIdIfCancellationRequestedAndLogItAsync()
        {
            // given
            Guid someContentItemId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ContentItem> retrieveContentItemByIdTask =
                this.contentItemService.RetrieveContentItemByIdAsync(
                    someContentItemId,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                retrieveContentItemByIdTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
