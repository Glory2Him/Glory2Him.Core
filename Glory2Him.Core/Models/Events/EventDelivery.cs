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

namespace Glory2Him.Core.Models.Events
{
    /// <summary>
    /// The outcome of delivering a published event to one subscription, observed at dispatch
    /// time. A failed delivery may still succeed later through retries; the durable record of
    /// every delivery lives in the event store.
    /// </summary>
    /// <typeparam name="T">The type of the domain event content payload.</typeparam>
    public sealed class EventDelivery<T>
    {
        /// <summary>
        /// The identifier of the subscription (event listener) this delivery went to.
        /// </summary>
        public Guid SubscriptionId { get; init; }

        /// <summary>
        /// Whether the subscription's handler completed successfully during inline dispatch.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// The delivery status at dispatch time: Pending, Success, Error, or Replay.
        /// </summary>
        public string Status { get; init; } = string.Empty;

        /// <summary>
        /// The response code the handler (or the dispatch pipeline) reported.
        /// </summary>
        public string? ResponseCode { get; init; }

        /// <summary>
        /// The response message the handler (or the dispatch pipeline) reported.
        /// </summary>
        public string? ResponseMessage { get; init; }

        /// <summary>
        /// The reply envelope the subscription's handler returned, if any. Only responder
        /// handlers produce one; notification handlers always leave this <c>null</c>.
        /// </summary>
        public EventEnvelope<T>? Response { get; init; }
    }
}
