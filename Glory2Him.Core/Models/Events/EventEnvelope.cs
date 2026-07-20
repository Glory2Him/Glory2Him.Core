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

namespace Glory2Him.Core.Models.Events
{
    /// <summary>
    /// A generic wrapper that carries a domain event payload alongside the security context,
    /// request context, and event metadata required to process it safely and consistently.
    /// Decouples orchestration services and event handlers from <c>HttpContext</c>,
    /// <c>IHttpContextAccessor</c>, <c>ClaimsPrincipal</c>, and raw JWT tokens, making the
    /// event pipeline compatible with both in-process and future disconnected processing.
    /// </summary>
    /// <typeparam name="T">The type of the domain event content payload.</typeparam>
    public sealed class EventEnvelope<T>
    {
        /// <summary>
        /// The business payload of the event, such as the domain entity that was created, updated, or deleted.
        /// </summary>
        public T Content { get; init; } = default!;

        /// <summary>
        /// The normalized identity of the authenticated caller at the time the event was created.
        /// </summary>
        public SecurityContext SecurityContext { get; init; } = new SecurityContext();

        /// <summary>
        /// Operational information about the originating request or process, used for tracing and audit.
        /// </summary>
        public RequestContext RequestContext { get; init; } = new RequestContext();

        /// <summary>
        /// Metadata describing the event instance, including its identifier, type, version, and causation.
        /// </summary>
        public EventMetadata Metadata { get; init; } = new EventMetadata();

        /// <summary>
        /// The cryptographic signature details proving the envelope has not been tampered with since it was created.
        /// </summary>
        public EnvelopeIntegrity Integrity { get; init; }
    }
}