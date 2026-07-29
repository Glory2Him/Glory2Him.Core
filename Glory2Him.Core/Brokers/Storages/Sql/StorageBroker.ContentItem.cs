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
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        public DbSet<ContentItem> ContentItems { get; set; }

        public async ValueTask<ContentItem> InsertContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default) =>
            await InsertAsync(contentItem, cancellationToken);

        public async ValueTask<IQueryable<ContentItem>> SelectAllContentItemsAsync(
            CancellationToken cancellationToken = default) =>
            await SelectAllAsync<ContentItem>(cancellationToken);

        public async ValueTask<ContentItem> SelectContentItemByIdAsync(
            Guid contentItemId,
            CancellationToken cancellationToken = default) =>
            await SelectAsync<ContentItem>(new object[] { contentItemId }, cancellationToken);

        public async ValueTask<ContentItem> UpdateContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default) =>
            await UpdateAsync(contentItem, cancellationToken);

        public async ValueTask<ContentItem> DeleteContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default) =>
            await DeleteAsync(contentItem, cancellationToken);

        public async ValueTask BulkInsertContentItemsAsync(
            List<ContentItem> contentItems,
            CancellationToken cancellationToken = default) =>
            await BulkInsertAsync(contentItems, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateContentItemsAsync(
            List<ContentItem> contentItems,
            CancellationToken cancellationToken = default) =>
            await BulkUpdateAsync(contentItems, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteContentItemsAsync(
            List<ContentItem> contentItems,
            CancellationToken cancellationToken = default) =>
            await BulkDeleteAsync(contentItems, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<ContentItem>> BulkReadContentItemsAsync(
            List<ContentItem> contentItems,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(contentItems, cancellationToken);

        public async ValueTask BulkUpsertContentItemsAsync(
            List<ContentItem> contentItems,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(contentItems, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsContentItemAsync(
            Guid contentItemId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<ContentItem>(new object[] { contentItemId }, cancellationToken);
    }
}