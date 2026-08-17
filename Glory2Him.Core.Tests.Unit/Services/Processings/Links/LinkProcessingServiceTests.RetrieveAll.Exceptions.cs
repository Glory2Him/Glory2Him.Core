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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Processings.Links.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.Links
{
    public partial class LinkProcessingServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task
            ShouldThrowDependencyValidationExceptionOnRetrieveAllIfDependencyValidationErrorOccursAndLogItAsync(
            Xeption dependencyValidationException)
        {
            // given
            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link(),
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedLinkProcessingDependencyValidationException =
                new LinkProcessingDependencyValidationException(
                    message: "Link processing dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (dependencyValidationException.InnerException as Xeption)!);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveAllRequest())))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyValidationException);

            // when
            ValueTask<IQueryable<Link>> retrieveAllLinksTask =
                this.linkProcessingService.RetrieveAllLinksAsync(
                    TestContext.Current.CancellationToken);

            LinkProcessingDependencyValidationException
                actualLinkProcessingDependencyValidationException =
                    await Assert.ThrowsAsync<LinkProcessingDependencyValidationException>(
                        retrieveAllLinksTask.AsTask);

            // then
            actualLinkProcessingDependencyValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRetrieveAllIfDependencyErrorOccursAndLogItAsync(
            Xeption dependencyException)
        {
            // given
            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link(),
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedLinkProcessingDependencyException =
                new LinkProcessingDependencyException(
                    message: "Link processing dependency error occurred, contact support.",
                    innerException: (dependencyException.InnerException as Xeption)!);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveAllRequest())))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ValueTask<IQueryable<Link>> retrieveAllLinksTask =
                this.linkProcessingService.RetrieveAllLinksAsync(
                    TestContext.Current.CancellationToken);

            LinkProcessingDependencyException actualLinkProcessingDependencyException =
                await Assert.ThrowsAsync<LinkProcessingDependencyException>(
                    retrieveAllLinksTask.AsTask);

            // then
            actualLinkProcessingDependencyException.Should().BeEquivalentTo(
                expectedLinkProcessingDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveAllIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            var operationCanceledException = new OperationCanceledException();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link(),
                securityContext: CreateAuthenticatedSecurityContext());

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutLinkProcessingException =
                new TimeoutLinkProcessingException(
                    message: "Failed link processing timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedLinkProcessingDependencyException =
                new LinkProcessingDependencyException(
                    message: "Link processing dependency error occurred, contact support.",
                    innerException: timeoutLinkProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveAllRequest())))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<IQueryable<Link>> retrieveAllLinksTask =
                this.linkProcessingService.RetrieveAllLinksAsync(
                    TestContext.Current.CancellationToken);

            LinkProcessingDependencyException actualLinkProcessingDependencyException =
                await Assert.ThrowsAsync<LinkProcessingDependencyException>(
                    retrieveAllLinksTask.AsTask);

            // then
            actualLinkProcessingDependencyException.Should().BeEquivalentTo(
                expectedLinkProcessingDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRetrieveAllIfCancellationRequestedAsync()
        {
            // given
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<IQueryable<Link>> retrieveAllLinksTask =
                this.linkProcessingService.RetrieveAllLinksAsync(
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(retrieveAllLinksTask.AsTask);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrieveAllIfServiceErrorOccursAndLogItAsync()
        {
            // given
            var serviceException = new Exception("Service error occurred.");

            var failedLinkProcessingServiceException =
                new FailedLinkProcessingServiceException(
                    message: "Failed link processing service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedLinkProcessingServiceException =
                new LinkProcessingServiceException(
                    message: "Link processing service error occurred, contact support.",
                    innerException: failedLinkProcessingServiceException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveAllRequest())))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<IQueryable<Link>> retrieveAllLinksTask =
                this.linkProcessingService.RetrieveAllLinksAsync(
                    TestContext.Current.CancellationToken);

            LinkProcessingServiceException actualLinkProcessingServiceException =
                await Assert.ThrowsAsync<LinkProcessingServiceException>(
                    retrieveAllLinksTask.AsTask);

            // then
            actualLinkProcessingServiceException.Should().BeEquivalentTo(
                expectedLinkProcessingServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingServiceException))),
                Times.Once);

            this.linkServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
