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
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.Links;

namespace Glory2Him.Core.Services.Processings.Links
{
    internal partial class LinkProcessingService
    {
        public ValueTask<EventEnvelope<Link>?> OnAddingLinkAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateLinkEventEnvelopeAsync(
                    envelope, LinkProcessingEventOperation.Adding);

                Link addedLink =
                    await DoAddLinkAsync(
                        link: envelope.Content,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: addedLink);
            });

        public ValueTask<EventEnvelope<Link>?> OnModifyingLinkAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateLinkEventEnvelopeAsync(
                    envelope, LinkProcessingEventOperation.Modifying);

                Link modifiedLink =
                    await DoModifyLinkAsync(
                        link: envelope.Content,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: modifiedLink);
            });

        public ValueTask<EventEnvelope<Link>?> OnRemovingLinkByIdAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateLinkEventEnvelopeAsync(
                    envelope, LinkProcessingEventOperation.RemovingById);

                Link removedLink =
                    await DoRemoveLinkByIdAsync(
                        linkId: envelope.Content.Id,
                        deletionReason: envelope.Content.DeletionReason,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: removedLink);
            });

        public ValueTask<EventEnvelope<Link>?> OnRetrievingLinkByIdAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateLinkEventEnvelopeAsync(
                    envelope, LinkProcessingEventOperation.RetrievingById);

                // read-only: naturally idempotent and publishes no completion fact — the
                // reply envelope is the whole outcome
                Link retrievedLink =
                    await DoRetrieveLinkByIdAsync(
                        linkId: envelope.Content.Id,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: retrievedLink);
            });
    }
}
