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

using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;

namespace Glory2Him.Core.Services.Foundations.ApprovalSettings
{
    /// <summary>
    /// The event-facing surface of the service: request handlers invoked by the event
    /// substrate, one per request address. These are wired to event listeners exclusively in
    /// <c>EventSubscriptionRegistration</c> — the service exposes the capability; the central
    /// registration decides what is connected. Every handler replies with the operation's
    /// outcome envelope (recorded on the delivery), or <c>null</c> when a duplicated request
    /// was skipped.
    /// </summary>
    public partial interface IApprovalSettingService
    {
        ValueTask<EventEnvelope<ApprovalSetting>?> OnAddingApprovalSettingAsync(
            EventEnvelope<ApprovalSetting> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<ApprovalSetting>?> OnModifyingApprovalSettingAsync(
            EventEnvelope<ApprovalSetting> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<ApprovalSetting>?> OnRemovingApprovalSettingByIdAsync(
            EventEnvelope<ApprovalSetting> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<ApprovalSetting>?> OnHardRemovingApprovalSettingByIdAsync(
            EventEnvelope<ApprovalSetting> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<ApprovalSetting>?> OnRetrievingApprovalSettingByIdAsync(
            EventEnvelope<ApprovalSetting> envelope,
            CancellationToken cancellationToken = default);
    }
}
