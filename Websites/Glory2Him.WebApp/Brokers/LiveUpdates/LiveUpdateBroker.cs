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

using Glory2Him.Core.Brokers.LiveUpdates;
using Glory2Him.Core.Models.Foundations.LiveUpdates;
using Glory2Him.WebApp.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Glory2Him.WebApp.Brokers.LiveUpdates
{
    internal sealed class LiveUpdateBroker : ILiveUpdateBroker
    {
        private readonly IHubContext<LiveUpdatesHub> hubContext;

        public LiveUpdateBroker(IHubContext<LiveUpdatesHub> hubContext) =>
            this.hubContext = hubContext;

        // Started, never awaited: the send's task completes only once every connection has
        // taken the update, so awaiting it would let a reader who stops reading hold the
        // write that caused it (LiveUpdateBroker.md §1 rule 2).
        public ValueTask SendLiveUpdateAsync(
            LiveUpdate liveUpdate,
            CancellationToken cancellationToken = default)
        {
            _ = this.hubContext.Clients.All.SendAsync(
                "ReceiveLiveUpdate",
                liveUpdate,
                cancellationToken);

            return ValueTask.CompletedTask;
        }
    }
}
