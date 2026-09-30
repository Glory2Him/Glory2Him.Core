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
using System.Collections.Generic;
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
    /// <summary>
    /// The standard paths of the public-groups read, mapped as the feed read maps its own: the
    /// read is awaited here, so a storage fault or a cancelled token reaches a catch arm able to
    /// report it.
    /// </summary>
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnRetrievePublicGroupsIfSqlErrorOccursAndLogItAsync()
        {
            // given
            SqlException sqlException = GetSqlException();
            IReadOnlyList<Guid> someContentItemIds = new[] { Guid.NewGuid() };

            var failedStorageContentItemException = new FailedStorageContentItemException(
                message: "Failed content item storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedContentItemDependencyException = new ContentItemDependencyException(
                message: "Content item dependency error occurred, contact support.",
                innerException: failedStorageContentItemException);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemsAsync(
                    It.IsAny<Func<IQueryable<ContentItem>, IQueryable<PublicContentItemGroup>>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<IReadOnlyList<PublicContentItemGroup>> retrievePublicContentItemGroupsTask =
                this.contentItemService.RetrievePublicContentItemGroupsAsync(
                    contentItemIds: someContentItemIds,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentItemDependencyException actualContentItemDependencyException =
                await Assert.ThrowsAsync<ContentItemDependencyException>(
                    retrievePublicContentItemGroupsTask.AsTask);

            // then
            actualContentItemDependencyException.Should().BeEquivalentTo(
                expectedContentItemDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedContentItemDependencyException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrievePublicGroupsIfTimeoutOccursAndLogItAsync()
        {
            // given: an OperationCanceledException with no cancellation REQUESTED is the
            // dependency timing out, not the caller walking away
            var operationCanceledException = new OperationCanceledException();
            IReadOnlyList<Guid> someContentItemIds = new[] { Guid.NewGuid() };

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

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemsAsync(
                    It.IsAny<Func<IQueryable<ContentItem>, IQueryable<PublicContentItemGroup>>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<IReadOnlyList<PublicContentItemGroup>> retrievePublicContentItemGroupsTask =
                this.contentItemService.RetrievePublicContentItemGroupsAsync(
                    contentItemIds: someContentItemIds,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentItemDependencyException actualContentItemDependencyException =
                await Assert.ThrowsAsync<ContentItemDependencyException>(
                    retrievePublicContentItemGroupsTask.AsTask);

            // then
            actualContentItemDependencyException.Should().BeEquivalentTo(
                expectedContentItemDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemDependencyException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRetrievePublicGroupsIfTokenIsCancelledAsync()
        {
            // given: UNWRAPPED. A caller who cancelled is not a dependency fault and must not
            // be logged or reported as one.
            var cancellationToken = new CancellationToken(canceled: true);
            IReadOnlyList<Guid> someContentItemIds = new[] { Guid.NewGuid() };

            // when
            ValueTask<IReadOnlyList<PublicContentItemGroup>> retrievePublicContentItemGroupsTask =
                this.contentItemService.RetrievePublicContentItemGroupsAsync(
                    contentItemIds: someContentItemIds,
                    cancellationToken: cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                retrievePublicContentItemGroupsTask.AsTask);

            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrievePublicGroupsIfServiceErrorOccursAndLogItAsync()
        {
            // given
            var serviceException = new Exception("Service error occurred.");
            IReadOnlyList<Guid> someContentItemIds = new[] { Guid.NewGuid() };

            var failedContentItemServiceException = new FailedContentItemServiceException(
                message: "Failed content item service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedContentItemServiceException = new ContentItemServiceException(
                message: "Content item service error occurred, contact support.",
                innerException: failedContentItemServiceException);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemsAsync(
                    It.IsAny<Func<IQueryable<ContentItem>, IQueryable<PublicContentItemGroup>>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<IReadOnlyList<PublicContentItemGroup>> retrievePublicContentItemGroupsTask =
                this.contentItemService.RetrievePublicContentItemGroupsAsync(
                    contentItemIds: someContentItemIds,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentItemServiceException actualContentItemServiceException =
                await Assert.ThrowsAsync<ContentItemServiceException>(
                    retrievePublicContentItemGroupsTask.AsTask);

            // then
            actualContentItemServiceException.Should().BeEquivalentTo(
                expectedContentItemServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemServiceException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
