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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Orchestrations.ContentItems;

namespace Glory2Him.Core.Services.Orchestrations.ContentItems
{
    internal partial class ContentItemOrchestrationService
    {
        public ValueTask<EventEnvelope<ContentItem>?> OnSubmittingContentItemAsync(
            EventEnvelope<ContentItem> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateContentItemEventEnvelope(envelope);

                ContentItemSubmissionResult contentItemSubmissionResult =
                    await DoSubmitContentItemAsync(
                        contentItem: envelope.Content,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                // duplicate submission: nothing was created and no reply is recorded —
                // a replayed request lands here too, so the flow is naturally idempotent
                if (contentItemSubmissionResult.IsCreated is false)
                    return null;

                return await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: contentItemSubmissionResult.ContentItem!);
            });
    }
}
