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
        public async Task ShouldThrowValidationExceptionOnSendIfLiveUpdateIsNullAndLogItAsync()
        {
            // given
            LiveUpdate nullLiveUpdate = null;

            var nullLiveUpdateException =
                new NullLiveUpdateException(message: "Live update is null.");

            var expectedLiveUpdateValidationException =
                new LiveUpdateValidationException(
                    message: "Live update validation error occurred, fix the errors and try again.",
                    innerException: nullLiveUpdateException);

            // when
            ValueTask sendLiveUpdateTask =
                this.liveUpdateService.SendLiveUpdateAsync(
                    nullLiveUpdate,
                    TestContext.Current.CancellationToken);

            LiveUpdateValidationException actualLiveUpdateValidationException =
                await Assert.ThrowsAsync<LiveUpdateValidationException>(
                    sendLiveUpdateTask.AsTask);

            // then
            actualLiveUpdateValidationException.Should().BeEquivalentTo(
                expectedLiveUpdateValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLiveUpdateValidationException))),
                Times.Once);

            this.liveUpdateBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
