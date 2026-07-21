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
using EFxceptions;
using Glory2Him.Core.Models.Foundations.Tags;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<Tag> Tags { get; set; }

        public async ValueTask<Tag> InsertTagAsync(Tag tag, CancellationToken cancellationToken = default) =>
            await InsertAsync(tag, cancellationToken);

        public async ValueTask<IQueryable<Tag>> SelectAllTagsAsync() =>
            await SelectAllAsync<Tag>();

        public async ValueTask<Tag> SelectTagByIdAsync(Guid tagId, CancellationToken cancellationToken = default) =>
            await SelectAsync<Tag>(new object[] { tagId }, cancellationToken);

        public async ValueTask<Tag> UpdateTagAsync(Tag tag, CancellationToken cancellationToken = default) =>
            await UpdateAsync(tag, cancellationToken);

        public async ValueTask<Tag> DeleteTagAsync(Tag tag, CancellationToken cancellationToken = default) =>
            await DeleteAsync(tag, cancellationToken);

        public async ValueTask BulkInsertTagsAsync(List<Tag> tags, CancellationToken cancellationToken = default) =>
            await BulkInsertAsync(tags, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateTagsAsync(List<Tag> tags, CancellationToken cancellationToken = default) =>
            await BulkUpdateAsync(tags, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteTagsAsync(List<Tag> tags, CancellationToken cancellationToken = default) =>
            await BulkDeleteAsync(tags, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<Tag>> BulkReadTagsAsync(
            List<Tag> tags,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(tags, cancellationToken);

        public async ValueTask BulkUpsertTagsAsync(
            List<Tag> tags,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(tags, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsTagAsync(
            Guid tagId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<Tag>(new object[] { tagId }, cancellationToken);
    }
}