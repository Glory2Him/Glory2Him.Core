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
using Glory2Him.Core.Models.Foundations.Links;

namespace Glory2Him.Core.Services.Processings.Links
{
    internal partial interface ILinkProcessingService
    {
        /// <summary>
        /// The event path of the processing service: handles <c>LinkProcessing-Adding</c>
        /// request envelopes, converging on the same do-work as <see cref="AddLinkAsync"/>.
        /// The envelope's <c>SecurityContext</c> carries the original caller for the
        /// contribution gate. Replies with the created link's envelope.
        /// </summary>
        ValueTask<EventEnvelope<Link>?> OnAddingLinkAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The event path of the modify flow: handles <c>LinkProcessing-Modifying</c> request
        /// envelopes, converging on the same do-work as <see cref="ModifyLinkAsync"/>. The
        /// envelope's <c>SecurityContext</c> carries the original caller for the contribution
        /// gate and the ownership/role permission checks. Replies with the amended (or
        /// forked) link's envelope.
        /// </summary>
        ValueTask<EventEnvelope<Link>?> OnModifyingLinkAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The event path of the remove flow: handles <c>LinkProcessing-RemovingById</c>
        /// request envelopes, converging on the same do-work as
        /// <see cref="RemoveLinkByIdAsync"/>. The request payload is the remove instruction —
        /// the link's <c>Id</c> and the optional <c>DeletionReason</c>; the envelope's
        /// <c>SecurityContext</c> carries the original caller for the contribution gate and
        /// the owner/<c>Admin</c> check. Replies with the removed link's envelope.
        /// </summary>
        ValueTask<EventEnvelope<Link>?> OnRemovingLinkByIdAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The event path of the retrieve flow: handles <c>LinkProcessing-RetrievingById</c>
        /// request envelopes, converging on the same do-work as
        /// <see cref="RetrieveLinkByIdAsync"/>. The request payload carries the link's
        /// <c>Id</c>; the envelope's <c>SecurityContext</c> carries the original caller for
        /// the visibility posture — public versions reply for any caller, non-public versions
        /// only for the owner or a review role. Replies with the retrieved link's envelope;
        /// being a read it is naturally idempotent and publishes no completion fact.
        /// </summary>
        ValueTask<EventEnvelope<Link>?> OnRetrievingLinkByIdAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles the approval command for this Versioned entity: clears the group's
        /// published slot before the newly approved row takes it, then forwards the
        /// decision to the foundation (design §9.7.7 rules 6–7, §12.4.1 rule 10).
        ///
        /// <para>The approval workflow addresses Versioned entities HERE rather than at
        /// the foundation, because the published slot is held by a unique index and
        /// promoting while the incumbent still holds it is refused outright. Only this
        /// layer can order the two writes.</para>
        /// </summary>
        ValueTask<EventEnvelope<Link>?> OnApprovingLinkAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default);
    }
}
