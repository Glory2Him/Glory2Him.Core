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
using Glory2Him.Core.Models.Foundations.Comments;

namespace Glory2Him.Core.Brokers.Events
{
    public partial class EventBroker
    {
        public ValueTask<EventPublishResult<Comment>> PublishCommentAsync(
            EventEnvelope<Comment> envelope,
            CommentEventOperation operation) =>
                PublishEventAsync(
                    eventAddressIds: EventBrokerIdentifiers.CommentEventAddressIds,
                    entityName: nameof(Comment),
                    envelope: envelope,
                    operation: operation);

        public ValueTask SubscribeToCommentEventAsync(
            EventSubscription subscription,
            CommentEventOperation operation,
            Func<EventEnvelope<Comment>, CancellationToken,
                ValueTask> commentEventHandler,
            CancellationToken cancellationToken = default) =>
                SubscribeToEventAsync(
                    eventAddressIds: EventBrokerIdentifiers.CommentEventAddressIds,
                    subscription: subscription,
                    operation: operation,
                    eventHandler: commentEventHandler,
                    cancellationToken: cancellationToken);

        public ValueTask SubscribeToCommentEventAsync(
            EventSubscription subscription,
            CommentEventOperation operation,
            Func<EventEnvelope<Comment>, CancellationToken,
                ValueTask<EventEnvelope<Comment>?>> commentEventHandler,
            CancellationToken cancellationToken = default) =>
                SubscribeToEventAsync(
                    eventAddressIds: EventBrokerIdentifiers.CommentEventAddressIds,
                    subscription: subscription,
                    operation: operation,
                    eventHandler: commentEventHandler,
                    cancellationToken: cancellationToken);
    }
}
