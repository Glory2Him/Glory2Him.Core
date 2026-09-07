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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Orchestrations.ContentItemSettings.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItemSettings
{
    // THE COLLECTION READ HAS ITS OWN WRAPPER. TryCatchQueryable is a separate method from the
    // TryCatch every other operation shares, so the Add tests pin nothing about it. It was rewritten
    // wholesale — two validation clauses retired, the service exception re-categorised from 424 to
    // 500 — with no test on the path at all, which meant the exact silent status regression that
    // rewrite existed to correct could have been reintroduced with the suite green.
    public partial class ContentItemSettingOrchestrationServiceTests
    {
        [Theory]
        [MemberData(nameof(ContentItemSettingDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRetrieveAllIfFoundationDependencyErrorOccursAndLogItAsync(
            Xeption foundationException)
        {
            // given
            var expectedException =
                new ContentItemSettingOrchestrationDependencyException(
                    message: "Content item setting orchestration dependency error occurred, contact support.",
                    innerException: (foundationException.InnerException as Xeption)
                        ?? foundationException);

            this.contentItemSettingServiceMock.Setup(service =>
                service.RetrieveAllContentItemSettingsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(foundationException);

            // when
            ValueTask<IQueryable<ContentItemSetting>> retrieveAllTask =
                this.contentItemSettingOrchestrationService.RetrieveAllContentItemSettingsAsync(
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationDependencyException>(
                    retrieveAllTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.contentItemSettingServiceMock.Verify(service =>
                service.RetrieveAllContentItemSettingsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A SERVICE failure answers 500 HERE TOO. This is the assertion whose absence let the
        // collection endpoint's category be changed — in either direction — without a test noticing.
        [Theory]
        [MemberData(nameof(ContentItemSettingServiceExceptions))]
        public async Task ShouldThrowServiceExceptionOnRetrieveAllIfFoundationServiceErrorOccursAndLogItAsync(
            Xeption foundationException)
        {
            // given
            var expectedException =
                new ContentItemSettingOrchestrationServiceException(
                    message: "Content item setting orchestration service error occurred, contact support.",
                    innerException: foundationException);

            this.contentItemSettingServiceMock.Setup(service =>
                service.RetrieveAllContentItemSettingsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(foundationException);

            // when
            ValueTask<IQueryable<ContentItemSetting>> retrieveAllTask =
                this.contentItemSettingOrchestrationService.RetrieveAllContentItemSettingsAsync(
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationServiceException>(
                    retrieveAllTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.contentItemSettingServiceMock.Verify(service =>
                service.RetrieveAllContentItemSettingsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A TIMEOUT IS A DEPENDENCY PROBLEM THAT STILL READS AS A TIMEOUT. The wrapper is kept whole
        // rather than unwrapped, so a call site can tell a slow store from an unreachable one.
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveAllIfTheOperationTimesOutAndLogItAsync()
        {
            // given: the default constructor leaves CancellationToken at None, whose
            // IsCancellationRequested is false — the timeout half of the filter
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutContentItemSettingOrchestrationException =
                new TimeoutContentItemSettingOrchestrationException(
                    message: "Failed content item setting orchestration timeout error occurred, "
                        + "contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedException =
                new ContentItemSettingOrchestrationDependencyException(
                    message: "Content item setting orchestration dependency error occurred, contact support.",
                    innerException: timeoutContentItemSettingOrchestrationException);

            this.contentItemSettingServiceMock.Setup(service =>
                service.RetrieveAllContentItemSettingsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when: a live token is handed in, so only the thrown exception's own token can decide
            // the branch
            ValueTask<IQueryable<ContentItemSetting>> retrieveAllTask =
                this.contentItemSettingOrchestrationService.RetrieveAllContentItemSettingsAsync(
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationDependencyException>(
                    retrieveAllTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.contentItemSettingServiceMock.Verify(service =>
                service.RetrieveAllContentItemSettingsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A GENUINE CANCELLATION IS NOT A TIMEOUT and must reach the caller untouched — the mirror
        // of the test above, thrown from the same dependency, so the token is the only difference.
        [Fact]
        public async Task ShouldRethrowOperationCanceledExceptionOnRetrieveAllIfItsTokenWasCancelledAsync()
        {
            // given
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            var operationCanceledException =
                new OperationCanceledException(cancellationTokenSource.Token);

            this.contentItemSettingServiceMock.Setup(service =>
                service.RetrieveAllContentItemSettingsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<IQueryable<ContentItemSetting>> retrieveAllTask =
                this.contentItemSettingOrchestrationService.RetrieveAllContentItemSettingsAsync(
                    TestContext.Current.CancellationToken);

            // then: the original exception, not a wrapper, and nothing logged
            await Assert.ThrowsAsync<OperationCanceledException>(retrieveAllTask.AsTask);

            this.contentItemSettingServiceMock.Verify(service =>
                service.RetrieveAllContentItemSettingsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // THE GENERAL HANDLER. Anything that is not one of the shapes above becomes a service
        // failure wrapped in FailedContentItemSettingOrchestrationServiceException — a type no test
        // referenced at all before this one, on a path where the new clause structure decides what
        // can still reach it.
        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrieveAllIfAnUnexpectedErrorOccursAndLogItAsync()
        {
            // given
            string randomMessage = GetRandomString();
            var serviceException = new Exception(randomMessage);

            var failedContentItemSettingOrchestrationServiceException =
                new FailedContentItemSettingOrchestrationServiceException(
                    message: "Failed content item setting orchestration service error occurred, "
                        + "contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedException =
                new ContentItemSettingOrchestrationServiceException(
                    message: "Content item setting orchestration service error occurred, contact support.",
                    innerException: failedContentItemSettingOrchestrationServiceException);

            this.contentItemSettingServiceMock.Setup(service =>
                service.RetrieveAllContentItemSettingsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<IQueryable<ContentItemSetting>> retrieveAllTask =
                this.contentItemSettingOrchestrationService.RetrieveAllContentItemSettingsAsync(
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationServiceException>(
                    retrieveAllTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.contentItemSettingServiceMock.Verify(service =>
                service.RetrieveAllContentItemSettingsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
