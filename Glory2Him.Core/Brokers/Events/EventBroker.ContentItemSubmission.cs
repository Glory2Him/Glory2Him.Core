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
using Glory2Him.Core.Models.Events.Orchestrations;
using Glory2Him.Core.Models.Foundations.ContentItems;

namespace Glory2Him.Core.Brokers.Events
{
    internal partial class EventBroker
    {
        public ValueTask<EventPublishResult<ContentItem>> PublishContentItemSubmissionAsync(
            EventEnvelope<ContentItem> envelope,
            ContentItemSubmissionEventOperation operation) =>
                PublishEventAsync(
                    eventAddressIds: EventBrokerIdentifiers.ContentItemSubmissionEventAddressIds,
                    entityName: nameof(ContentItem),
                    envelope: envelope,
                    operation: operation);

        public ValueTask SubscribeToContentItemSubmissionEventAsync(
            EventSubscription subscription,
            ContentItemSubmissionEventOperation operation,
            Func<EventEnvelope<ContentItem>, CancellationToken,
                ValueTask> contentItemSubmissionEventHandler,
            CancellationToken cancellationToken = default) =>
                SubscribeToEventAsync(
                    eventAddressIds: EventBrokerIdentifiers.ContentItemSubmissionEventAddressIds,
                    subscription: subscription,
                    operation: operation,
                    eventHandler: contentItemSubmissionEventHandler,
                    cancellationToken: cancellationToken);

        public ValueTask SubscribeToContentItemSubmissionEventAsync(
            EventSubscription subscription,
            ContentItemSubmissionEventOperation operation,
            Func<EventEnvelope<ContentItem>, CancellationToken,
                ValueTask<EventEnvelope<ContentItem>?>> contentItemSubmissionEventHandler,
            CancellationToken cancellationToken = default) =>
                SubscribeToEventAsync(
                    eventAddressIds: EventBrokerIdentifiers.ContentItemSubmissionEventAddressIds,
                    subscription: subscription,
                    operation: operation,
                    eventHandler: contentItemSubmissionEventHandler,
                    cancellationToken: cancellationToken);
    }
}
