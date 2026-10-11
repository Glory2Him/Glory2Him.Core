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
using Glory2Him.Core.Models.Foundations.LiveUpdates;
using Glory2Him.Core.Models.Foundations.LiveUpdates.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.LiveUpdates
{
    public partial class LiveUpdateServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnSendIfTokenIsCancelledAsync()
        {
            // given
            var cancellationToken = new CancellationToken(canceled: true);

            var someLiveUpdate = new LiveUpdate
            {
                Type = LiveUpdateType.ContentItemSettingChanged,
                ContentItemId = null
            };

            // when
            ValueTask sendLiveUpdateTask =
                this.liveUpdateService.SendLiveUpdateAsync(
                    someLiveUpdate,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                sendLiveUpdateTask.AsTask);

            this.liveUpdateBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnSendIfTimeoutOccursAndLogItAsync()
        {
            // given
            var someLiveUpdate = new LiveUpdate
            {
                Type = LiveUpdateType.ContentItemSettingChanged,
                ContentItemId = null
            };

            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutLiveUpdateException =
                new TimeoutLiveUpdateException(
                    message: "Failed live update timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedLiveUpdateDependencyException =
                new LiveUpdateDependencyException(
                    message: "Live update dependency error occurred, contact support.",
                    innerException: timeoutLiveUpdateException);

            this.liveUpdateBrokerMock.Setup(broker =>
                broker.SendLiveUpdateAsync(
                    It.IsAny<LiveUpdate>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask sendLiveUpdateTask =
                this.liveUpdateService.SendLiveUpdateAsync(
                    someLiveUpdate,
                    TestContext.Current.CancellationToken);

            LiveUpdateDependencyException actualLiveUpdateDependencyException =
                await Assert.ThrowsAsync<LiveUpdateDependencyException>(
                    sendLiveUpdateTask.AsTask);

            // then
            actualLiveUpdateDependencyException.Should().BeEquivalentTo(
                expectedLiveUpdateDependencyException);

            this.liveUpdateBrokerMock.Verify(broker =>
                broker.SendLiveUpdateAsync(
                    It.IsAny<LiveUpdate>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLiveUpdateDependencyException))),
                Times.Once);

            this.liveUpdateBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnSendIfServiceErrorOccursAndLogItAsync()
        {
            // given
            var someLiveUpdate = new LiveUpdate
            {
                Type = LiveUpdateType.ContentItemSettingChanged,
                ContentItemId = null
            };

            var serviceException = new Exception();

            var failedLiveUpdateServiceException =
                new FailedLiveUpdateServiceException(
                    message: "Failed live update service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedLiveUpdateServiceException =
                new LiveUpdateServiceException(
                    message: "Live update service error occurred, contact support.",
                    innerException: failedLiveUpdateServiceException);

            this.liveUpdateBrokerMock.Setup(broker =>
                broker.SendLiveUpdateAsync(
                    It.IsAny<LiveUpdate>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask sendLiveUpdateTask =
                this.liveUpdateService.SendLiveUpdateAsync(
                    someLiveUpdate,
                    TestContext.Current.CancellationToken);

            LiveUpdateServiceException actualLiveUpdateServiceException =
                await Assert.ThrowsAsync<LiveUpdateServiceException>(
                    sendLiveUpdateTask.AsTask);

            // then
            actualLiveUpdateServiceException.Should().BeEquivalentTo(
                expectedLiveUpdateServiceException);

            this.liveUpdateBrokerMock.Verify(broker =>
                broker.SendLiveUpdateAsync(
                    It.IsAny<LiveUpdate>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLiveUpdateServiceException))),
                Times.Once);

            this.liveUpdateBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
