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
using Glory2Him.Core.Models.Foundations.Tags;

namespace Glory2Him.Core.Brokers.Events
{
    public partial class EventBroker
    {
        public ValueTask<EventPublishResult<Tag>> PublishTagAsync(
            EventEnvelope<Tag> envelope,
            TagEventOperation operation) =>
                PublishEventAsync(
                    EventBrokerIdentifiers.TagEventAddressIds,
                    nameof(Tag),
                    envelope,
                    operation);

        public ValueTask SubscribeToTagEventAsync(
            EventSubscription subscription,
            TagEventOperation operation,
            Func<EventEnvelope<Tag>, CancellationToken, ValueTask> tagEventHandler,
            CancellationToken cancellationToken = default) =>
                SubscribeToEventAsync(
                    EventBrokerIdentifiers.TagEventAddressIds,
                    subscription,
                    operation,
                    tagEventHandler,
                    cancellationToken);

        public ValueTask SubscribeToTagEventAsync(
            EventSubscription subscription,
            TagEventOperation operation,
            Func<EventEnvelope<Tag>, CancellationToken, ValueTask<EventEnvelope<Tag>?>> tagEventHandler,
            CancellationToken cancellationToken = default) =>
                SubscribeToEventAsync(
                    EventBrokerIdentifiers.TagEventAddressIds,
                    subscription,
                    operation,
                    tagEventHandler,
                    cancellationToken);
    }
}
