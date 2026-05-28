// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System;

namespace Glory2Him.Core.Models.Events
{
    /// <summary>
    /// Contains operational information about the original request or process that triggered the event.
    /// Used for audit trails, diagnostics, distributed tracing, and support investigations.
    /// Remains available throughout the event lifecycle, including after the originating HTTP request has ended.
    /// </summary>
    public sealed class RequestContext
    {
        /// <summary>
        /// A unique identifier that spans the entire business operation or request chain.
        /// Used to correlate related events, log entries, and downstream calls across services.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// The UTC date and time at which the originating request was received.
        /// </summary>
        public DateTimeOffset RequestedDate { get; init; }

        /// <summary>
        /// An optional identifier for the specific HTTP request or operation instance.
        /// Useful for correlating log entries within a single request.
        /// </summary>
        public string? RequestId { get; init; }

        /// <summary>
        /// An optional identifier for the system or component that originated the request,
        /// for example the API name, a background worker name, or an external integration name.
        /// </summary>
        public string? SourceSystem { get; init; }

        /// <summary>
        /// An optional identifier for the client application that initiated the request.
        /// Distinct from the OAuth 2.0 <c>client_id</c> and may represent an internal application name or id.
        /// </summary>
        public string? ClientApplicationId { get; init; }
    }
}
