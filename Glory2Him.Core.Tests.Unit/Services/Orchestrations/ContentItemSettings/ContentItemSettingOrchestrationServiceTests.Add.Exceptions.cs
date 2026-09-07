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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Orchestrations.ContentItemSettings.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItemSettings
{
    public partial class ContentItemSettingOrchestrationServiceTests
    {
        // THE FOUNDATION'S ANSWERS KEEP THEIR CATEGORY. The exposer maps this service's exceptions
        // to the status codes it once mapped the foundation's to, so a validation failure that
        // arrived as a dependency failure — or the reverse — would silently move a 400 to a 424.
        [Theory]
        [MemberData(nameof(ContentItemSettingValidationExceptions))]
        public async Task ShouldThrowValidationExceptionOnAddIfFoundationValidationErrorOccursAndLogItAsync(
            Xeption foundationException)
        {
            // given
            ContentItemSetting someContentItemSetting = CreateRandomContentItemSetting();
            someContentItemSetting.ContentItemId = null;

            var expectedException =
                new ContentItemSettingOrchestrationValidationException(
                    message: "Content item setting orchestration validation error occurred, "
                        + "fix the errors and try again.",
                    innerException: (foundationException.InnerException as Xeption)
                        ?? foundationException);

            this.contentItemSettingServiceMock.Setup(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ContentItemSetting> addContentItemSettingTask =
                this.contentItemSettingOrchestrationService.AddContentItemSettingAsync(
                    someContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationValidationException>(
                    addContentItemSettingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            this.contentItemSettingServiceMock.Verify(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ContentItemSettingDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddIfFoundationDependencyValidationErrorOccursAndLogItAsync(
            Xeption foundationException)
        {
            // given
            ContentItemSetting someContentItemSetting = CreateRandomContentItemSetting();
            someContentItemSetting.ContentItemId = null;

            var expectedException =
                new ContentItemSettingOrchestrationDependencyValidationException(
                    message: "Content item setting orchestration dependency validation error occurred, "
                        + "fix the errors and try again.",
                    innerException: (foundationException.InnerException as Xeption)
                        ?? foundationException);

            this.contentItemSettingServiceMock.Setup(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ContentItemSetting> addContentItemSettingTask =
                this.contentItemSettingOrchestrationService.AddContentItemSettingAsync(
                    someContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationDependencyValidationException>(
                    addContentItemSettingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            this.contentItemSettingServiceMock.Verify(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ContentItemSettingDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddIfFoundationDependencyErrorOccursAndLogItAsync(
            Xeption foundationException)
        {
            // given
            ContentItemSetting someContentItemSetting = CreateRandomContentItemSetting();
            someContentItemSetting.ContentItemId = null;

            var expectedException =
                new ContentItemSettingOrchestrationDependencyException(
                    message: "Content item setting orchestration dependency error occurred, contact support.",
                    innerException: (foundationException.InnerException as Xeption)
                        ?? foundationException);

            this.contentItemSettingServiceMock.Setup(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ContentItemSetting> addContentItemSettingTask =
                this.contentItemSettingOrchestrationService.AddContentItemSettingAsync(
                    someContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationDependencyException>(
                    addContentItemSettingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            this.contentItemSettingServiceMock.Verify(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A SERVICE failure keeps its category and stays a 500. Routing it to the dependency
        // wrapper silently answered 424 instead — caught in review, pinned here.
        [Theory]
        [MemberData(nameof(ContentItemSettingServiceExceptions))]
        public async Task ShouldThrowServiceExceptionOnAddIfFoundationServiceErrorOccursAndLogItAsync(
            Xeption foundationException)
        {
            // given
            ContentItemSetting someContentItemSetting = CreateRandomContentItemSetting();
            someContentItemSetting.ContentItemId = null;

            var expectedException =
                new ContentItemSettingOrchestrationServiceException(
                    message: "Content item setting orchestration service error occurred, contact support.",
                    innerException: foundationException);

            this.contentItemSettingServiceMock.Setup(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ContentItemSetting> addContentItemSettingTask =
                this.contentItemSettingOrchestrationService.AddContentItemSettingAsync(
                    someContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationServiceException>(
                    addContentItemSettingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            this.contentItemSettingServiceMock.Verify(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // THE DERIVATION'S OWN READ CAN FAIL FOR REASONS THAT ARE NOT "NO SUCH ITEM". A store that
        // cannot answer must stay a DEPENDENCY problem and reach the caller as 424 — before the
        // `catch (Xeption)` clause existed these fell into the general handler and answered 500,
        // carrying a foreign foundation's exception out through the wrapper.
        [Theory]
        [MemberData(nameof(ContentItemDownstreamExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddIfResolvingTheContentItemFailsAndLogItAsync(
            Xeption downstreamException)
        {
            // given
            ContentItemSetting randomContentItemSetting = CreateRandomOverrideRequest();
            ContentItemSetting inputContentItemSetting = randomContentItemSetting;
            Guid contentItemId = inputContentItemSetting.ContentItemId.Value;

            var expectedException =
                new ContentItemSettingOrchestrationDependencyException(
                    message: "Content item setting orchestration dependency error occurred, contact support.",
                    innerException: (downstreamException.InnerException as Xeption)
                        ?? downstreamException);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(contentItemId, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(downstreamException);

            // when
            ValueTask<ContentItemSetting> addContentItemSettingTask =
                this.contentItemSettingOrchestrationService.AddContentItemSettingAsync(
                    inputContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationDependencyException>(
                    addContentItemSettingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(contentItemId, It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            // NOTHING WAS WRITTEN — the row never reached the settings foundation
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A TIMEOUT IS A DEPENDENCY PROBLEM THAT STILL READS AS A TIMEOUT. This wrapper is shared by
        // add, modify, retrieve-by-id, remove and hard-remove, so the clause is on every write path;
        // the wrapper is kept whole rather than unwrapped so a call site can tell a slow store from
        // an unreachable one. TimeoutContentItemSettingOrchestrationException was referenced by no
        // test at all before this one.
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddIfTheOperationTimesOutAndLogItAsync()
        {
            // given: the default constructor leaves CancellationToken at None, whose
            // IsCancellationRequested is false — the timeout half of the filter
            ContentItemSetting someContentItemSetting = CreateRandomContentItemSetting();
            someContentItemSetting.ContentItemId = null;

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
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when: a live token is handed in, so only the thrown exception's own token can decide
            // the branch
            ValueTask<ContentItemSetting> addContentItemSettingTask =
                this.contentItemSettingOrchestrationService.AddContentItemSettingAsync(
                    someContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationDependencyException>(
                    addContentItemSettingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            this.contentItemSettingServiceMock.Verify(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A GENUINE CANCELLATION IS NOT A TIMEOUT and must reach the caller untouched — the mirror
        // of the test above, thrown from the same dependency, so the token carried by the exception
        // is the only difference between the two runs.
        [Fact]
        public async Task ShouldRethrowOperationCanceledExceptionOnAddIfItsTokenWasCancelledAsync()
        {
            // given
            ContentItemSetting someContentItemSetting = CreateRandomContentItemSetting();
            someContentItemSetting.ContentItemId = null;

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            var operationCanceledException =
                new OperationCanceledException(cancellationTokenSource.Token);

            this.contentItemSettingServiceMock.Setup(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ContentItemSetting> addContentItemSettingTask =
                this.contentItemSettingOrchestrationService.AddContentItemSettingAsync(
                    someContentItemSetting,
                    TestContext.Current.CancellationToken);

            // then: the original exception, not a wrapper, and nothing logged
            await Assert.ThrowsAsync<OperationCanceledException>(addContentItemSettingTask.AsTask);

            this.contentItemSettingServiceMock.Verify(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // THE GENERAL HANDLER. Anything that is not one of the shapes above becomes a service
        // failure wrapped in FailedContentItemSettingOrchestrationServiceException — a type nothing
        // referenced before this test, on the path whose new catch (Xeption) clause decides what can
        // still reach it.
        [Fact]
        public async Task ShouldThrowServiceExceptionOnAddIfAnUnexpectedErrorOccursAndLogItAsync()
        {
            // given
            ContentItemSetting someContentItemSetting = CreateRandomContentItemSetting();
            someContentItemSetting.ContentItemId = null;

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
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ContentItemSetting> addContentItemSettingTask =
                this.contentItemSettingOrchestrationService.AddContentItemSettingAsync(
                    someContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationServiceException>(
                    addContentItemSettingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            this.contentItemSettingServiceMock.Verify(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
