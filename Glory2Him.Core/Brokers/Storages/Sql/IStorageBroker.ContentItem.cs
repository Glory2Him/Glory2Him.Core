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
using Glory2Him.Core.Models.Foundations.ContentItems;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<ContentItem> InsertContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ContentItem>> SelectAllContentItemsAsync();

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
