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
        public async Task ShouldThrowDependencyValidationExceptionOnAddIfDependencyValidationErrorOccursAndLogItAsync(
            Xeption dependencyValidationException)
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedContentItemOrchestrationDependencyValidationException =
                new ContentItemOrchestrationDependencyValidationException(
                    message: "Content item orchestration dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (dependencyValidationException.InnerException as Xeption)!);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(It.IsAny<string>()))
                    .ReturnsAsync(GetRandomString());

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Enumerable.Empty<ContentItem>().AsQueryable());

            this.contentItemServiceMock.Setup(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyValidationException);

            // when
            ValueTask<ContentItem> addContentItemTask =
                this.contentItemOrchestrationService.AddContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationDependencyValidationException
                actualContentItemOrchestrationDependencyValidationException =
                    await Assert.ThrowsAsync<ContentItemOrchestrationDependencyValidationException>(
                        addContentItemTask.AsTask);

            // then
            actualContentItemOrchestrationDependencyValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationDependencyValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddIfDependencyErrorOccursAndLogItAsync(
            Xeption dependencyException)
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedContentItemOrchestrationDependencyException =
                new ContentItemOrchestrationDependencyException(
                    message: "Content item orchestration dependency error occurred, contact support.",
                    innerException: (dependencyException.InnerException as Xeption)!);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(It.IsAny<string>()))
                    .ReturnsAsync(GetRandomString());

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ValueTask<ContentItem> addContentItemTask =
                this.contentItemOrchestrationService.AddContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationDependencyException actualContentItemOrchestrationDependencyException =
                await Assert.ThrowsAsync<ContentItemOrchestrationDependencyException>(
                    addContentItemTask.AsTask);

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
        public async Task ShouldThrowDependencyExceptionOnAddIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            var operationCanceledException = new OperationCanceledException();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
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
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(It.IsAny<string>()))
                    .ReturnsAsync(GetRandomString());

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ContentItem> addContentItemTask =
                this.contentItemOrchestrationService.AddContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationDependencyException actualContentItemOrchestrationDependencyException =
                await Assert.ThrowsAsync<ContentItemOrchestrationDependencyException>(
                    addContentItemTask.AsTask);

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
        public async Task ShouldThrowOperationCanceledExceptionOnAddIfCancellationRequestedAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<ContentItem> addContentItemTask =
                this.contentItemOrchestrationService.AddContentItemAsync(
                    inputContentItem,
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(addContentItemTask.AsTask);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnAddIfServiceErrorOccursAndLogItAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
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
                broker.CreateAsync(inputContentItem))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<ContentItem> addContentItemTask =
                this.contentItemOrchestrationService.AddContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationServiceException actualContentItemOrchestrationServiceException =
                await Assert.ThrowsAsync<ContentItemOrchestrationServiceException>(
                    addContentItemTask.AsTask);

            // then
            actualContentItemOrchestrationServiceException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationServiceException))),
                Times.Once);

            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
