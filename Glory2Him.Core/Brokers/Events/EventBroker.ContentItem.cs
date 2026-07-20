// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems;

namespace Glory2Him.Core.Brokers.Events
{
    public partial class EventBroker
    {
        public ValueTask<EventPublishResult<ContentItem>> PublishContentItemAsync(
            EventEnvelope<ContentItem> envelope,
            ContentItemEventOperation operation) =>
                PublishEventAsync(
                    EventBrokerIdentifiers.ContentItemEventAddressIds,
                    nameof(ContentItem),
                    envelope,
                    operation);

        public ValueTask SubscribeToContentItemEventAsync(
            EventSubscription subscription,
            ContentItemEventOperation operation,
            Func<EventEnvelope<ContentItem>, CancellationToken, ValueTask> contentItemEventHandler,
            CancellationToken cancellationToken = default) =>
                SubscribeToEventAsync(
                    EventBrokerIdentifiers.ContentItemEventAddressIds,
                    subscription,
                    operation,
                    contentItemEventHandler,
                    cancellationToken);

        public ValueTask SubscribeToContentItemEventAsync(
            EventSubscription subscription,
            ContentItemEventOperation operation,
            Func<EventEnvelope<ContentItem>, CancellationToken, ValueTask<EventEnvelope<ContentItem>?>> contentItemEventHandler,
            CancellationToken cancellationToken = default) =>
                SubscribeToEventAsync(
                    EventBrokerIdentifiers.ContentItemEventAddressIds,
                    subscription,
                    operation,
                    contentItemEventHandler,
                    cancellationToken);
    }
}
