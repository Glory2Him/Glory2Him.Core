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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;

namespace Glory2Him.Core.Services.Orchestrations.ContentItemSettings
{
    /// <summary>
    /// The event-facing surface of this orchestration: the ONE request address whose handler must
    /// sit above the foundation, because the rule it carries spans a second entity type.
    ///
    /// <para>The other four <c>ContentItemSetting</c> request addresses stay bound to
    /// <c>IContentItemSettingService</c>, and that is deliberate rather than unfinished. Modify
    /// pins <c>ContentType</c> and <c>ContentItemId</c> against the stored row, both removals read
    /// the stored row, and the retrieve reads one. None of them has a caller claim left to derive,
    /// so none of them spans a second entity — routing them through here would add a layer that
    /// does nothing but forward (§12.1).</para>
    ///
    /// <para>Wired to the listener exclusively in <c>EventSubscriptionRegistration</c>: the service
    /// exposes the capability, the central registration decides which tier the address binds to —
    /// which is the whole substance of this fix (#456).</para>
    /// </summary>
    public partial interface IContentItemSettingOrchestrationService
    {
        /// <summary>
        /// Handles an add request that arrived over the event substrate, deriving the override's
        /// <c>ContentType</c> from the content item it names before the foundation's write gate
        /// composes the publisher tier out of that value.
        ///
        /// <para>Replies with the outcome envelope the foundation produced, or <c>null</c> when the
        /// foundation skipped a duplicated request — the deduplication, the audit stamping, the
        /// storage write and the past-tense fact all remain the foundation's, reached with the
        /// original envelope so nothing about the delivery's identity or causation changes.</para>
        /// </summary>
        ValueTask<EventEnvelope<ContentItemSetting>?> OnAddingContentItemSettingAsync(
            EventEnvelope<ContentItemSetting> envelope,
            CancellationToken cancellationToken = default);
    }
}
