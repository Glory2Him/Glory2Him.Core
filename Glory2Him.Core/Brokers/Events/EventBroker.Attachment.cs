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
using Glory2Him.Core.Models.Foundations.Attachments;

namespace Glory2Him.Core.Brokers.Events
{
    internal partial class EventBroker
    {
        public ValueTask<EventPublishResult<Attachment>> PublishAttachmentAsync(
            EventEnvelope<Attachment> envelope,
            AttachmentEventOperation operation) =>
                PublishEventAsync(
                    eventAddressIds: EventBrokerIdentifiers.AttachmentEventAddressIds,
                    entityName: nameof(Attachment),
                    envelope: envelope,
                    operation: operation);

        public ValueTask SubscribeToAttachmentEventAsync(
            EventSubscription subscription,
            AttachmentEventOperation operation,
            Func<EventEnvelope<Attachment>, CancellationToken,
                ValueTask> attachmentEventHandler,
            CancellationToken cancellationToken = default) =>
                SubscribeToEventAsync(
                    eventAddressIds: EventBrokerIdentifiers.AttachmentEventAddressIds,
                    entityName: nameof(Attachment),
                    subscription: subscription,
                    operation: operation,
                    eventHandler: attachmentEventHandler,
                    cancellationToken: cancellationToken);

        public ValueTask SubscribeToAttachmentEventAsync(
            EventSubscription subscription,
            AttachmentEventOperation operation,
            Func<EventEnvelope<Attachment>, CancellationToken,
                ValueTask<EventEnvelope<Attachment>?>> attachmentEventHandler,
            CancellationToken cancellationToken = default) =>
                SubscribeToEventAsync(
                    eventAddressIds: EventBrokerIdentifiers.AttachmentEventAddressIds,
                    entityName: nameof(Attachment),
                    subscription: subscription,
                    operation: operation,
                    eventHandler: attachmentEventHandler,
                    cancellationToken: cancellationToken);
    }
}
