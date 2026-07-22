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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnHardRemoveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            Guid someLinkId = Guid.NewGuid();

            var expectedLinkDependencyException = new LinkDependencyException(
                message: "Link dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<Link> hardRemoveLinkByIdTask =
                this.linkService.HardRemoveLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken);

            LinkDependencyException actualLinkDependencyException =
                await Assert.ThrowsAsync<LinkDependencyException>(
                    hardRemoveLinkByIdTask.AsTask);

            // then
            actualLinkDependencyException.Should().BeEquivalentTo(
                expectedLinkDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkDependencyException))),
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
            Guid someLinkId = Guid.NewGuid();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutLinkException =
                new TimeoutLinkException(
                    message: "Failed link timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedLinkDependencyException = new LinkDependencyException(
                message: "Link dependency error occurred, contact support.",
                innerException: timeoutLinkException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<Link> hardRemoveLinkByIdTask =
                this.linkService.HardRemoveLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken);

            LinkDependencyException actualLinkDependencyException =
                await Assert.ThrowsAsync<LinkDependencyException>(
                    hardRemoveLinkByIdTask.AsTask);

            // then
            actualLinkDependencyException.Should().BeEquivalentTo(
                expectedLinkDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkDependencyException))),
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
            Guid someLinkId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<Link> hardRemoveLinkByIdTask =
                this.linkService.HardRemoveLinkByIdAsync(
                    someLinkId,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                hardRemoveLinkByIdTask.AsTask);

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
            Guid someLinkId = Guid.NewGuid();
            SqlException sqlException = GetSqlException();

            var failedStorageLinkException = new FailedStorageLinkException(
                message: "Failed link storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedLinkDependencyException = new LinkDependencyException(
                message: "Link dependency error occurred, contact support.",
                innerException: failedStorageLinkException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<Link> hardRemoveLinkByIdTask =
                this.linkService.HardRemoveLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken);

            LinkDependencyException actualLinkDependencyException =
                await Assert.ThrowsAsync<LinkDependencyException>(
                    hardRemoveLinkByIdTask.AsTask);

            // then
            actualLinkDependencyException.Should().BeEquivalentTo(
                expectedLinkDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedLinkDependencyException))),
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
            Guid someLinkId = Guid.NewGuid();
            Link someLink = CreateRandomLink();
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();

            var lockedLinkException = new LockedLinkException(
                message: "Locked link record, please try again later.",
                innerException: dbUpdateConcurrencyException,
                data: dbUpdateConcurrencyException.Data);

            var expectedLinkDependencyValidationException = new LinkDependencyValidationException(
                message: "Link dependency validation error occurred, fix the errors and try again.",
                innerException: lockedLinkException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(someLink);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteLinkAsync(
                    someLink,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(dbUpdateConcurrencyException);

            // when
            ValueTask<Link> hardRemoveLinkByIdTask =
                this.linkService.HardRemoveLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken);

            LinkDependencyValidationException actualLinkDependencyValidationException =
                await Assert.ThrowsAsync<LinkDependencyValidationException>(
                    hardRemoveLinkByIdTask.AsTask);

            // then
            actualLinkDependencyValidationException.Should().BeEquivalentTo(
                expectedLinkDependencyValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteLinkAsync(
                    someLink,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkDependencyValidationException))),
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
            Guid someLinkId = Guid.NewGuid();
            var serviceException = new Exception();

            var failedLinkServiceException = new FailedLinkServiceException(
                message: "Failed link service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedLinkServiceException = new LinkServiceException(
                message: "Link service error occurred, contact support.",
                innerException: failedLinkServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<Link> hardRemoveLinkByIdTask =
                this.linkService.HardRemoveLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken);

            LinkServiceException actualLinkServiceException =
                await Assert.ThrowsAsync<LinkServiceException>(
                    hardRemoveLinkByIdTask.AsTask);

            // then
            actualLinkServiceException.Should().BeEquivalentTo(
                expectedLinkServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
