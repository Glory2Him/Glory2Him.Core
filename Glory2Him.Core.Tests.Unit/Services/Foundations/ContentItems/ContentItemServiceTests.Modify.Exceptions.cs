// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
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

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnModifyIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            ContentItem someContentItem = CreateRandomContentItem();
            var operationCanceledException = new OperationCanceledException();

            var timeoutContentItemException = new TimeoutContentItemException(
                message: "Content item timed out, contact support.",
                innerException: new TimeoutException(),
                data: operationCanceledException.Data);

            var expectedContentItemDependencyException = new ContentItemDependencyException(
                message: "Content item dependency error occurred, contact support.",
                innerException: timeoutContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someContentItem))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    someContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemDependencyException actualContentItemDependencyException =
                await Assert.ThrowsAsync<ContentItemDependencyException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemDependencyException.Should().BeEquivalentTo(
                expectedContentItemDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someContentItem),
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
        public async Task ShouldThrowOperationCanceledExceptionOnModifyIfCancellationRequestedAndLogItAsync()
        {
            // given
            ContentItem someContentItem = CreateRandomContentItem();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    someContentItem,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                modifyContentItemTask.AsTask);

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
            ContentItem someContentItem = CreateRandomContentItem();
            SqlException sqlException = GetSqlException();

            var failedStorageContentItemException = new FailedStorageContentItemException(
                message: "Failed content item storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedContentItemDependencyException = new ContentItemDependencyException(
                message: "Content item dependency error occurred, contact support.",
                innerException: failedStorageContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someContentItem))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    someContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemDependencyException actualContentItemDependencyException =
                await Assert.ThrowsAsync<ContentItemDependencyException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemDependencyException.Should().BeEquivalentTo(
                expectedContentItemDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someContentItem),
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
        public async Task ShouldThrowDependencyValidationExceptionOnModifyIfDbUpdateConcurrencyExceptionOccursAndLogItAsync()
        {
            // given
            ContentItem someContentItem = CreateRandomContentItem();
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();

            var lockedContentItemException = new LockedContentItemException(
                message: "Locked content item record, please try again later.",
                innerException: dbUpdateConcurrencyException,
                data: dbUpdateConcurrencyException.Data);

            var expectedContentItemDependencyValidationException = new ContentItemDependencyValidationException(
                message: "Content item dependency validation error occurred, fix the errors and try again.",
                innerException: lockedContentItemException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someContentItem))
                    .ThrowsAsync(dbUpdateConcurrencyException);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    someContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemDependencyValidationException actualContentItemDependencyValidationException =
                await Assert.ThrowsAsync<ContentItemDependencyValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemDependencyValidationException.Should().BeEquivalentTo(
                expectedContentItemDependencyValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someContentItem),
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
    }
}
