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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
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
        public async Task ShouldThrowDependencyValidationExceptionOnRetrieveFeedIfDependencyValidationErrorOccursAndLogItAsync(
            Xeption dependencyValidationException)
        {
            // given
            var expectedContentItemProcessingDependencyValidationException =
                new ContentItemProcessingDependencyValidationException(
                    message: "Content item processing dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (dependencyValidationException.InnerException as Xeption)!);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemFeedAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyValidationException);

            // when
            ValueTask<IReadOnlyList<ContentItem>> retrieveContentItemFeedTask =
                this.contentItemProcessingService.RetrieveContentItemFeedAsync(
                    skip: 0,
                    take: 10,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentItemProcessingDependencyValidationException
                actualContentItemProcessingDependencyValidationException =
                    await Assert.ThrowsAsync<ContentItemProcessingDependencyValidationException>(
                        retrieveContentItemFeedTask.AsTask);

            // then
            actualContentItemProcessingDependencyValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRetrieveFeedIfSqlErrorOccursAsync(
            Xeption dependencyException)
        {
            // given: the storage fault the foundation already categorised, on its way to the
            // route's 424. The outward exception carries the inner categorisation and NOT the
            // reason, the row state or the caller's identity (§SEC14.5 rule 2) — the true
            // reason was logged server-side when the foundation raised it.
            var expectedContentItemProcessingDependencyException =
                new ContentItemProcessingDependencyException(
                    message: "Content item processing dependency error occurred, contact support.",
                    innerException: (dependencyException.InnerException as Xeption)!);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemFeedAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ValueTask<IReadOnlyList<ContentItem>> retrieveContentItemFeedTask =
                this.contentItemProcessingService.RetrieveContentItemFeedAsync(
                    skip: 0,
                    take: 10,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentItemProcessingDependencyException actualContentItemProcessingDependencyException =
                await Assert.ThrowsAsync<ContentItemProcessingDependencyException>(
                    retrieveContentItemFeedTask.AsTask);

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
        public async Task ShouldThrowDependencyExceptionOnRetrieveFeedIfTimeoutOccursAsync()
        {
            // given
            var operationCanceledException = new OperationCanceledException();

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

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemFeedAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<IReadOnlyList<ContentItem>> retrieveContentItemFeedTask =
                this.contentItemProcessingService.RetrieveContentItemFeedAsync(
                    skip: 0,
                    take: 10,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentItemProcessingDependencyException actualContentItemProcessingDependencyException =
                await Assert.ThrowsAsync<ContentItemProcessingDependencyException>(
                    retrieveContentItemFeedTask.AsTask);

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
        public async Task ShouldThrowOperationCanceledExceptionOnRetrieveFeedIfTokenIsCancelledAsync()
        {
            // given: UNWRAPPED, never dressed as a dependency or a service fault
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<IReadOnlyList<ContentItem>> retrieveContentItemFeedTask =
                this.contentItemProcessingService.RetrieveContentItemFeedAsync(
                    skip: 0,
                    take: 10,
                    cancellationToken: cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                retrieveContentItemFeedTask.AsTask);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrieveFeedIfServiceErrorOccursAsync()
        {
            // given
            var serviceException = new Exception("Service error occurred.");

            var failedContentItemProcessingServiceException =
                new FailedContentItemProcessingServiceException(
                    message: "Failed content item processing service error occurred, " +
                        "please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedContentItemProcessingServiceException =
                new ContentItemProcessingServiceException(
                    message: "Content item processing service error occurred, contact support.",
                    innerException: failedContentItemProcessingServiceException);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemFeedAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<IReadOnlyList<ContentItem>> retrieveContentItemFeedTask =
                this.contentItemProcessingService.RetrieveContentItemFeedAsync(
                    skip: 0,
                    take: 10,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentItemProcessingServiceException actualContentItemProcessingServiceException =
                await Assert.ThrowsAsync<ContentItemProcessingServiceException>(
                    retrieveContentItemFeedTask.AsTask);

            // then
            actualContentItemProcessingServiceException.Should().BeEquivalentTo(
                expectedContentItemProcessingServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingServiceException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
