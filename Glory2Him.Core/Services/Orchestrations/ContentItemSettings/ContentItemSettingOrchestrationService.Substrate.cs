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
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;

namespace Glory2Him.Core.Services.Orchestrations.ContentItemSettings
{
    /// <summary>
    /// The event path of the derivation. <c>ContentItemSetting-Adding</c> binds HERE rather than to
    /// the foundation, so the rule that an override's <c>ContentType</c> is the item's — not the
    /// caller's word for it — holds "regardless of which layer is called", which is what §14.6
    /// rule 1 exists to require.
    ///
    /// <para><b>What was wrong before (#456).</b> #455 put the derivation in this orchestration and
    /// repointed the HTTP exposer at it, which closed the method path. The subscription stayed on
    /// <c>IContentItemSettingService</c>, so an add request published to the address reached
    /// <c>DoAddContentItemSettingAsync</c> directly and the scope gate composed the publisher tier
    /// out of the caller's own <c>ContentType</c> — the exact composition #450 was raised to
    /// remove. A holder of <c>ContentItem-Devotional-Publishers</c> could publish an add labelled
    /// <c>Devotional</c> against a quote's id, pass the gate, and take that quote's only override
    /// scope. The envelope is signed, so this needed the signing key or in-process code rather than
    /// an internet-facing request; it was a layering hole, not a remote exploit, and it is closed
    /// the way §10.17 rule 3 closes the analogous one for approvable entities — by moving the
    /// subscription up a tier.</para>
    ///
    /// <para><b>The foundation keeps everything else.</b> This handler adds exactly two things
    /// ahead of it — verify, and derive — then hands the SAME envelope down. Deduplication on
    /// <c>Metadata.EventId</c>, the audit stamping, the write, the past-tense fact and the reply
    /// are all still the foundation's, reached with the envelope untouched, so the delivery's
    /// identity, its causation chain and its <c>ProcessedEvents</c> bookkeeping are exactly what
    /// they were when the address bound one layer lower.</para>
    /// </summary>
    internal partial class ContentItemSettingOrchestrationService
    {
        public ValueTask<EventEnvelope<ContentItemSetting>?> OnAddingContentItemSettingAsync(
            EventEnvelope<ContentItemSetting> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatch<EventEnvelope<ContentItemSetting>?>(async () =>
            {
                // Ahead of the verify: a caller who has already cancelled should not pay for an
                // HMAC computation, and the item read below observes the token in its own right.
                cancellationToken.ThrowIfCancellationRequested();

                await ValidateContentItemSettingEventEnvelopeAsync(
                    envelope: envelope,
                    operation: ContentItemSettingEventOperation.Adding);

                // AN OVERRIDE'S TYPE IS THE ITEM'S TYPE. A default names no item, so there is
                // nothing to resolve and its content type is the whole of what it declares —
                // an administrator saying "this is the Devotional default", and only an
                // administrator may say it. Same branch, same reason, as the method path.
                if (envelope.Content.ContentItemId is not null)
                {
                    // Resolved as the envelope's SIGNED caller, not as whatever identity happens
                    // to be ambient on this delivery — see the resolve for why the difference
                    // matters here and not on the method path.
                    ContentItem contentItem = await ResolveContentItemAsync(
                        contentItemId: envelope.Content.ContentItemId.Value,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                    ValidateContentTypeIsTheContentItems(
                        claimedContentType: envelope.Content.ContentType,
                        derivedContentType: contentItem.ContentType);
                }

                return await this.contentItemSettingService.OnAddingContentItemSettingAsync(
                    envelope: envelope,
                    cancellationToken: cancellationToken);
            });
    }
}
