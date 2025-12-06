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
using Glory2Him.Core.Models.Foundations.Tags;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<Tag> Tags { get; set; }

        public async ValueTask<Tag> InsertTagAsync(Tag tag) =>
            await InsertAsync(tag);

        public async ValueTask<IQueryable<Tag>> SelectAllTagsAsync() =>
            await SelectAllAsync<Tag>();

        public async ValueTask<Tag> SelectTagByIdAsync(Guid tagId) =>
            await SelectAsync<Tag>(tagId);

        public async ValueTask<Tag> UpdateTagAsync(Tag tag) =>
            await UpdateAsync(tag);

        public async ValueTask<Tag> DeleteTagAsync(Tag tag) =>
            await DeleteAsync(tag);

        public async ValueTask BulkInsertTagsAsync(List<Tag> tags) =>
            await BulkInsertAsync(tags);

        public async ValueTask BulkUpdateTagsAsync(List<Tag> tags) =>
            await BulkUpdateAsync(tags);

        public async ValueTask BulkDeleteTagsAsync(List<Tag> tags) =>
            await BulkDeleteAsync(tags);
    }
}