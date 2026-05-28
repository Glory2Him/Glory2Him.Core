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
using Glory2Him.Core.Models.Foundations.Comments;
using LeVent.Clients;

namespace Glory2Him.Core.Brokers.Events
{
    public partial class EventBroker
    {
        public ILeVentClient<EventEnvelope<Comment>> CommentEvents { get; set; }

        public ValueTask PublishCommentAsync(EventEnvelope<Comment> envelope, string? eventName = null) =>
            this.CommentEvents.PublishEventAsync(envelope, eventName);

        public void SubscribeToCommentEvent(
            Func<EventEnvelope<Comment>, ValueTask> commentEventHandler,
            string? eventName = null) =>
                this.CommentEvents.RegisterEventHandler(commentEventHandler, eventName);
    }
}
