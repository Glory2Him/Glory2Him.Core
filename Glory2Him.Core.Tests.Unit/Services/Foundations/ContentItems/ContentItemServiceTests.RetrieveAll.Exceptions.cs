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
using System.Linq;
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
        public async Task ShouldThrowDependencyExceptionOnRetrieveAllIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
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
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<IQueryable<ContentItem>> retrieveAllContentItemsTask =
                this.contentItemService.RetrieveAllContentItemsAsync(
                    TestContext.Current.CancellationToken);

            ContentItemDependencyException actualContentItemDependencyException =
                await Assert.ThrowsAsync<ContentItemDependencyException>(
                    retrieveAllContentItemsTask.AsTask);

            // then
            actualContentItemDependencyException.Should().BeEquivalentTo(
                expectedContentItemDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldThrowOperationCanceledExceptionOnRetrieveAllIfCancellationRequestedAsync()
        {
            // given
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<IQueryable<ContentItem>> retrieveAllContentItemsTask =
                this.contentItemService.RetrieveAllContentItemsAsync(cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                retrieveAllContentItemsTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnRetrieveAllIfSqlErrorOccursAndLogItAsync()
        {
            // given
            SqlException sqlException = GetSqlException();

            var failedStorageContentItemException = new FailedStorageContentItemException(
                message: "Failed content item storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedContentItemDependencyException = new ContentItemDependencyException(
                message: "Content item dependency error occurred, contact support.",
                innerException: failedStorageContentItemException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<IQueryable<ContentItem>> retrieveAllContentItemsTask =
                this.contentItemService.RetrieveAllContentItemsAsync(
                    TestContext.Current.CancellationToken);

            ContentItemDependencyException actualContentItemDependencyException =
                await Assert.ThrowsAsync<ContentItemDependencyException>(
                    retrieveAllContentItemsTask.AsTask);

            // then
            actualContentItemDependencyException.Should().BeEquivalentTo(
                expectedContentItemDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldThrowServiceExceptionOnRetrieveAllIfServiceErrorOccursAndLogItAsync()
        {
            // given
            var serviceException = new Exception();

            var failedContentItemServiceException = new FailedContentItemServiceException(
                message: "Failed content item service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedContentItemServiceException = new ContentItemServiceException(
                message: "Content item service error occurred, contact support.",
                innerException: failedContentItemServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<IQueryable<ContentItem>> retrieveAllContentItemsTask =
                this.contentItemService.RetrieveAllContentItemsAsync(
                    TestContext.Current.CancellationToken);

            ContentItemServiceException actualContentItemServiceException =
                await Assert.ThrowsAsync<ContentItemServiceException>(
                    retrieveAllContentItemsTask.AsTask);

            // then
            actualContentItemServiceException.Should().BeEquivalentTo(
                expectedContentItemServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()),
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
    }
}
