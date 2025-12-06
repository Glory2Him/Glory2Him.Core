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
using Glory2Him.Core.Models.Foundations.Tags;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<Tag> InsertTagAsync(Tag tag);
        ValueTask<IQueryable<Tag>> SelectAllTagsAsync();
        ValueTask<Tag> SelectTagByIdAsync(Guid tagId);
        ValueTask<Tag> UpdateTagAsync(Tag tag);
        ValueTask<Tag> DeleteTagAsync(Tag tag);
        ValueTask BulkInsertTagsAsync(List<Tag> tags);
        ValueTask BulkUpdateTagsAsync(List<Tag> tags);
        ValueTask BulkDeleteTagsAsync(List<Tag> tags);
    }
}
