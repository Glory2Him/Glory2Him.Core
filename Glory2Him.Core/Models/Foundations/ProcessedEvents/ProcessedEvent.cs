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

namespace Glory2Him.Core.Models.Foundations.ProcessedEvents
{
    /// <summary>
    /// Records that a receiver has processed a specific event, guarding mutating event
    /// handlers against replays and duplicate deliveries. Uniqueness is enforced on
    /// (<see cref="EventId"/>, <see cref="ReceiverName"/>), so recording the same event twice
    /// for the same receiver fails with a duplicate-key error, which callers treat as
    /// "already processed".
    /// </summary>
    public class ProcessedEvent
    {
        public Guid Id { get; set; }

        /// <summary>
        /// The envelope's <c>Metadata.EventId</c> of the processed event.
        /// </summary>
        public Guid EventId { get; set; }

        /// <summary>
        /// The service (and handler) that processed the event, for example
        /// <c>"ContentTypeService.OnContentItemAdded"</c>.
        /// </summary>
        public string ReceiverName { get; set; } = string.Empty;

        public DateTimeOffset ProcessedAt { get; set; }
    }
}
