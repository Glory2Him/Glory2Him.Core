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
using Glory2Him.Core.Models.Foundations.BibleReferences;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<BibleReference> InsertBibleReferenceAsync(
            BibleReference bibleReference,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<BibleReference>> SelectAllBibleReferencesAsync();

        ValueTask<BibleReference> SelectBibleReferenceByIdAsync(
            Guid bibleReferenceId,
            CancellationToken cancellationToken = default);

        ValueTask<BibleReference> UpdateBibleReferenceAsync(
            BibleReference bibleReference,
            CancellationToken cancellationToken = default);

        ValueTask<BibleReference> DeleteBibleReferenceAsync(
            BibleReference bibleReference,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertBibleReferencesAsync(
            List<BibleReference> bibleReferences,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateBibleReferencesAsync(
            List<BibleReference> bibleReferences,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteBibleReferencesAsync(
            List<BibleReference> bibleReferences,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<BibleReference>> BulkReadBibleReferencesAsync(
            List<BibleReference> bibleReferences,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertBibleReferencesAsync(
            List<BibleReference> bibleReferences,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsBibleReferenceAsync(
            Guid bibleReferenceId,
            CancellationToken cancellationToken = default);
    }
}
