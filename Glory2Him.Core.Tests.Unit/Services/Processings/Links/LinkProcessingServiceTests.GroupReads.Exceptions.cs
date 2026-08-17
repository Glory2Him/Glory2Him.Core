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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Glory2Him.Core.Models.Processings.Links.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.Links
{
    public partial class LinkProcessingServiceTests
    {
        // The two single-row group reads share one exception surface: both resolve their row
        // out of the same foundation collection read, so a dependency failure reaches the
        // caller by the same path. They are parameterised together rather than copied, so a
        // divergence between the two shows up as a failure instead of as a stale duplicate.
        private ValueTask<Link> RetrieveByGroupIdAsync(
            bool retrieveLatest,
            Guid groupId,
            CancellationToken cancellationToken) =>
            retrieveLatest
                ? this.linkProcessingService.RetrieveLatestLinkByGroupIdAsync(
                    groupId,
                    cancellationToken)

                : this.linkProcessingService.RetrievePublishedLinkByGroupIdAsync(
                    groupId,
                    cancellationToken);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldThrowDependencyValidationExceptionOnGroupReadIfValidationErrorOccursAndLogItAsync(
            bool retrieveLatest)
        {
            // given
            Guid inputGroupId = Guid.NewGuid();
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            var dependencyValidationException = new LinkValidationException(
                message: randomMessage,
                innerException: innerException);

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { GroupId = inputGroupId },
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedLinkProcessingDependencyValidationException =
                new LinkProcessingDependencyValidationException(
                    message: "Link processing dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: innerException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyValidationException);

            // when
            ValueTask<Link> groupReadTask = RetrieveByGroupIdAsync(
                retrieveLatest,
                inputGroupId,
                TestContext.Current.CancellationToken);

            LinkProcessingDependencyValidationException
                actualLinkProcessingDependencyValidationException =
                    await Assert.ThrowsAsync<LinkProcessingDependencyValidationException>(
                        groupReadTask.AsTask);

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
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldThrowDependencyExceptionOnGroupReadIfDependencyErrorOccursAndLogItAsync(
            bool retrieveLatest)
        {
            // given
            Guid inputGroupId = Guid.NewGuid();
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            var dependencyException = new LinkDependencyException(
                message: randomMessage,
                innerException: innerException);

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { GroupId = inputGroupId },
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedLinkProcessingDependencyException =
                new LinkProcessingDependencyException(
                    message: "Link processing dependency error occurred, contact support.",
                    innerException: innerException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ValueTask<Link> groupReadTask = RetrieveByGroupIdAsync(
                retrieveLatest,
                inputGroupId,
                TestContext.Current.CancellationToken);

            LinkProcessingDependencyException actualLinkProcessingDependencyException =
                await Assert.ThrowsAsync<LinkProcessingDependencyException>(
                    groupReadTask.AsTask);

            // then
            actualLinkProcessingDependencyException.Should().BeEquivalentTo(
                expectedLinkProcessingDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldThrowDependencyExceptionOnGroupReadIfOperationCanceledExceptionOccursAndLogItAsync(
            bool retrieveLatest)
        {
            // given
            Guid inputGroupId = Guid.NewGuid();
            var operationCanceledException = new OperationCanceledException();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { GroupId = inputGroupId },
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
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<Link> groupReadTask = RetrieveByGroupIdAsync(
                retrieveLatest,
                inputGroupId,
                TestContext.Current.CancellationToken);

            LinkProcessingDependencyException actualLinkProcessingDependencyException =
                await Assert.ThrowsAsync<LinkProcessingDependencyException>(
                    groupReadTask.AsTask);

            // then
            actualLinkProcessingDependencyException.Should().BeEquivalentTo(
                expectedLinkProcessingDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldThrowServiceExceptionOnGroupReadIfServiceErrorOccursAndLogItAsync(
            bool retrieveLatest)
        {
            // given
            Guid inputGroupId = Guid.NewGuid();
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
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<Link> groupReadTask = RetrieveByGroupIdAsync(
                retrieveLatest,
                inputGroupId,
                TestContext.Current.CancellationToken);

            LinkProcessingServiceException actualLinkProcessingServiceException =
                await Assert.ThrowsAsync<LinkProcessingServiceException>(
                    groupReadTask.AsTask);

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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldThrowOperationCanceledExceptionOnGroupReadIfCancellationRequestedAsync(
            bool retrieveLatest)
        {
            // given
            Guid inputGroupId = Guid.NewGuid();
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<Link> groupReadTask = RetrieveByGroupIdAsync(
                retrieveLatest,
                inputGroupId,
                cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(groupReadTask.AsTask);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
