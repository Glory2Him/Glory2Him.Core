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

namespace Glory2Him.Core.Services.Processings.ContentItems
{
    public partial interface IContentItemProcessingService
    {
        /// <summary>
        /// The event path of the processing service: handles <c>ContentItemProcessing-Adding</c> request
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
        /// The event path of the modify flow: handles <c>ContentItemProcessing-Modifying</c> request
        /// envelopes, converging on the same do-work as <see cref="ModifyContentItemAsync"/>.
        /// The envelope's <c>SecurityContext</c> carries the original caller for the
        /// contribution gate and the ownership/role permission checks. Replies with the
        /// amended (or forked) content item's envelope.
        /// </summary>
        ValueTask<EventEnvelope<ContentItem>?> OnModifyingContentItemAsync(
            EventEnvelope<ContentItem> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The event path of the remove flow: handles <c>ContentItemProcessing-RemovingById</c> request
        /// envelopes, converging on the same do-work as <see cref="RemoveContentItemByIdAsync"/>.
        /// The request payload is the remove instruction — the content item's <c>Id</c> and
        /// the optional <c>DeletionReason</c>; the envelope's <c>SecurityContext</c> carries
        /// the original caller for the contribution gate and the owner/<c>Administrators</c> check.
        /// Replies with the removed content item's envelope.
        /// </summary>
        ValueTask<EventEnvelope<ContentItem>?> OnRemovingContentItemByIdAsync(
            EventEnvelope<ContentItem> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The event path of the retrieve flow: handles <c>ContentItemProcessing-RetrievingById</c>
        /// request envelopes, converging on the same do-work as
        /// <see cref="RetrieveContentItemByIdAsync"/>. The request payload carries the
        /// content item's <c>Id</c>; the envelope's <c>SecurityContext</c> carries the
        /// original caller for the visibility posture — public versions reply for any
        /// caller, non-public versions only for the owner or a review role. Replies with
        /// the retrieved content item's envelope; being a read it is naturally idempotent
        /// and publishes no completion fact.
        /// </summary>
        ValueTask<EventEnvelope<ContentItem>?> OnRetrievingContentItemByIdAsync(
            EventEnvelope<ContentItem> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles the approval command for this Versioned entity: clears the group's published
        /// slot before the newly approved row takes it, then forwards the decision to the
        /// foundation (design §9.7.7 rules 6–7, §12.4.1 rule 10).
        ///
        /// <para>The approval workflow addresses Versioned entities HERE rather than at the
        /// foundation, because the published slot is held by a unique filtered index and
        /// promoting while the incumbent still holds it is refused outright. Only this layer can
        /// order the two writes.</para>
        /// </summary>
        ValueTask<EventEnvelope<ContentItem>?> OnApprovingContentItemAsync(
            EventEnvelope<ContentItem> envelope,
            CancellationToken cancellationToken = default);
    }
}
