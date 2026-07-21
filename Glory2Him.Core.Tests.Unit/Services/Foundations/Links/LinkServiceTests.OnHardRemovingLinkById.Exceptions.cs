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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnHardRemovingLinkByIdEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<Link> requestEnvelope = CreateRandomLinkRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<Link>?> onHardRemovingTask =
                this.linkService.OnHardRemovingLinkByIdAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onHardRemovingTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnHardRemovingLinkByIdEventIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Link> requestEnvelope = CreateRandomLinkRequestEnvelope();
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
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.LinkOnHardRemovingLinkByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<Link>?> onHardRemovingTask =
                this.linkService.OnHardRemovingLinkByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            LinkDependencyException actualLinkDependencyException =
                await Assert.ThrowsAsync<LinkDependencyException>(
                    onHardRemovingTask.AsTask);

            // then
            actualLinkDependencyException.Should().BeEquivalentTo(
                expectedLinkDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnHardRemovingLinkByIdEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<Link> requestEnvelope = CreateRandomLinkRequestEnvelope();

            var expectedLinkDependencyException = new LinkDependencyException(
                message: "Link dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.LinkOnHardRemovingLinkByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<Link>?> onHardRemovingTask =
                this.linkService.OnHardRemovingLinkByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            LinkDependencyException actualLinkDependencyException =
                await Assert.ThrowsAsync<LinkDependencyException>(
                    onHardRemovingTask.AsTask);

            // then
            actualLinkDependencyException.Should().BeEquivalentTo(
                expectedLinkDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnHardRemovingLinkByIdEventIfSqlErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Link> requestEnvelope = CreateRandomLinkRequestEnvelope();
            SqlException sqlException = GetSqlException();

            var failedStorageLinkException = new FailedStorageLinkException(
                message: "Failed link storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedLinkDependencyException = new LinkDependencyException(
                message: "Link dependency error occurred, contact support.",
                innerException: failedStorageLinkException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.LinkOnHardRemovingLinkByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<EventEnvelope<Link>?> onHardRemovingTask =
                this.linkService.OnHardRemovingLinkByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            LinkDependencyException actualLinkDependencyException =
                await Assert.ThrowsAsync<LinkDependencyException>(
                    onHardRemovingTask.AsTask);

            // then
            actualLinkDependencyException.Should().BeEquivalentTo(
                expectedLinkDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedLinkDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ModifyDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnHardRemovingLinkByIdEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<Link> requestEnvelope = CreateRandomLinkRequestEnvelope();

            var expectedLinkDependencyValidationException = new LinkDependencyValidationException(
                message: "Link dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.LinkOnHardRemovingLinkByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<Link>?> onHardRemovingTask =
                this.linkService.OnHardRemovingLinkByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            LinkDependencyValidationException actualLinkDependencyValidationException =
                await Assert.ThrowsAsync<LinkDependencyValidationException>(
                    onHardRemovingTask.AsTask);

            // then
            actualLinkDependencyValidationException.Should().BeEquivalentTo(
                expectedLinkDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnHardRemovingLinkByIdEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Link> requestEnvelope = CreateRandomLinkRequestEnvelope();
            var serviceException = new Exception();

            var failedLinkServiceException = new FailedLinkServiceException(
                message: "Failed link service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedLinkServiceException = new LinkServiceException(
                message: "Link service error occurred, contact support.",
                innerException: failedLinkServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.LinkOnHardRemovingLinkByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<Link>?> onHardRemovingTask =
                this.linkService.OnHardRemovingLinkByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            LinkServiceException actualLinkServiceException =
                await Assert.ThrowsAsync<LinkServiceException>(
                    onHardRemovingTask.AsTask);

            // then
            actualLinkServiceException.Should().BeEquivalentTo(
                expectedLinkServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
