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
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Glory2Him.Core.Models.Securities;
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
        public async Task ShouldThrowDependencyExceptionOnHardRemoveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
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
            ValueTask<Tag> hardRemoveTagByIdTask =
                this.tagService.HardRemoveTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken);

            TagDependencyException actualTagDependencyException =
                await Assert.ThrowsAsync<TagDependencyException>(
                    hardRemoveTagByIdTask.AsTask);

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
        public async Task ShouldThrowDependencyExceptionOnHardRemoveByIdIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
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
            ValueTask<Tag> hardRemoveTagByIdTask =
                this.tagService.HardRemoveTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken);

            TagDependencyException actualTagDependencyException =
                await Assert.ThrowsAsync<TagDependencyException>(
                    hardRemoveTagByIdTask.AsTask);

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
        public async Task ShouldThrowOperationCanceledExceptionOnHardRemoveByIdIfCancellationRequestedAsync()
        {
            // given
            Guid someTagId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<Tag> hardRemoveTagByIdTask =
                this.tagService.HardRemoveTagByIdAsync(
                    someTagId,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                hardRemoveTagByIdTask.AsTask);

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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
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
            ValueTask<Tag> hardRemoveTagByIdTask =
                this.tagService.HardRemoveTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken);

            TagDependencyException actualTagDependencyException =
                await Assert.ThrowsAsync<TagDependencyException>(
                    hardRemoveTagByIdTask.AsTask);

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
        public async Task ShouldThrowDependencyValidationExceptionOnHardRemoveByIdIfDbUpdateConcurrencyExceptionOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someTagId = Guid.NewGuid();
            Tag someTag = CreateRandomTag();
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

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteTagAsync(
                    someTag,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(dbUpdateConcurrencyException);

            // when
            ValueTask<Tag> hardRemoveTagByIdTask =
                this.tagService.HardRemoveTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken);

            TagDependencyValidationException actualTagDependencyValidationException =
                await Assert.ThrowsAsync<TagDependencyValidationException>(
                    hardRemoveTagByIdTask.AsTask);

            // then
            actualTagDependencyValidationException.Should().BeEquivalentTo(
                expectedTagDependencyValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteTagAsync(
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
        public async Task ShouldThrowServiceExceptionOnHardRemoveByIdIfServiceErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
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
            ValueTask<Tag> hardRemoveTagByIdTask =
                this.tagService.HardRemoveTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken);

            TagServiceException actualTagServiceException =
                await Assert.ThrowsAsync<TagServiceException>(
                    hardRemoveTagByIdTask.AsTask);

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
