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
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<ContentType> ContentTypes { get; set; }

        public async ValueTask<ContentType> InsertContentTypeAsync(
            ContentType contentType) =>
                await InsertAsync(contentType);

        public async ValueTask<IQueryable<ContentType>> SelectAllContentTypesAsync() =>
            await SelectAllAsync<ContentType>();

        public async ValueTask<ContentType> SelectContentTypeByIdAsync(
            Guid contentTypeId) =>
                await SelectAsync<ContentType>(contentTypeId);

        public async ValueTask<ContentType> UpdateContentTypeAsync(
            ContentType contentType) =>
                await UpdateAsync(contentType);

        public async ValueTask<ContentType> DeleteContentTypeAsync(
            ContentType contentType) =>
                await DeleteAsync(contentType);

        public async ValueTask BulkInsertContentTypesAsync(
            List<ContentType> contentTypes) =>
                await BulkInsertAsync(contentTypes);

        public async ValueTask BulkUpdateContentTypesAsync(
            List<ContentType> contentTypes) =>
                await BulkUpdateAsync(contentTypes);

        public async ValueTask BulkDeleteContentTypesAsync(
            List<ContentType> contentTypes) =>
                await BulkDeleteAsync(contentTypes);
    }
}