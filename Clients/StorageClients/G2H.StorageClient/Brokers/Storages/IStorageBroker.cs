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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace G2H.StorageClient.Brokers.Storages
{
    internal interface IStorageBroker
    {
        ValueTask SaveChangesAsync(CancellationToken cancellationToken = default);
        ValueTask<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
        ValueTask<IEntityType> FindEntityTypeAsync<T>();
        ValueTask<IQueryable<T>> SelectAllAsync<T>() where T : class;

        ValueTask<T> SelectAsync<T>(object[] objectIds, CancellationToken cancellationToken = default)
            where T : class;

        ValueTask UpdateObjectStateAsync<T>(T @object, EntityState entityState)
            where T : class;

        ValueTask BulkInsertAsync<T>(IEnumerable<T> objects, CancellationToken cancellationToken = default)
            where T : class;

        ValueTask BulkUpdateAsync<T>(IEnumerable<T> objects, CancellationToken cancellationToken = default)
            where T : class;

        ValueTask BulkDeleteAsync<T>(IEnumerable<T> objects, CancellationToken cancellationToken = default)
            where T : class;
    }
}
