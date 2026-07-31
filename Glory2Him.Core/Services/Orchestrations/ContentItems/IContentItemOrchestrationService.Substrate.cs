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
    internal partial interface IContentItemOrchestrationService
    {
        /// <summary>
        /// The event path of the orchestration: handles <c>ContentItemOrchestration-Adding</c> request
        /// envelopes, converging on the same do-work as <see cref="AddContentItemAsync"/>.
        /// The envelope's <c>SecurityContext</c> carries the original caller for the
        /// contribution gate. Replies with the created content item's envelope; a
        /// duplicate submission fails with an already-exists validation error, so a
        /// replayed request can never create a second item.
        /// </summary>
        ValueTask<EventEnvelope<ContentItem>?> OnAddingContentItemAsync(
            EventEnvelope<ContentItem> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The event path of the modify flow: handles <c>ContentItemOrchestration-Modifying</c> request
        /// envelopes, converging on the same do-work as <see cref="ModifyContentItemAsync"/>.
        /// The envelope's <c>SecurityContext</c> carries the original caller for the
        /// contribution gate and the ownership/role permission checks. Replies with the
        /// amended (or forked) content item's envelope.
        /// </summary>
        ValueTask<EventEnvelope<ContentItem>?> OnModifyingContentItemAsync(
            EventEnvelope<ContentItem> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The event path of the remove flow: handles <c>ContentItemOrchestration-RemovingById</c> request
        /// envelopes, converging on the same do-work as <see cref="RemoveContentItemByIdAsync"/>.
        /// The request payload is the remove instruction — the content item's <c>Id</c> and
        /// the optional <c>DeletionReason</c>; the envelope's <c>SecurityContext</c> carries
        /// the original caller for the contribution gate and the owner/<c>Admin</c> check.
        /// Replies with the removed content item's envelope.
        /// </summary>
        ValueTask<EventEnvelope<ContentItem>?> OnRemovingContentItemByIdAsync(
            EventEnvelope<ContentItem> envelope,
            CancellationToken cancellationToken = default);
    }
}
