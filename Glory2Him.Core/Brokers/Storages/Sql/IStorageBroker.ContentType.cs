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
using Glory2Him.Core.Models.Foundations.ContentTypes;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<ContentType> InsertContentTypeAsync(
        ContentType contentType);

        ValueTask<IQueryable<ContentType>> SelectAllContentTypesAsync();
        ValueTask<ContentType> SelectContentTypeByIdAsync(Guid contentTypeId);

        ValueTask<ContentType> UpdateContentTypeAsync(
            ContentType contentType);

        ValueTask<ContentType> DeleteContentTypeAsync(
            ContentType contentType);

        ValueTask BulkInsertContentTypesAsync(List<ContentType> contentTypes);
        ValueTask BulkUpdateContentTypesAsync(List<ContentType> contentTypes);
        ValueTask BulkDeleteContentTypesAsync(List<ContentType> contentTypes);
    }
}
