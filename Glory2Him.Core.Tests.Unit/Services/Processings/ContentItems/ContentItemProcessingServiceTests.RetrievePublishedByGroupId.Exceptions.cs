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
using Glory2Him.Core.Models.Processings.ContentItems.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.ContentItems
{
    public partial class ContentItemProcessingServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnRetrievePublishedByGroupIdIfDependencyValidationErrorOccursAndLogItAsync(
            Xeption dependencyValidationException)
        {
            // given
            Guid randomGroupId = Guid.NewGuid();
            Guid inputGroupId = randomGroupId;
            ContentItem randomContentItem = CreateRandomContentItem();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: randomContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedContentItemProcessingDependencyValidationException =
                new ContentItemProcessingDependencyValidationException(
                    message: "Content item processing dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (dependencyValidationException.InnerException as Xeption)!);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyValidationException);

            // when
            ValueTask<ContentItem> retrievePublishedContentItemByGroupIdTask =
                this.contentItemProcessingService.RetrievePublishedContentItemByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingDependencyValidationException
                actualContentItemProcessingDependencyValidationException =
                    await Assert.ThrowsAsync<ContentItemProcessingDependencyValidationException>(
                        retrievePublishedContentItemByGroupIdTask.AsTask);

            // then
            actualContentItemProcessingDependencyValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingDependencyValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRetrievePublishedByGroupIdIfDependencyErrorOccursAndLogItAsync(
            Xeption dependencyException)
        {
            // given
            Guid randomGroupId = Guid.NewGuid();
            Guid inputGroupId = randomGroupId;
            ContentItem randomContentItem = CreateRandomContentItem();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: randomContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedContentItemProcessingDependencyException =
                new ContentItemProcessingDependencyException(
                    message: "Content item processing dependency error occurred, contact support.",
                    innerException: (dependencyException.InnerException as Xeption)!);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ValueTask<ContentItem> retrievePublishedContentItemByGroupIdTask =
                this.contentItemProcessingService.RetrievePublishedContentItemByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingDependencyException actualContentItemProcessingDependencyException =
                await Assert.ThrowsAsync<ContentItemProcessingDependencyException>(
                    retrievePublishedContentItemByGroupIdTask.AsTask);

            // then
            actualContentItemProcessingDependencyException.Should().BeEquivalentTo(
                expectedContentItemProcessingDependencyException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrievePublishedByGroupIdIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given: an OperationCanceledException without a cancellation request is a
            // dependency timeout, not a caller cancellation
            Guid randomGroupId = Guid.NewGuid();
            Guid inputGroupId = randomGroupId;
            ContentItem randomContentItem = CreateRandomContentItem();
            var operationCanceledException = new OperationCanceledException();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: randomContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutContentItemProcessingException =
                new TimeoutContentItemProcessingException(
                    message: "Failed content item processing timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedContentItemProcessingDependencyException =
                new ContentItemProcessingDependencyException(
                    message: "Content item processing dependency error occurred, contact support.",
                    innerException: timeoutContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ContentItem> retrievePublishedContentItemByGroupIdTask =
                this.contentItemProcessingService.RetrievePublishedContentItemByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingDependencyException actualContentItemProcessingDependencyException =
                await Assert.ThrowsAsync<ContentItemProcessingDependencyException>(
                    retrievePublishedContentItemByGroupIdTask.AsTask);

            // then
            actualContentItemProcessingDependencyException.Should().BeEquivalentTo(
                expectedContentItemProcessingDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRetrievePublishedByGroupIdIfCancellationRequestedAsync()
        {
            // given
            Guid randomGroupId = Guid.NewGuid();
            Guid inputGroupId = randomGroupId;
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<ContentItem> retrievePublishedContentItemByGroupIdTask =
                this.contentItemProcessingService.RetrievePublishedContentItemByGroupIdAsync(
                    inputGroupId,
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(retrievePublishedContentItemByGroupIdTask.AsTask);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrievePublishedByGroupIdIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Guid randomGroupId = Guid.NewGuid();
            Guid inputGroupId = randomGroupId;
            var serviceException = new Exception("Service error occurred.");

            var failedContentItemProcessingServiceException =
                new FailedContentItemProcessingServiceException(
                    message: "Failed content item processing service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedContentItemProcessingServiceException =
                new ContentItemProcessingServiceException(
                    message: "Content item processing service error occurred, contact support.",
                    innerException: failedContentItemProcessingServiceException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<ContentItem> retrievePublishedContentItemByGroupIdTask =
                this.contentItemProcessingService.RetrievePublishedContentItemByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingServiceException actualContentItemProcessingServiceException =
                await Assert.ThrowsAsync<ContentItemProcessingServiceException>(
                    retrievePublishedContentItemByGroupIdTask.AsTask);

            // then
            actualContentItemProcessingServiceException.Should().BeEquivalentTo(
                expectedContentItemProcessingServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingServiceException))),
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
