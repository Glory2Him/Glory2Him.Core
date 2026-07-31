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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Orchestrations.ContentItems.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItems
{
    public partial class ContentItemOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnAddingContentItemEventIfCancellationRequestedAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();

            EventEnvelope<ContentItem> requestEnvelope = CreateEventEnvelope(
                contentItem: randomContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<EventEnvelope<ContentItem>?> onAddingTask =
                this.contentItemOrchestrationService.OnAddingContentItemAsync(
                    requestEnvelope,
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(onAddingTask.AsTask);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddingContentItemEventIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            var operationCanceledException = new OperationCanceledException();

            EventEnvelope<ContentItem> requestEnvelope = CreateEventEnvelope(
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

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(It.IsAny<string>()))
                    .ReturnsAsync(GetRandomString());

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onAddingTask =
                this.contentItemOrchestrationService.OnAddingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationDependencyException actualContentItemOrchestrationDependencyException =
                await Assert.ThrowsAsync<ContentItemOrchestrationDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualContentItemOrchestrationDependencyException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddingContentItemEventIfErrorOccursAndLogItAsync(
            Xeption dependencyValidationException)
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();

            EventEnvelope<ContentItem> requestEnvelope = CreateEventEnvelope(
                contentItem: randomContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedContentItemOrchestrationDependencyValidationException =
                new ContentItemOrchestrationDependencyValidationException(
                    message: "Content item orchestration dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (dependencyValidationException.InnerException as Xeption)!);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(It.IsAny<string>()))
                    .ReturnsAsync(GetRandomString());

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyValidationException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onAddingTask =
                this.contentItemOrchestrationService.OnAddingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationDependencyValidationException
                actualContentItemOrchestrationDependencyValidationException =
                    await Assert.ThrowsAsync<ContentItemOrchestrationDependencyValidationException>(
                        onAddingTask.AsTask);

            // then
            actualContentItemOrchestrationDependencyValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddingContentItemEventIfDependencyErrorOccursAndLogItAsync(
            Xeption dependencyException)
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();

            EventEnvelope<ContentItem> requestEnvelope = CreateEventEnvelope(
                contentItem: randomContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedContentItemOrchestrationDependencyException =
                new ContentItemOrchestrationDependencyException(
                    message: "Content item orchestration dependency error occurred, contact support.",
                    innerException: (dependencyException.InnerException as Xeption)!);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(It.IsAny<string>()))
                    .ReturnsAsync(GetRandomString());

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onAddingTask =
                this.contentItemOrchestrationService.OnAddingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationDependencyException actualContentItemOrchestrationDependencyException =
                await Assert.ThrowsAsync<ContentItemOrchestrationDependencyException>(
                    onAddingTask.AsTask);

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
        public async Task ShouldThrowServiceExceptionOnAddingContentItemEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            var serviceException = new Exception("Service error occurred.");

            EventEnvelope<ContentItem> requestEnvelope = CreateEventEnvelope(
                contentItem: randomContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var failedContentItemOrchestrationServiceException =
                new FailedContentItemOrchestrationServiceException(
                    message: "Failed content item orchestration service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedContentItemOrchestrationServiceException =
                new ContentItemOrchestrationServiceException(
                    message: "Content item orchestration service error occurred, contact support.",
                    innerException: failedContentItemOrchestrationServiceException);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(It.IsAny<string>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onAddingTask =
                this.contentItemOrchestrationService.OnAddingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationServiceException actualContentItemOrchestrationServiceException =
                await Assert.ThrowsAsync<ContentItemOrchestrationServiceException>(
                    onAddingTask.AsTask);

            // then
            actualContentItemOrchestrationServiceException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationServiceException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
