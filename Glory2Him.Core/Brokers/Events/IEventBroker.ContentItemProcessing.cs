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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.ContentItems;

namespace Glory2Him.Core.Brokers.Events
{
    internal partial interface IEventBroker
    {
        ValueTask<EventPublishResult<ContentItem>> PublishContentItemProcessingAsync(
            EventEnvelope<ContentItem> envelope,
            ContentItemProcessingEventOperation operation);

        ValueTask SubscribeToContentItemProcessingEventAsync(
            EventSubscription subscription,
            ContentItemProcessingEventOperation operation,
            Func<EventEnvelope<ContentItem>, CancellationToken,
                ValueTask> contentItemProcessingEventHandler,
            CancellationToken cancellationToken = default);

        ValueTask SubscribeToContentItemProcessingEventAsync(
            EventSubscription subscription,
            ContentItemProcessingEventOperation operation,
            Func<EventEnvelope<ContentItem>, CancellationToken,
                ValueTask<EventEnvelope<ContentItem>?>> contentItemProcessingEventHandler,
            CancellationToken cancellationToken = default);
    }
}
