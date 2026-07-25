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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;

namespace Glory2Him.Core.Brokers.Events
{
    public partial interface IEventBroker
    {
        ValueTask<EventPublishResult<ApprovalSettingReviewerRole>> PublishApprovalSettingReviewerRoleAsync(
            EventEnvelope<ApprovalSettingReviewerRole> envelope,
            ApprovalSettingReviewerRoleEventOperation operation);

        ValueTask SubscribeToApprovalSettingReviewerRoleEventAsync(
            EventSubscription subscription,
            ApprovalSettingReviewerRoleEventOperation operation,
            Func<EventEnvelope<ApprovalSettingReviewerRole>, CancellationToken,
                ValueTask> approvalSettingReviewerRoleEventHandler,
            CancellationToken cancellationToken = default);

        ValueTask SubscribeToApprovalSettingReviewerRoleEventAsync(
            EventSubscription subscription,
            ApprovalSettingReviewerRoleEventOperation operation,
            Func<EventEnvelope<ApprovalSettingReviewerRole>, CancellationToken,
                ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?>> approvalSettingReviewerRoleEventHandler,
            CancellationToken cancellationToken = default);
    }
}
