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

using System.Threading;
using System.Threading.Tasks;

namespace Glory2Him.Core.Brokers.Events
{
    public partial interface IEventBroker
    {
        /// <summary>
        /// Registers the Glory 2 Him participant in the event substrate. Idempotent;
        /// called once at startup by <c>EventSubscriptionRegistration</c>.
        /// </summary>
        ValueTask RegisterEventParticipantAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers one event address per entity in the event substrate. Idempotent;
        /// called once at startup by <c>EventSubscriptionRegistration</c>.
        /// </summary>
        ValueTask RegisterEventAddressesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Fires scheduled and pending (retryable) events. Intended to be called periodically
        /// by a background service to drive retries and scheduled delivery.
        /// </summary>
        ValueTask FireScheduledPendingEventsAsync(CancellationToken cancellationToken = default);
    }
}
