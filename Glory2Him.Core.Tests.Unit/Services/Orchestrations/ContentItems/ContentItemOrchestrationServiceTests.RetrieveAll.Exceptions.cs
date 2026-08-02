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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Orchestrations.ContentItems.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItems
{
    public partial class ContentItemOrchestrationServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnRetrieveAllIfDependencyValidationErrorOccursAndLogItAsync(
            Xeption dependencyValidationException)
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: randomContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedContentItemOrchestrationDependencyValidationException =
                new ContentItemOrchestrationDependencyValidationException(
                    message: "Content item orchestration dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (dependencyValidationException.InnerException as Xeption)!);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveAllRequest())))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyValidationException);

            // when
            ValueTask<IQueryable<ContentItem>> retrieveAllContentItemsTask =
                this.contentItemOrchestrationService.RetrieveAllContentItemsAsync(
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationDependencyValidationException
                actualContentItemOrchestrationDependencyValidationException =
                    await Assert.ThrowsAsync<ContentItemOrchestrationDependencyValidationException>(
                        retrieveAllContentItemsTask.AsTask);

            // then
            actualContentItemOrchestrationDependencyValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationDependencyValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRetrieveAllIfDependencyErrorOccursAndLogItAsync(
            Xeption dependencyException)
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: randomContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedContentItemOrchestrationDependencyException =
                new ContentItemOrchestrationDependencyException(
                    message: "Content item orchestration dependency error occurred, contact support.",
                    innerException: (dependencyException.InnerException as Xeption)!);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveAllRequest())))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ValueTask<IQueryable<ContentItem>> retrieveAllContentItemsTask =
                this.contentItemOrchestrationService.RetrieveAllContentItemsAsync(
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationDependencyException actualContentItemOrchestrationDependencyException =
                await Assert.ThrowsAsync<ContentItemOrchestrationDependencyException>(
                    retrieveAllContentItemsTask.AsTask);

            // then
            actualContentItemOrchestrationDependencyException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationDependencyException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveAllIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given: an OperationCanceledException without a cancellation request is a
            // dependency timeout, not a caller cancellation
            ContentItem randomContentItem = CreateRandomContentItem();
            var operationCanceledException = new OperationCanceledException();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: randomContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutContentItemOrchestrationException =
                new TimeoutContentItemOrchestrationException(
                    message: "Failed content item orchestration timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedContentItemOrchestrationDependencyException =
                new ContentItemOrchestrationDependencyException(
                    message: "Content item orchestration dependency error occurred, contact support.",
                    innerException: timeoutContentItemOrchestrationException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveAllRequest())))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<IQueryable<ContentItem>> retrieveAllContentItemsTask =
                this.contentItemOrchestrationService.RetrieveAllContentItemsAsync(
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationDependencyException actualContentItemOrchestrationDependencyException =
                await Assert.ThrowsAsync<ContentItemOrchestrationDependencyException>(
                    retrieveAllContentItemsTask.AsTask);

            // then
            actualContentItemOrchestrationDependencyException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationDependencyException))),
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
            ValueTask<IQueryable<ContentItem>> retrieveAllContentItemsTask =
                this.contentItemOrchestrationService.RetrieveAllContentItemsAsync(
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(retrieveAllContentItemsTask.AsTask);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrieveAllIfServiceErrorOccursAndLogItAsync()
        {
            // given
            var serviceException = new Exception("Service error occurred.");

            var failedContentItemOrchestrationServiceException =
                new FailedContentItemOrchestrationServiceException(
                    message: "Failed content item orchestration service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedContentItemOrchestrationServiceException =
                new ContentItemOrchestrationServiceException(
                    message: "Content item orchestration service error occurred, contact support.",
                    innerException: failedContentItemOrchestrationServiceException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveAllRequest())))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<IQueryable<ContentItem>> retrieveAllContentItemsTask =
                this.contentItemOrchestrationService.RetrieveAllContentItemsAsync(
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationServiceException actualContentItemOrchestrationServiceException =
                await Assert.ThrowsAsync<ContentItemOrchestrationServiceException>(
                    retrieveAllContentItemsTask.AsTask);

            // then
            actualContentItemOrchestrationServiceException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationServiceException))),
                Times.Once);

            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
