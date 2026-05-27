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
using System.Threading;
using System.Threading.Tasks;
using EFxceptions;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<BibleReference> BibleReferences { get; set; }

        public async ValueTask<BibleReference> InsertBibleReferenceAsync(
            BibleReference bibleReference,
            CancellationToken cancellationToken = default) =>
            await InsertAsync(bibleReference, cancellationToken);

        public async ValueTask<IQueryable<BibleReference>> SelectAllBibleReferencesAsync() =>
            await SelectAllAsync<BibleReference>();

        public async ValueTask<BibleReference> SelectBibleReferenceByIdAsync(
            Guid bibleReferenceId,
            CancellationToken cancellationToken = default) =>
            await SelectAsync<BibleReference>(new object[] { bibleReferenceId }, cancellationToken);

        public async ValueTask<BibleReference> UpdateBibleReferenceAsync(
            BibleReference bibleReference,
            CancellationToken cancellationToken = default) =>
            await UpdateAsync(bibleReference, cancellationToken);

        public async ValueTask<BibleReference> DeleteBibleReferenceAsync(
            BibleReference bibleReference,
            CancellationToken cancellationToken = default) =>
            await DeleteAsync(bibleReference, cancellationToken);

        public async ValueTask BulkInsertBibleReferencesAsync(
            List<BibleReference> bibleReferences,
            CancellationToken cancellationToken = default) =>
            await BulkInsertAsync(bibleReferences, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateBibleReferencesAsync(
            List<BibleReference> bibleReferences,
            CancellationToken cancellationToken = default) =>
            await BulkUpdateAsync(bibleReferences, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteBibleReferencesAsync(
            List<BibleReference> bibleReferences,
            CancellationToken cancellationToken = default) =>
            await BulkDeleteAsync(bibleReferences, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<BibleReference>> BulkReadBibleReferencesAsync(
            List<BibleReference> bibleReferences,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(bibleReferences, cancellationToken);

        public async ValueTask BulkUpsertBibleReferencesAsync(
            List<BibleReference> bibleReferences,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(bibleReferences, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsBibleReferenceAsync(
            Guid bibleReferenceId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<BibleReference>(new object[] { bibleReferenceId }, cancellationToken);
    }
}
