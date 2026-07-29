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

namespace Glory2Him.Core.Services.Orchestrations.ContentItems
{
    public partial interface IContentItemOrchestrationService
    {
        /// <summary>
        /// The event path of the orchestration: handles <c>ContentItem-Submitting</c> request
        /// envelopes, converging on the same do-work as <see cref="SubmitContentItemAsync"/>.
        /// The envelope's <c>SecurityContext</c> carries the original caller for the
        /// contribution gate. Replies with the created content item's envelope, or
        /// <c>null</c> when the submission was a duplicate and nothing was created.
        /// </summary>
        ValueTask<EventEnvelope<ContentItem>?> OnSubmittingContentItemAsync(
            EventEnvelope<ContentItem> envelope,
            CancellationToken cancellationToken = default);
    }
}
