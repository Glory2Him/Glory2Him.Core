// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.Tags;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<Tag> InsertTagAsync(Tag tag, CancellationToken cancellationToken = default);
        ValueTask<IQueryable<Tag>> SelectAllTagsAsync();
        ValueTask<Tag> SelectTagByIdAsync(Guid tagId, CancellationToken cancellationToken = default);
        ValueTask<Tag> UpdateTagAsync(Tag tag, CancellationToken cancellationToken = default);
        ValueTask<Tag> DeleteTagAsync(Tag tag, CancellationToken cancellationToken = default);
        ValueTask BulkInsertTagsAsync(List<Tag> tags, CancellationToken cancellationToken = default);
        ValueTask BulkUpdateTagsAsync(List<Tag> tags, CancellationToken cancellationToken = default);
        ValueTask BulkDeleteTagsAsync(List<Tag> tags, CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<Tag>> BulkReadTagsAsync(
            List<Tag> tags,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertTagsAsync(
            List<Tag> tags,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsTagAsync(
            Guid tagId,
            CancellationToken cancellationToken = default);
    }
}
