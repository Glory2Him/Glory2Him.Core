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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.Links;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial interface IStorageBroker
    {
        ValueTask<Link> InsertLinkAsync(
            Link link,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<Link>> SelectAllLinksAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Every row of one group, materialised - the link twin of
        /// <see cref="SelectContentItemsByGroupIdAsync"/>, and unfiltered for the same reasons:
        /// whether a tombstone counts, and §14.7 visibility, are both the caller's to decide.
        /// </summary>
        ValueTask<List<Link>> SelectLinksByGroupIdAsync(
            Guid groupId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The version numbers a group's rows already own, tombstones included - a soft-deleted
        /// row still owns its number under the unfiltered unique index on (GroupId, Version).
        /// </summary>
        ValueTask<List<int>> SelectLinkVersionsInGroupAsync(
            Guid groupId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The row holding the group's published slot, ignoring one id. UNFILTERED on the
        /// incumbent side: a soft delete never clears IsPublished and the slot index names that
        /// column alone, so a tombstone still holds the slot.
        /// </summary>
        /// <summary>
        /// Whether the group holds a LIVE row at a higher version than the one given — the link
        /// twin of <see cref="ExistsHigherLiveContentItemVersionInGroupAsync"/>, unfiltered for
        /// the same reason: which row is the tip is a fact about storage, not a per-caller view.
        /// </summary>
        ValueTask<bool> ExistsHigherLiveLinkVersionInGroupAsync(
            Guid groupId,
            int version,
            CancellationToken cancellationToken = default);

        ValueTask<Link?> SelectPublishedLinkInGroupAsync(
            Guid groupId,
            Guid excludedLinkId,
            CancellationToken cancellationToken = default);

        ValueTask<Link> SelectLinkByIdAsync(
            Guid linkId,
            CancellationToken cancellationToken = default);

        ValueTask<Link> UpdateLinkAsync(
            Link link,
            CancellationToken cancellationToken = default);

        ValueTask<Link> DeleteLinkAsync(
            Link link,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertLinksAsync(
            List<Link> links,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateLinksAsync(
            List<Link> links,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteLinksAsync(
            List<Link> links,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<Link>> BulkReadLinksAsync(
            List<Link> links,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertLinksAsync(
            List<Link> links,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsLinkAsync(
            Guid linkId,
            CancellationToken cancellationToken = default);
    }
}
