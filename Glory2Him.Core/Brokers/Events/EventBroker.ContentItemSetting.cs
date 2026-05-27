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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using LeVent.Clients;

namespace Glory2Him.Core.Brokers.Events
{
    public partial class EventBroker
    {
        public ILeVentClient<ContentItemSetting> ContentItemSettingEvents { get; set; }

        public ValueTask PublishContentItemSettingAsync(ContentItemSetting contentItemSetting, string? eventName = null) =>
            this.ContentItemSettingEvents.PublishEventAsync(contentItemSetting, eventName);

        public void SubscribeToContentItemSettingEvent(
            Func<ContentItemSetting, ValueTask> contentItemSettingEventHandler,
            string? eventName = null) =>
                this.ContentItemSettingEvents.RegisterEventHandler(contentItemSettingEventHandler, eventName);
    }
}
