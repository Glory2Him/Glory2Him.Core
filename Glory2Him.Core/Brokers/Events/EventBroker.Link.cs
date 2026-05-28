// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;
using LeVent.Clients;

namespace Glory2Him.Core.Brokers.Events
{
    public partial class EventBroker
    {
        public ILeVentClient<EventEnvelope<Link>> LinkEvents { get; set; }

        public ValueTask PublishLinkAsync(EventEnvelope<Link> envelope, string? eventName = null) =>
            this.LinkEvents.PublishEventAsync(envelope, eventName);

        public void SubscribeToLinkEvent(
            Func<EventEnvelope<Link>, ValueTask> linkEventHandler,
            string? eventName = null) =>
                this.LinkEvents.RegisterEventHandler(linkEventHandler, eventName);
    }
}
