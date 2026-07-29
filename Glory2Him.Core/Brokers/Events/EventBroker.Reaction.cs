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
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Reactions;

namespace Glory2Him.Core.Brokers.Events
{
    internal partial class EventBroker
    {
        public ValueTask<EventPublishResult<Reaction>> PublishReactionAsync(
            EventEnvelope<Reaction> envelope,
            ReactionEventOperation operation) =>
                PublishEventAsync(
                    eventAddressIds: EventBrokerIdentifiers.ReactionEventAddressIds,
                    entityName: nameof(Reaction),
                    envelope: envelope,
                    operation: operation);

        public ValueTask SubscribeToReactionEventAsync(
            EventSubscription subscription,
            ReactionEventOperation operation,
            Func<EventEnvelope<Reaction>, CancellationToken,
                ValueTask> reactionEventHandler,
            CancellationToken cancellationToken = default) =>
                SubscribeToEventAsync(
                    eventAddressIds: EventBrokerIdentifiers.ReactionEventAddressIds,
                    subscription: subscription,
                    operation: operation,
                    eventHandler: reactionEventHandler,
                    cancellationToken: cancellationToken);

        public ValueTask SubscribeToReactionEventAsync(
            EventSubscription subscription,
            ReactionEventOperation operation,
            Func<EventEnvelope<Reaction>, CancellationToken,
                ValueTask<EventEnvelope<Reaction>?>> reactionEventHandler,
            CancellationToken cancellationToken = default) =>
                SubscribeToEventAsync(
                    eventAddressIds: EventBrokerIdentifiers.ReactionEventAddressIds,
                    subscription: subscription,
                    operation: operation,
                    eventHandler: reactionEventHandler,
                    cancellationToken: cancellationToken);
    }
}
