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
using Glory2Him.Core.Models.Foundations.Reactions;

namespace Glory2Him.Core.Brokers.Events
{
    public partial class EventBroker
    {
        public ValueTask<EventPublishResult<Reaction>> PublishReactionAsync(
            EventEnvelope<Reaction> envelope,
            ReactionEventOperation operation) =>
                PublishEventAsync(
                    EventBrokerIdentifiers.ReactionEventAddressIds,
                    nameof(Reaction),
                    envelope,
                    operation);

        public ValueTask SubscribeToReactionEventAsync(
            EventSubscription subscription,
            ReactionEventOperation operation,
            Func<EventEnvelope<Reaction>, CancellationToken, ValueTask> reactionEventHandler,
            CancellationToken cancellationToken = default) =>
                SubscribeToEventAsync(
                    EventBrokerIdentifiers.ReactionEventAddressIds,
                    subscription,
                    operation,
                    reactionEventHandler,
                    cancellationToken);

        public ValueTask SubscribeToReactionEventAsync(
            EventSubscription subscription,
            ReactionEventOperation operation,
            Func<EventEnvelope<Reaction>, CancellationToken, ValueTask<EventEnvelope<Reaction>?>> reactionEventHandler,
            CancellationToken cancellationToken = default) =>
                SubscribeToEventAsync(
                    EventBrokerIdentifiers.ReactionEventAddressIds,
                    subscription,
                    operation,
                    reactionEventHandler,
                    cancellationToken);
    }
}
