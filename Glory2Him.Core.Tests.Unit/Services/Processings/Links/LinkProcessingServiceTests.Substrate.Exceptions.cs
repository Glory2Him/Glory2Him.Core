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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Processings.Links.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.Links
{
    public partial class LinkProcessingServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task
            ShouldThrowDependencyValidationExceptionOnModifyingLinkIfDependencyValidationErrorOccursAndLogItAsync(
            Xeption dependencyValidationException)
        {
            // given
            Link inputLink = CreateRandomLink();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedLinkProcessingDependencyValidationException =
                new LinkProcessingDependencyValidationException(
                    message: "Link processing dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (dependencyValidationException.InnerException as Xeption)!);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyValidationException);

            // when
            ValueTask<EventEnvelope<Link>?> onModifyingLinkTask =
                this.linkProcessingService.OnModifyingLinkAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            LinkProcessingDependencyValidationException
                actualLinkProcessingDependencyValidationException =
                    await Assert.ThrowsAsync<LinkProcessingDependencyValidationException>(
                        onModifyingLinkTask.AsTask);

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
        public async Task ShouldThrowDependencyExceptionOnAddingLinkIfDependencyErrorOccursAndLogItAsync(
            Xeption dependencyException)
        {
            // given
            Link inputLink = CreateRandomLink();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedLinkProcessingDependencyException =
                new LinkProcessingDependencyException(
                    message: "Link processing dependency error occurred, contact support.",
                    innerException: (dependencyException.InnerException as Xeption)!);

            this.linkServiceMock.Setup(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ValueTask<EventEnvelope<Link>?> onAddingLinkTask =
                this.linkProcessingService.OnAddingLinkAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            LinkProcessingDependencyException actualLinkProcessingDependencyException =
                await Assert.ThrowsAsync<LinkProcessingDependencyException>(
                    onAddingLinkTask.AsTask);

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
        public async Task ShouldThrowDependencyExceptionOnRemovingLinkByIdIfOperationCanceledOccursAndLogItAsync()
        {
            // given
            Guid inputLinkId = Guid.NewGuid();
            var operationCanceledException = new OperationCanceledException();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { Id = inputLinkId },
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

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLinkId, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<Link>?> onRemovingLinkTask =
                this.linkProcessingService.OnRemovingLinkByIdAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            LinkProcessingDependencyException actualLinkProcessingDependencyException =
                await Assert.ThrowsAsync<LinkProcessingDependencyException>(
                    onRemovingLinkTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnRetrievingLinkByIdIfSoftDeletedAndLogItAsync()
        {
            // given: the event path applies the same read posture as the direct call — a
            // removed row is not found for every caller
            Guid inputLinkId = Guid.NewGuid();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            Link storageLink = CreateRandomDeletedLink(currentDateTime);
            storageLink.Id = inputLinkId;

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { Id = inputLinkId },
                securityContext: CreateAuthenticatedSecurityContext(Roles.Administrators));

            var notFoundLinkProcessingException =
                new NotFoundLinkProcessingException(message: "The link was not found.");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: notFoundLinkProcessingException);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLinkId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            // when
            ValueTask<EventEnvelope<Link>?> onRetrievingLinkTask =
                this.linkProcessingService.OnRetrievingLinkByIdAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    onRetrievingLinkTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnAddingLinkIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Link inputLink = CreateRandomLink();
            var serviceException = new Exception("Service error occurred.");

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: CreateAuthenticatedSecurityContext());

            var failedLinkProcessingServiceException =
                new FailedLinkProcessingServiceException(
                    message: "Failed link processing service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedLinkProcessingServiceException =
                new LinkProcessingServiceException(
                    message: "Link processing service error occurred, contact support.",
                    innerException: failedLinkProcessingServiceException);

            this.linkServiceMock.Setup(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<Link>?> onAddingLinkTask =
                this.linkProcessingService.OnAddingLinkAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            LinkProcessingServiceException actualLinkProcessingServiceException =
                await Assert.ThrowsAsync<LinkProcessingServiceException>(
                    onAddingLinkTask.AsTask);

            // then
            actualLinkProcessingServiceException.Should().BeEquivalentTo(
                expectedLinkProcessingServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingServiceException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
