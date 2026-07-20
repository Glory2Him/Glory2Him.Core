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
using System.Collections.Generic;

namespace Glory2Him.Core.Models.Events
{
    /// <summary>
    /// The result of publishing an event: the persisted event's identifier and the
    /// dispatch-time outcome of every delivery, including any reply envelopes returned by
    /// responder subscriptions. Notification-style publishers may simply ignore this result.
    /// </summary>
    /// <typeparam name="T">The type of the domain event content payload.</typeparam>
    public sealed class EventPublishResult<T>
    {
        /// <summary>
        /// The identifier of the persisted event in the event store.
        /// </summary>
        public Guid EventId { get; init; }

        /// <summary>
        /// One delivery outcome per subscription on the event's address, observed at dispatch
        /// time. Empty when the address has no subscriptions.
        /// </summary>
        public IReadOnlyList<EventDelivery<T>> Deliveries { get; init; } = [];
    }
}
