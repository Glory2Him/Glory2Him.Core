// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems;

namespace Glory2Him.Core.Brokers.Events
{
    public partial interface IEventBroker
    {
        ValueTask PublishContentItemAsync(EventEnvelope<ContentItem> envelope, string? eventName = null);

        void SubscribeToContentItemEvent(
            Func<EventEnvelope<ContentItem>, ValueTask> contentItemEventHandler,
            string? eventName = null);
    }
}
