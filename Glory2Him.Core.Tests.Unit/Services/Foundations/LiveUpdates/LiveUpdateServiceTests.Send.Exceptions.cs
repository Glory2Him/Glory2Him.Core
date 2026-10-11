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
using Glory2Him.Core.Models.Foundations.LiveUpdates;

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
    }
}
