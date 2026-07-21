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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;

namespace Glory2Him.Core.Brokers.Events
{
    public partial class EventBroker
    {
        public ValueTask<EventPublishResult<ApprovalSetting>> PublishApprovalSettingAsync(
            EventEnvelope<ApprovalSetting> envelope,
            ApprovalSettingEventOperation operation) =>
                PublishEventAsync(
                    eventAddressIds: EventBrokerIdentifiers.ApprovalSettingEventAddressIds,
                    entityName: nameof(ApprovalSetting),
                    envelope: envelope,
                    operation: operation);

        public ValueTask SubscribeToApprovalSettingEventAsync(
            EventSubscription subscription,
            ApprovalSettingEventOperation operation,
            Func<EventEnvelope<ApprovalSetting>, CancellationToken,
                ValueTask> approvalSettingEventHandler,
            CancellationToken cancellationToken = default) =>
                SubscribeToEventAsync(
                    eventAddressIds: EventBrokerIdentifiers.ApprovalSettingEventAddressIds,
                    subscription: subscription,
                    operation: operation,
                    eventHandler: approvalSettingEventHandler,
                    cancellationToken: cancellationToken);

        public ValueTask SubscribeToApprovalSettingEventAsync(
            EventSubscription subscription,
            ApprovalSettingEventOperation operation,
            Func<EventEnvelope<ApprovalSetting>, CancellationToken,
                ValueTask<EventEnvelope<ApprovalSetting>?>> approvalSettingEventHandler,
            CancellationToken cancellationToken = default) =>
                SubscribeToEventAsync(
                    eventAddressIds: EventBrokerIdentifiers.ApprovalSettingEventAddressIds,
                    subscription: subscription,
                    operation: operation,
                    eventHandler: approvalSettingEventHandler,
                    cancellationToken: cancellationToken);
    }
}
