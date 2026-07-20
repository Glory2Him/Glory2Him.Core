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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;

namespace Glory2Him.Core.Brokers.Events
{
    public partial class EventBroker
    {
        public ValueTask<EventPublishResult<Link>> PublishLinkAsync(
            EventEnvelope<Link> envelope,
            LinkEventOperation operation) =>
                PublishEventAsync(
                    EventBrokerIdentifiers.LinkEventAddressIds,
                    nameof(Link),
                    envelope,
                    operation);

        public ValueTask SubscribeToLinkEventAsync(
            EventSubscription subscription,
            LinkEventOperation operation,
            Func<EventEnvelope<Link>, CancellationToken,
                ValueTask> linkEventHandler,
            CancellationToken cancellationToken = default) =>
                SubscribeToEventAsync(
                    EventBrokerIdentifiers.LinkEventAddressIds,
                    subscription,
                    operation,
                    linkEventHandler,
                    cancellationToken);

        public ValueTask SubscribeToLinkEventAsync(
            EventSubscription subscription,
            LinkEventOperation operation,
            Func<EventEnvelope<Link>, CancellationToken,
                ValueTask<EventEnvelope<Link>?>> linkEventHandler,
            CancellationToken cancellationToken = default) =>
                SubscribeToEventAsync(
                    EventBrokerIdentifiers.LinkEventAddressIds,
                    subscription,
                    operation,
                    linkEventHandler,
                    cancellationToken);
    }
}
