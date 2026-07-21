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
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRemoveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            Guid someTagId = Guid.NewGuid();

            var expectedTagDependencyException = new TagDependencyException(
                message: "Tag dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<Tag> removeTagByIdTask =
                this.tagService.RemoveTagByIdAsync(
                    someTagId,
                    cancellationToken: TestContext.Current.CancellationToken);

            TagDependencyException actualTagDependencyException =
                await Assert.ThrowsAsync<TagDependencyException>(
                    removeTagByIdTask.AsTask);

            // then
            actualTagDependencyException.Should().BeEquivalentTo(
                expectedTagDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRemoveByIdIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            Guid someTagId = Guid.NewGuid();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutTagException =
                new TimeoutTagException(
                    message: "Failed tag timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedTagDependencyException = new TagDependencyException(
                message: "Tag dependency error occurred, contact support.",
                innerException: timeoutTagException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<Tag> removeTagByIdTask =
                this.tagService.RemoveTagByIdAsync(
                    someTagId,
                    cancellationToken: TestContext.Current.CancellationToken);

            TagDependencyException actualTagDependencyException =
                await Assert.ThrowsAsync<TagDependencyException>(
                    removeTagByIdTask.AsTask);

            // then
            actualTagDependencyException.Should().BeEquivalentTo(
                expectedTagDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRemoveByIdIfCancellationRequestedAsync()
        {
            // given
            Guid someTagId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<Tag> removeTagByIdTask =
                this.tagService.RemoveTagByIdAsync(
                    someTagId,
                    cancellationToken: cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                removeTagByIdTask.AsTask);

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
            Guid someTagId = Guid.NewGuid();
            SqlException sqlException = GetSqlException();

            var failedStorageTagException = new FailedStorageTagException(
                message: "Failed tag storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedTagDependencyException = new TagDependencyException(
                message: "Tag dependency error occurred, contact support.",
                innerException: failedStorageTagException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<Tag> removeTagByIdTask =
                this.tagService.RemoveTagByIdAsync(
                    someTagId,
                    cancellationToken: TestContext.Current.CancellationToken);

            TagDependencyException actualTagDependencyException =
                await Assert.ThrowsAsync<TagDependencyException>(
                    removeTagByIdTask.AsTask);

            // then
            actualTagDependencyException.Should().BeEquivalentTo(
                expectedTagDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedTagDependencyException))),
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
            Guid someTagId = Guid.NewGuid();
            Tag someTag = CreateRandomTag();
            someTag.IsDeleted = false;
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();

            var lockedTagException = new LockedTagException(
                message: "Locked tag record, please try again later.",
                innerException: dbUpdateConcurrencyException,
                data: dbUpdateConcurrencyException.Data);

            var expectedTagDependencyValidationException = new TagDependencyValidationException(
                message: "Tag dependency validation error occurred, fix the errors and try again.",
                innerException: lockedTagException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(someTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(someTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(someTag);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateTagAsync(
                    someTag,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(dbUpdateConcurrencyException);

            // when
            ValueTask<Tag> removeTagByIdTask =
                this.tagService.RemoveTagByIdAsync(
                    someTagId,
                    cancellationToken: TestContext.Current.CancellationToken);

            TagDependencyValidationException actualTagDependencyValidationException =
                await Assert.ThrowsAsync<TagDependencyValidationException>(
                    removeTagByIdTask.AsTask);

            // then
            actualTagDependencyValidationException.Should().BeEquivalentTo(
                expectedTagDependencyValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(someTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateTagAsync(
                    someTag,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagDependencyValidationException))),
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
            Guid someTagId = Guid.NewGuid();
            var serviceException = new Exception();

            var failedTagServiceException = new FailedTagServiceException(
                message: "Failed tag service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedTagServiceException = new TagServiceException(
                message: "Tag service error occurred, contact support.",
                innerException: failedTagServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<Tag> removeTagByIdTask =
                this.tagService.RemoveTagByIdAsync(
                    someTagId,
                    cancellationToken: TestContext.Current.CancellationToken);

            TagServiceException actualTagServiceException =
                await Assert.ThrowsAsync<TagServiceException>(
                    removeTagByIdTask.AsTask);

            // then
            actualTagServiceException.Should().BeEquivalentTo(
                expectedTagServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
