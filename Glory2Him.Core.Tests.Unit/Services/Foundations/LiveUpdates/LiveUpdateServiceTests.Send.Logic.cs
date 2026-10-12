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
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.LiveUpdates
{
    public partial class LiveUpdateServiceTests
    {
        [Fact]
        public async Task ShouldSendAContentItemSettingChangedUpdateAsync()
        {
            // given
            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            var inputLiveUpdate = new LiveUpdate
            {
                Type = LiveUpdateType.ContentItemSettingChanged,
                ContentItemId = null
            };

            // when
            await this.liveUpdateService.SendLiveUpdateAsync(
                inputLiveUpdate,
                cancellationToken);

            // then
            this.liveUpdateBrokerMock.Verify(broker =>
                broker.SendLiveUpdateAsync(inputLiveUpdate, cancellationToken),
                Times.Once);

            this.liveUpdateBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSendAContentItemReactionsChangedUpdateAsync()
        {
            // given
            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            var inputLiveUpdate = new LiveUpdate
            {
                Type = LiveUpdateType.ContentItemReactionsChanged,
                ContentItemId = Guid.NewGuid()
            };

            // when
            await this.liveUpdateService.SendLiveUpdateAsync(
                inputLiveUpdate,
                cancellationToken);

            // then
            this.liveUpdateBrokerMock.Verify(broker =>
                broker.SendLiveUpdateAsync(inputLiveUpdate, cancellationToken),
                Times.Once);

            this.liveUpdateBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
