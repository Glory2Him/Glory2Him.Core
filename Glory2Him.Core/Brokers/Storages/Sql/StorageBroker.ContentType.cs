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
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        public DbSet<ContentType> ContentTypes { get; set; }

        public async ValueTask<ContentType> InsertContentTypeAsync(
            ContentType contentType,
            CancellationToken cancellationToken = default) =>
            await InsertAsync(contentType, cancellationToken);

        public async ValueTask<IQueryable<ContentType>> SelectAllContentTypesAsync(
            CancellationToken cancellationToken = default) =>
            await SelectAllAsync<ContentType>(cancellationToken);

        public async ValueTask<ContentType> SelectContentTypeByIdAsync(
            Guid contentTypeId,
            CancellationToken cancellationToken = default) =>
            await SelectAsync<ContentType>(new object[] { contentTypeId }, cancellationToken);

        public async ValueTask<ContentType> UpdateContentTypeAsync(
            ContentType contentType,
            CancellationToken cancellationToken = default) =>
            await UpdateAsync(contentType, cancellationToken);

        public async ValueTask<ContentType> DeleteContentTypeAsync(
            ContentType contentType,
            CancellationToken cancellationToken = default) =>
            await DeleteAsync(contentType, cancellationToken);

        public async ValueTask BulkInsertContentTypesAsync(
            List<ContentType> contentTypes,
            CancellationToken cancellationToken = default) =>
            await BulkInsertAsync(contentTypes, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateContentTypesAsync(
            List<ContentType> contentTypes,
            CancellationToken cancellationToken = default) =>
            await BulkUpdateAsync(contentTypes, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteContentTypesAsync(
            List<ContentType> contentTypes,
            CancellationToken cancellationToken = default) =>
            await BulkDeleteAsync(contentTypes, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<ContentType>> BulkReadContentTypesAsync(
            List<ContentType> contentTypes,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(contentTypes, cancellationToken);

        public async ValueTask BulkUpsertContentTypesAsync(
            List<ContentType> contentTypes,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(contentTypes, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsContentTypeAsync(
            Guid contentTypeId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<ContentType>(new object[] { contentTypeId }, cancellationToken);
    }
}