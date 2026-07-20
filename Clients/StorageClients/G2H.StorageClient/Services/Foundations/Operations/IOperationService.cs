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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace G2H.StorageClient.Services.Foundations.Operations
{
    internal interface IOperationService
    {
        ValueTask<T> InsertAsync<T>(T @object, CancellationToken cancellationToken = default)
            where T : class;

        ValueTask<IQueryable<T>> SelectAllAsync<T>(CancellationToken cancellationToken = default)
            where T : class;

        ValueTask<T> SelectAsync<T>(object[] objectIds, CancellationToken cancellationToken = default)
            where T : class;

        ValueTask<T> UpdateAsync<T>(T @object, CancellationToken cancellationToken = default)
            where T : class;

        ValueTask<T> DeleteAsync<T>(T @object, CancellationToken cancellationToken = default)
            where T : class;

        ValueTask BulkInsertAsync<T>(
            IEnumerable<T> objects,
            bool useTransaction = true,
            CancellationToken cancellationToken = default)
            where T : class;

        ValueTask<IEnumerable<T>> BulkReadAsync<T>(
            IEnumerable<T> objects,
            CancellationToken cancellationToken = default)
            where T : class;

        ValueTask BulkUpdateAsync<T>(
            IEnumerable<T> objects,
            bool useTransaction = true,
            CancellationToken cancellationToken = default)
            where T : class;

        ValueTask BulkDeleteAsync<T>(
            IEnumerable<T> objects,
            bool useTransaction = true,
            CancellationToken cancellationToken = default)
            where T : class;

        ValueTask BulkUpsertAsync<T>(
            IEnumerable<T> objects,
            bool useTransaction = true,
            CancellationToken cancellationToken = default)
            where T : class;

        ValueTask<bool> ExistsAsync<T>(object[] objectIds, CancellationToken cancellationToken = default)
            where T : class;
    }
}
