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
    /// Contains metadata that describes the event instance itself rather than the business payload.
    /// Supports retries, replays, event versioning, idempotency, causation tracking,
    /// and parent/child event relationships, particularly when events are processed asynchronously
    /// or across distributed systems.
    /// </summary>
    public sealed class EventMetadata
    {
        /// <summary>
        /// A unique identifier for this specific event instance.
        /// Used for idempotency checks and deduplication during retry or replay scenarios.
        /// </summary>
        public Guid EventId { get; init; }

        /// <summary>
        /// The name of the event type, for example the name of the domain entity or operation that produced it.
        /// </summary>
        public string EventType { get; init; }

        /// <summary>
        /// The schema version of this event. Used to support event versioning and backward-compatible evolution.
        /// </summary>
        public int Version { get; init; }

        /// <summary>
        /// The number of times this event has been retried after a processing failure.
        /// </summary>
        public int RetryCount { get; init; }

        /// <summary>
        /// The <see cref="EventId"/> of the event that directly caused this event to be published.
        /// Used to reconstruct causation chains across related events.
        /// </summary>
        public string? CausationId { get; init; }

        /// <summary>
        /// The correlation identifier of the parent operation that spawned this event, if applicable.
        /// Used to link child events back to the wider business workflow they belong to.
        /// </summary>
        public Guid? ParentCorrelationId { get; init; }
    }
}
