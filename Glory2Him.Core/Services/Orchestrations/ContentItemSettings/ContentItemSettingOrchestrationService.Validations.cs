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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Orchestrations.ContentItemSettings.Exceptions;

namespace Glory2Him.Core.Services.Orchestrations.ContentItemSettings
{
    internal partial class ContentItemSettingOrchestrationService
    {
        // The item's own service answers whether it exists and whether this caller may see it
        // (§16.6), so the read carries the visibility posture rather than reinventing it. Its
        // not-found has already been logged there; to this flow the endpoint simply did not
        // resolve, and the caller is told that and not which of the two reasons applied.
        //
        // A store that could not ANSWER is a different thing and must not be mistaken for a
        // missing row — IsContentItemNotFound lets those through to the dependency clause in the
        // .Exceptions partial, where they keep their category.
        //
        // The METHOD path's read: the item service mints its own envelope, which captures the
        // ambient caller — and on an HTTP request the ambient caller IS the caller, so that is the
        // right answer.
        private async ValueTask<ContentItem> ResolveContentItemAsync(
            Guid contentItemId,
            CancellationToken cancellationToken) =>
            await ResolveContentItemAsync(
                contentItemId: contentItemId,
                read: async () => await this.contentItemService.RetrieveContentItemByIdAsync(
                    contentItemId,
                    cancellationToken));

        // THE EVENT PATH'S READ, and the only difference is WHOSE it is. A substrate delivery has
        // no ambient caller worth trusting: either none at all, so the read runs unauthenticated
        // and answers not-found for every row that is not publicly visible — refusing a legitimate
        // override on an unpublished item; or, since delivery is synchronous inside a publish and
        // HttpContextAccessor flows on an AsyncLocal, whoever PUBLISHED, who for a relayed or
        // system-minted envelope is not the subject the envelope was signed for — resolving a row
        // the signed caller may not see.
        //
        // Handing the foundation the inbound envelope makes the read the SIGNED CALLER'S, so the
        // §16.6 posture the derivation is documented to carry is the posture it actually carries,
        // on both entry paths. Caught by the Copilot review on #469.
        private async ValueTask<ContentItem> ResolveContentItemAsync<TSource>(
            Guid contentItemId,
            EventEnvelope<TSource> inboundEnvelope,
            CancellationToken cancellationToken) =>
            await ResolveContentItemAsync(
                contentItemId: contentItemId,
                read: async () => await this.contentItemService.RetrieveContentItemByIdAsync(
                    contentItemId,
                    inboundEnvelope,
                    cancellationToken));

        // ONE translation for both reads. The two differ only in which read they make; the
        // not-found mapping, and the deliberate gap that lets a store-level failure through with
        // its own category, are the same rule and live in one place so a change to either cannot
        // be applied to one path and forgotten on the other.
        private static async ValueTask<ContentItem> ResolveContentItemAsync(
            Guid contentItemId,
            Func<ValueTask<ContentItem>> read)
        {
            try
            {
                return await read();
            }
            catch (Exception contentItemException)
                when (IsContentItemNotFound(contentItemException))
            {
                throw new NotFoundContentItemSettingOrchestrationException(
                    message: $"The content item was not found with id: {contentItemId}.");
            }
        }

        private static void ValidateContentItemSettingIsNotNull(ContentItemSetting contentItemSetting)
        {
            if (contentItemSetting is null)
            {
                throw new NullContentItemSettingOrchestrationException(
                    message: "Content item setting is null.");
            }
        }

        // THE EVENT PATH'S OWN GUARD, asked before this service reads anything. The foundation
        // asks the identical question further down, and that duplication is §14.6 rule 2 working
        // as intended rather than waste: this handler resolves a CONTENT ITEM before the
        // foundation is reached, and doing that on the word of an envelope nothing has vouched
        // for would be acting on an unattested payload.
        //
        // The name is the publisher's composition — entity name plus operation — and it is inside
        // the HMAC, so a mismatch here is silent: the request is delivered, verified against a
        // name it was never signed with, and refused. It reads "ContentItemSettingAdding" because
        // that is what EventBroker signs for this address, exactly as the foundation composed it
        // while the address bound there.
        private async ValueTask ValidateContentItemSettingEventEnvelopeAsync(
            EventEnvelope<ContentItemSetting> envelope,
            ContentItemSettingEventOperation operation)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidContentItemSettingEventOrchestrationException(
                    message: "Invalid content item setting event. " +
                        "The event envelope, its content and metadata are required.");
            }

            string eventName = $"{nameof(ContentItemSetting)}{operation}";

            bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                envelope, eventName, EnvelopeDirection.Request);

            if (isSignatureValid is false)
            {
                throw new InvalidContentItemSettingEventOrchestrationException(
                    message: "Invalid content item setting event. Integrity verification failed.");
            }
        }

        // THE DERIVATION, EXPRESSED AS A REFUSAL — and the difference from the method path is the
        // signature, not the rule.
        //
        // Both paths run the same derivation: resolve the item the row names, and let what the
        // item IS decide the content type the write gate composes its publisher tier from. What
        // differs is what happens when the caller's claim disagrees. On the method path the
        // derived value simply overwrites it — the caller handed over a loose object and nobody
        // attested to it, so amending it costs nothing. Here the claim arrived inside a signed
        // envelope whose HMAC covers the content (§14.6 rule 4), and the property that signature
        // buys is that no receiver edits a part the rules read. Overwriting the field would leave
        // the ProcessedEvents record, and the reply built from this envelope, carrying content the
        // publisher never signed — and would need this service to mint a request signature of its
        // own, which no service does and none should.
        //
        // So the derived value still governs, and a request that contradicts it is refused rather
        // than quietly corrected. An honest publisher — anything that resolved the item, which is
        // every in-process caller — is unaffected, because for them the two values agree.
        private static void ValidateContentTypeIsTheContentItems(
            ContentType claimedContentType,
            ContentType derivedContentType)
        {
            if (claimedContentType != derivedContentType)
            {
                throw new ContentTypeMismatchContentItemSettingOrchestrationException(
                    message: "Content item setting is invalid. An override's content type is " +
                        "derived from the content item it names and cannot be stated by the " +
                        "caller; fix the errors and try again.");
            }
        }
    }
}
