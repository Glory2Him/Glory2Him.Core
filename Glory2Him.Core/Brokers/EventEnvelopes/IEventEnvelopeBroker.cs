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

using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;

namespace Glory2Him.Core.Brokers.EventEnvelopes
{
    /// <summary>
    /// The single place event envelopes are shaped, so metadata, security context, and
    /// causation chaining never diverge between services or between the event and non-event
    /// paths.
    /// </summary>
    internal interface IEventEnvelopeBroker
    {
        /// <summary>
        /// Creates a root envelope for a new operation: fresh event and correlation
        /// identifiers, and the current caller captured as the <c>SecurityContext</c>.
        /// Used by the non-event path to convert an incoming object into the envelope
        /// currency before calling the shared do-work methods.
        /// </summary>
        ValueTask<EventEnvelope<T>> CreateAsync<T>(T content);

        /// <summary>
        /// Creates the next envelope in a causation chain: fresh event identifier,
        /// <c>CausationId</c> pointing at the source envelope's event, and the source's
        /// security and request context carried forward unchanged. Used for outbound events
        /// emitted by do-work methods and for replies returned by responder handlers.
        /// </summary>
        ValueTask<EventEnvelope<T>> CreateNextAsync<TSource, T>(
            EventEnvelope<TSource> sourceEnvelope,
            T content);
    }
}
