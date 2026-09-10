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
using System.Collections.Generic;
using System.Linq;

namespace Glory2Him.Core.Models.Events
{
    /// <summary>
    /// The result of publishing an event: the persisted event's identifier and the
    /// dispatch-time outcome of every delivery, including any reply envelopes returned by
    /// responder subscriptions.
    ///
    /// <para><b>A publisher does not get to ignore this.</b> Delivery is contained rather than
    /// propagated, so a subscriber that failed says so HERE and nowhere else, and nothing
    /// redelivers it today. §10.19 rules who must look: an address carrying a state-writing
    /// subscriber is a required delivery whose result must be inspected, and only an address
    /// nobody subscribes to may discard it. Inspecting unconditionally is the cheap answer —
    /// an unsubscribed address returns no deliveries at all.</para>
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

        /// <summary>
        /// The deliveries that reported a FAILURE at dispatch time — usually a handler that
        /// received the envelope and then threw, rather than one the event never reached.
        ///
        /// <para>Keyed on <see cref="EventDelivery{T}.IsFailure"/>, never on the inverse of
        /// <see cref="EventDelivery{T}.IsSuccess"/>: Pending and Replay are also "not successful"
        /// and are ordinary transient outcomes, so inverting success would alarm on deliveries
        /// that had simply not been attempted yet.</para>
        ///
        /// <para>ONE definition of "failed", here, because the guard and the message that explains
        /// the guard must not be able to disagree. Deriving the predicate twice — once to decide
        /// whether to report and once to say what to report — lets a widening of either side
        /// produce a report that names nobody.</para>
        ///
        /// <para><b>Empty is not proof of a clean publish.</b> It is also what an address nobody
        /// subscribes to returns, and what a substrate that failed to RECORD a delivery returns —
        /// so this answers "was a failure reported", never "did everything arrive".</para>
        /// </summary>
        public IReadOnlyList<EventDelivery<T>> FailedDeliveries =>
            (Deliveries ?? [])
                .Where(delivery => delivery.IsFailure)
                .ToList();

        /// <summary>
        /// Whether any subscription reported an unsuccessful delivery at dispatch time. False for
        /// an address nobody subscribes to, which is what lets a publisher inspect without first
        /// knowing whether its address is subscribed (§10.19 rule 1).
        ///
        /// <para>Null-tolerant on <see cref="Deliveries"/> deliberately. It is
        /// <c>init</c>-settable and the solution's own integration tests already null-coalesce it,
        /// so a null is reachable — and this runs AFTER the row is committed, where throwing
        /// would report a completed write as a failed one (§10.19 rule 2).</para>
        /// </summary>
        public bool HasFailedDeliveries =>
            FailedDeliveries.Count > 0;
    }
}
