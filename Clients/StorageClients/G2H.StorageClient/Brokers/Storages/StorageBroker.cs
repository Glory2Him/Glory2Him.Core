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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace G2H.StorageClient.Brokers.Storages
{
    internal class StorageBroker : IStorageBroker
    {
        private readonly DbContext dbContext;

        public StorageBroker(DbContext dbContext) =>
            this.dbContext = dbContext;

        public async ValueTask<IEntityType> FindEntityTypeAsync<T>() =>
            this.dbContext.Model.FindEntityType(typeof(T));

        public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default) =>
            await this.dbContext.SaveChangesAsync(cancellationToken);

        public async ValueTask<IDbContextTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken = default) =>
                await this.dbContext.Database.BeginTransactionAsync(cancellationToken);

        public async ValueTask<IQueryable<T>> SelectAllAsync<T>() where T : class =>
            this.dbContext.Set<T>();

        public async ValueTask<T> SelectAsync<T>(object[] objectIds, CancellationToken cancellationToken = default)
            where T : class =>
                await this.dbContext.FindAsync<T>(objectIds, cancellationToken);

        public async ValueTask UpdateObjectStateAsync<T>(T @object, EntityState entityState)
            where T : class =>
                this.dbContext.Entry(@object).State = entityState;

        public async ValueTask BulkInsertAsync<T>(IEnumerable<T> objects, CancellationToken cancellationToken = default)
            where T : class =>
                await this.dbContext.AddRangeAsync(objects, cancellationToken);

        public async ValueTask BulkUpdateAsync<T>(IEnumerable<T> objects, CancellationToken cancellationToken = default)
            where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.dbContext.UpdateRange(objects);
        }

        public async ValueTask BulkDeleteAsync<T>(IEnumerable<T> objects, CancellationToken cancellationToken = default)
            where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.dbContext.RemoveRange(objects);
        }
    }
}
