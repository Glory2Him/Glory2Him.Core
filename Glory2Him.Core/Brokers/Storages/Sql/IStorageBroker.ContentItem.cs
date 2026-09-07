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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ContentItems;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial interface IStorageBroker
    {
        ValueTask<ContentItem> InsertContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ContentItem>> SelectAllContentItemsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Every row of one group, materialised. The group is the unit three separate reads work
        /// in - the edit tip, the published row, and whether a candidate is still the tip - and a
        /// group holds a handful of versions, so one narrow read serves all three without any of
        /// them enumerating the table.
        ///
        /// <para>UNFILTERED, including soft-deleted rows: callers differ on whether a tombstone
        /// counts, and §14.7 visibility is the SERVICE's to apply. Both decisions belong to the
        /// caller, and neither can be taken here without one of them being wrong.</para>
        /// </summary>
        ValueTask<List<ContentItem>> SelectContentItemsByGroupIdAsync(
            Guid groupId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The version numbers a group's rows already own, tombstones included. The unique index
        /// on (GroupId, Version) carries no IsDeleted filter, so a soft-deleted row still owns its
        /// number and a read that skipped it would hand a fork a number that collides (#271).
        /// </summary>
        ValueTask<List<int>> SelectContentItemVersionsInGroupAsync(
            Guid groupId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The row holding the group's published slot, ignoring one id - the row being published,
        /// which must not find itself. UNFILTERED on the incumbent side: a soft delete never
        /// clears IsPublished and the slot index names that column alone, so a tombstone still
        /// holds the slot and skipping it would leave the group permanently unpublishable.
        /// </summary>
        ValueTask<ContentItem?> SelectPublishedContentItemInGroupAsync(
            Guid groupId,
            Guid excludedContentItemId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Any one row of a group, used only to read the ContentType every version of the group
        /// shares. Deliberately unfiltered (§3.4.2/§14.6): running the read visibility filter would
        /// let a caller who cannot SEE the group's other versions skip the pin, which is the
        /// opposite of what a pin is for.
        /// </summary>
        ValueTask<ContentItem?> SelectContentItemInGroupAsync(
            Guid groupId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether identical content already exists live under one content type, optionally
        /// ignoring a group - the duplicate rule of §3.4.2. Deliberately unfiltered: the rule is
        /// global, and a boolean reveals nothing resubmitting would not already disclose.
        /// </summary>
        ValueTask<bool> ExistsContentItemContentAsync(
            ContentType contentType,
            string contentHash,
            Guid? excludedGroupId = null,
            CancellationToken cancellationToken = default);

        ValueTask<ContentItem> SelectContentItemByIdAsync(
            Guid contentItemId,
            CancellationToken cancellationToken = default);

        ValueTask<ContentItem> UpdateContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default);

        ValueTask<ContentItem> DeleteContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertContentItemsAsync(
            List<ContentItem> contentItems,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateContentItemsAsync(
            List<ContentItem> contentItems,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteContentItemsAsync(
            List<ContentItem> contentItems,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<ContentItem>> BulkReadContentItemsAsync(
            List<ContentItem> contentItems,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertContentItemsAsync(
            List<ContentItem> contentItems,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsContentItemAsync(
            Guid contentItemId,
            CancellationToken cancellationToken = default);
    }
}
