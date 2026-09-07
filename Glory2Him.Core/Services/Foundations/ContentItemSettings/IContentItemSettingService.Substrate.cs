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

namespace Glory2Him.Core.Services.Foundations.ContentItemSettings
{
    /// <summary>
    /// The event-facing surface of the service: request handlers invoked by the event
    /// substrate, one per request address. These are wired to event listeners exclusively in
    /// <c>EventSubscriptionRegistration</c> — the service exposes the capability; the central
    /// registration decides what is connected. Every handler replies with the operation's
    /// outcome envelope (recorded on the delivery), or <c>null</c> when a duplicated request
    /// was skipped.
    ///
    /// <para><b><c>OnAddingContentItemSettingAsync</c> is no longer reached from the
    /// registration.</b> That address binds <c>IContentItemSettingOrchestrationService</c>, which
    /// derives the override's <c>ContentType</c> from the content item it names and then calls
    /// this handler with the same envelope (#456). The capability stays here — the deduplication,
    /// the write, the fact and the reply are all this service's — but the address is one tier up,
    /// because the rule it carries reads a second entity type and a foundation may not.</para>
    /// </summary>
    public partial interface IContentItemSettingService
    {
        ValueTask<EventEnvelope<ContentItemSetting>?> OnAddingContentItemSettingAsync(
            EventEnvelope<ContentItemSetting> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether this add request has ALREADY been applied by this receiver — the same
        /// <c>ProcessedEvents</c> question <see cref="OnAddingContentItemSettingAsync"/> asks
        /// itself, exposed so the layer above can ask it FIRST.
        ///
        /// <para>The orchestration that now owns this address does work of its own before
        /// delegating: it reads the <c>ContentItem</c> the row names, to derive the content type
        /// (#456). Left after the deduplication, that read makes a replay do more than it used to
        /// — a re-delivered envelope whose item has since been soft-deleted, or has stopped being
        /// visible to the signed caller, fails the derivation and is recorded as a failed delivery
        /// and retried, for an event that was already applied successfully. Before the address
        /// moved up a tier, the duplicate short-circuited to <c>null</c> without touching the item
        /// at all, and it must still.</para>
        ///
        /// <para>A boolean, over the receiver's own bookkeeping — it reveals nothing but whether
        /// this system has seen an event id the caller minted. The handler keeps asking the same
        /// question for itself, because it must be safe called alone (§14.6 rule 1); this only
        /// moves the answer earlier for the path that has work in front of it.</para>
        /// </summary>
        internal ValueTask<bool> HasAlreadyAddedContentItemSettingAsync(
            EventEnvelope<ContentItemSetting> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<ContentItemSetting>?> OnModifyingContentItemSettingAsync(
            EventEnvelope<ContentItemSetting> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<ContentItemSetting>?> OnRemovingContentItemSettingByIdAsync(
            EventEnvelope<ContentItemSetting> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<ContentItemSetting>?> OnHardRemovingContentItemSettingByIdAsync(
            EventEnvelope<ContentItemSetting> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<ContentItemSetting>?> OnRetrievingContentItemSettingByIdAsync(
            EventEnvelope<ContentItemSetting> envelope,
            CancellationToken cancellationToken = default);
    }
}
