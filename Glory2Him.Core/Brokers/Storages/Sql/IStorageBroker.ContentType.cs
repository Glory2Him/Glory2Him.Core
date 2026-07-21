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

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<ContentType> InsertContentTypeAsync(
            ContentType contentType,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ContentType>> SelectAllContentTypesAsync(
            CancellationToken cancellationToken = default);

        ValueTask<ContentType> SelectContentTypeByIdAsync(
            Guid contentTypeId,
            CancellationToken cancellationToken = default);

        ValueTask<ContentType> UpdateContentTypeAsync(
            ContentType contentType,
            CancellationToken cancellationToken = default);

        ValueTask<ContentType> DeleteContentTypeAsync(
            ContentType contentType,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertContentTypesAsync(
            List<ContentType> contentTypes,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateContentTypesAsync(
            List<ContentType> contentTypes,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteContentTypesAsync(
            List<ContentType> contentTypes,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<ContentType>> BulkReadContentTypesAsync(
            List<ContentType> contentTypes,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertContentTypesAsync(
            List<ContentType> contentTypes,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsContentTypeAsync(
            Guid contentTypeId,
            CancellationToken cancellationToken = default);
    }
}
