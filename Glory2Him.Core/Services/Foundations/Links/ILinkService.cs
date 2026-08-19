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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;

namespace Glory2Him.Core.Services.Foundations.Links
{
    internal partial interface ILinkService
    {
        ValueTask<Link> AddLinkAsync(
            Link link,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<Link>> RetrieveAllLinksAsync(
            CancellationToken cancellationToken = default);

        ValueTask<Link> RetrieveLinkByIdAsync(
            Guid linkId,
            CancellationToken cancellationToken = default);

        ValueTask<Link> ModifyLinkAsync(
            Link link,
            CancellationToken cancellationToken = default);

        ValueTask<Link> RemoveLinkByIdAsync(
            Guid linkId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        ValueTask<Link> HardRemoveLinkByIdAsync(
            Guid linkId,
            CancellationToken cancellationToken = default);

        ValueTask<Link> SubmitLinkByIdAsync(
            Guid linkId,
            CancellationToken cancellationToken = default);

        ValueTask<Link> TransitionLinkApprovalAsync(
            Link link,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears the group's published slot so a newly approved sibling can take it
        /// (design §9.7.7 rules 6–7). Owns <c>IsPublished</c> and
        /// <c>PublishDate</c> and nothing else — the row stays <c>Approved</c>,
        /// because it was approved; it is superseded, not un-approved — and
        /// <c>IsLatestVersion</c> is untouched (§3.4 rule 18).
        ///
        /// <para>Gated to <c>Admin</c> or the workflow's system identity, never the
        /// publisher tier: the row being unpublished is itself <c>Approved</c>, and
        /// §8.6 HR-4 bars a <c>Publisher</c> from moving an approved row.</para>
        ///
        /// <para>Deliberately loads a soft-deleted row too. The published slot is held
        /// by an index filtered on <c>IsPublished</c> alone, so a tombstone that kept
        /// the flag still occupies it — refusing to clear one would make the group
        /// permanently unpublishable.</para>
        /// </summary>
        ValueTask<Link> UnpublishLinkByIdAsync(
            Guid linkId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The publication swap's route into <see cref="UnpublishLinkByIdAsync(System.Guid,
        /// System.Threading.CancellationToken)"/>, taking the envelope the swap is acting
        /// under so the workflow's identity is CARRIED rather than re-asserted.
        ///
        /// <para>Minting a fresh context here would read the ambient HTTP caller — who on
        /// an automatic approval is the reviewer whose own review completed the round, not
        /// an <c>Admin</c> — and the unpublish would be refused for the one caller entitled
        /// to make it.</para>
        /// </summary>
        internal ValueTask<Link> UnpublishLinkByIdAsync(
            Guid linkId,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The publication swap's second write, taking the envelope the swap is acting
        /// under so the workflow's identity is CARRIED rather than re-asserted — the same
        /// reason <see cref="UnpublishLinkByIdAsync(System.Guid,
        /// Glory2Him.Core.Models.Events.EventEnvelope{Link}, System.Threading.CancellationToken)"/>
        /// takes one.
        ///
        /// <para>Minting a fresh context here would read the ambient caller and re-ask a
        /// question already answered on the <c>Approval</c> row. It fails deterministically:
        /// the decision function refuses any outcome once the approval is no longer
        /// <c>Submitted</c>, which by this point it is not (§16.7.1).</para>
        /// </summary>
        internal ValueTask<Link> TransitionLinkApprovalAsync(
            Link link,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The id of the row currently holding this group's published slot, if any, over the
        /// UNFILTERED store — or <c>null</c> when the slot is free.
        ///
        /// <para>The publication swap cannot use the caller-facing collection read: that applies
        /// the visibility filter, which drops soft-deleted rows. A soft delete never clears
        /// <c>IsPublished</c> and the slot index names that column alone, so a tombstone still
        /// occupies the slot while being invisible to every ordinary read. A filtered probe would
        /// report no incumbent, the demote would be skipped, and the promote would be refused by
        /// the unique index — permanently, for every future approval in the group
        /// (design §9.7.7 rule 7).</para>
        ///
        /// <para>Only an id crosses back, following the §14.6 pattern of filtered reads for
        /// entities and gated probes for cross-row facts.</para>
        /// </summary>
        ValueTask<Guid?> FindPublishedLinkIdByGroupAsync(
            Guid groupId,
            Guid excludedLinkId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The highest <c>Version</c> any row of this group has ever held, over the UNFILTERED
        /// store — <c>0</c> when the group has no rows.
        ///
        /// <para>Deliberately distinct from "which row is the tip". The tip is the highest LIVE
        /// version, because nobody edits a tombstone. The next free version number must account
        /// for tombstones too, because the unique index on <c>(GroupId, Version)</c> carries no
        /// <c>IsDeleted</c> filter and a soft-deleted row still owns its number.</para>
        ///
        /// <para>Conflating the two was issue #271: a live row beneath a soft-deleted higher
        /// version looked like the tip, and the fork numbered its successor onto the tombstone,
        /// failing at the index for every subsequent fork in that group.</para>
        /// </summary>
        ValueTask<int> FindHighestVersionInGroupAsync(
            Guid groupId,
            CancellationToken cancellationToken = default);

    }
}
