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
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveByIdIfOperationCanceledExceptionOccursAndLogItAsync()
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
            ValueTask<Link> retrieveLinkByIdTask =
                this.linkService.RetrieveLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken);

            LinkDependencyException actualLinkDependencyException =
                await Assert.ThrowsAsync<LinkDependencyException>(
                    retrieveLinkByIdTask.AsTask);

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
        public async Task ShouldThrowCriticalDependencyExceptionOnRetrieveByIdIfSqlErrorOccursAndLogItAsync()
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
            ValueTask<Link> retrieveLinkByIdTask =
                this.linkService.RetrieveLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken);

            LinkDependencyException actualLinkDependencyException =
                await Assert.ThrowsAsync<LinkDependencyException>(
                    retrieveLinkByIdTask.AsTask);

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
        public async Task ShouldThrowOperationCanceledExceptionOnRetrieveByIdIfCancellationRequestedAsync()
        {
            // given
            Guid someLinkId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<Link> retrieveLinkByIdTask =
                this.linkService.RetrieveLinkByIdAsync(
                    someLinkId,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                retrieveLinkByIdTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrieveByIdIfServiceErrorOccursAndLogItAsync()
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
            ValueTask<Link> retrieveLinkByIdTask =
                this.linkService.RetrieveLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken);

            LinkServiceException actualLinkServiceException =
                await Assert.ThrowsAsync<LinkServiceException>(
                    retrieveLinkByIdTask.AsTask);

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
