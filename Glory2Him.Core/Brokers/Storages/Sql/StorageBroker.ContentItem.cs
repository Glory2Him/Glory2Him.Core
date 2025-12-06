// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFxceptions;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<ContentItem> ContentItems { get; set; }

        public async ValueTask<ContentItem> InsertContentItemAsync(ContentItem contentItem) =>
            await InsertAsync(contentItem);

        public async ValueTask<IQueryable<ContentItem>> SelectAllContentItemsAsync() =>
            await SelectAllAsync<ContentItem>();

        public async ValueTask<ContentItem> SelectContentItemByIdAsync(Guid contentItemId) =>
            await SelectAsync<ContentItem>(contentItemId);

        public async ValueTask<ContentItem> UpdateContentItemAsync(ContentItem contentItem) =>
            await UpdateAsync(contentItem);

        public async ValueTask<ContentItem> DeleteContentItemAsync(ContentItem contentItem) =>
            await DeleteAsync(contentItem);

        public async ValueTask BulkInsertContentItemsAsync(List<ContentItem> contentItems) =>
            await BulkInsertAsync(contentItems);

        public async ValueTask BulkUpdateContentItemsAsync(List<ContentItem> contentItems) =>
            await BulkUpdateAsync(contentItems);

        public async ValueTask BulkDeleteContentItemsAsync(List<ContentItem> contentItems) =>
            await BulkDeleteAsync(contentItems);
    }
}