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
using Microsoft.EntityFrameworkCore;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnHardRemoveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            Guid someContentItemId = Guid.NewGuid();

            var expectedContentItemDependencyException = new ContentItemDependencyException(
                message: "Content item dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    someContentItemId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ContentItem> hardRemoveContentItemByIdTask =
                this.contentItemService.HardRemoveContentItemByIdAsync(
                    someContentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemDependencyException actualContentItemDependencyException =
                await Assert.ThrowsAsync<ContentItemDependencyException>(
                    hardRemoveContentItemByIdTask.AsTask);

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
        public async Task ShouldThrowDependencyExceptionOnHardRemoveByIdIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            Guid someContentItemId = Guid.NewGuid();
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
                broker.SelectContentItemByIdAsync(
                    someContentItemId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ContentItem> hardRemoveContentItemByIdTask =
                this.contentItemService.HardRemoveContentItemByIdAsync(
                    someContentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemDependencyException actualContentItemDependencyException =
                await Assert.ThrowsAsync<ContentItemDependencyException>(
                    hardRemoveContentItemByIdTask.AsTask);

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
        public async Task ShouldThrowOperationCanceledExceptionOnHardRemoveByIdIfCancellationRequestedAsync()
        {
            // given
            Guid someContentItemId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ContentItem> hardRemoveContentItemByIdTask =
                this.contentItemService.HardRemoveContentItemByIdAsync(
                    someContentItemId,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                hardRemoveContentItemByIdTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnHardRemoveByIdIfSqlErrorOccursAndLogItAsync()
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
            ValueTask<ContentItem> hardRemoveContentItemByIdTask =
                this.contentItemService.HardRemoveContentItemByIdAsync(
                    someContentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemDependencyException actualContentItemDependencyException =
                await Assert.ThrowsAsync<ContentItemDependencyException>(
                    hardRemoveContentItemByIdTask.AsTask);

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
        public async Task ShouldThrowDependencyValidationExceptionOnHardRemoveByIdIfDbUpdateConcurrencyExceptionOccursAndLogItAsync()
        {
            // given
            Guid someContentItemId = Guid.NewGuid();
            ContentItem someContentItem = CreateRandomContentItem();
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();

            var lockedContentItemException = new LockedContentItemException(
                message: "Locked content item record, please try again later.",
                innerException: dbUpdateConcurrencyException,
                data: dbUpdateConcurrencyException.Data);

            var expectedContentItemDependencyValidationException = new ContentItemDependencyValidationException(
                message: "Content item dependency validation error occurred, fix the errors and try again.",
                innerException: lockedContentItemException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    someContentItemId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(someContentItem);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteContentItemAsync(
                    someContentItem,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(dbUpdateConcurrencyException);

            // when
            ValueTask<ContentItem> hardRemoveContentItemByIdTask =
                this.contentItemService.HardRemoveContentItemByIdAsync(
                    someContentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemDependencyValidationException actualContentItemDependencyValidationException =
                await Assert.ThrowsAsync<ContentItemDependencyValidationException>(
                    hardRemoveContentItemByIdTask.AsTask);

            // then
            actualContentItemDependencyValidationException.Should().BeEquivalentTo(
                expectedContentItemDependencyValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(
                    someContentItemId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteContentItemAsync(
                    someContentItem,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnHardRemoveByIdIfServiceErrorOccursAndLogItAsync()
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
            ValueTask<ContentItem> hardRemoveContentItemByIdTask =
                this.contentItemService.HardRemoveContentItemByIdAsync(
                    someContentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemServiceException actualContentItemServiceException =
                await Assert.ThrowsAsync<ContentItemServiceException>(
                    hardRemoveContentItemByIdTask.AsTask);

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
    }
}
