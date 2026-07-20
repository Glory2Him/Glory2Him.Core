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
using G2H.StorageClient.Brokers.Storages;
using G2H.StorageClient.Services.Foundations.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace G2H.StorageClient.Clients
{
    /// <summary>
    /// An EF Core client that wraps common data operations for use in a Storage Broker.
    /// Pass your <see cref="Microsoft.EntityFrameworkCore.DbContext"/> to the constructor and
    /// delegate all CRUD and bulk operations to this client.
    /// </summary>
    public class EFCoreClient : IEFCoreClient
    {
        private readonly IOperationService operationService;

        /// <summary>
        /// Initialises a new instance of <see cref="EFCoreClient"/> using the supplied
        /// <see cref="Microsoft.EntityFrameworkCore.DbContext"/>.
        /// </summary>
        /// <param name="dbContext">
        /// The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> to use for all operations.
        /// Must not be <see langword="null"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="dbContext"/> is <see langword="null"/>.
        /// </exception>
        public EFCoreClient(DbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            IServiceProvider serviceProvider = RegisterServices(dbContext);
            this.operationService = serviceProvider.GetRequiredService<IOperationService>();
        }

        internal EFCoreClient(IOperationService operationService) =>
            this.operationService = operationService;

        /// <inheritdoc/>
        public async ValueTask<T> InsertAsync<T>(T @object, CancellationToken cancellationToken = default)
            where T : class =>
                await this.operationService.InsertAsync(@object, cancellationToken);

        /// <inheritdoc/>
        public async ValueTask<IQueryable<T>> SelectAllAsync<T>(CancellationToken cancellationToken = default)
            where T : class =>
                await this.operationService.SelectAllAsync<T>(cancellationToken);

        /// <inheritdoc/>
        public async ValueTask<T> SelectAsync<T>(object[] objectIds, CancellationToken cancellationToken = default)
            where T : class =>
                await this.operationService.SelectAsync<T>(objectIds, cancellationToken);

        /// <inheritdoc/>
        public async ValueTask<T> UpdateAsync<T>(T @object, CancellationToken cancellationToken = default)
            where T : class =>
                await this.operationService.UpdateAsync(@object, cancellationToken);

        /// <inheritdoc/>
        public async ValueTask<T> DeleteAsync<T>(T @object, CancellationToken cancellationToken = default)
            where T : class =>
                await this.operationService.DeleteAsync(@object, cancellationToken);

        /// <inheritdoc/>
        public async ValueTask BulkInsertAsync<T>(
            IEnumerable<T> objects,
            bool useTransaction = true,
            CancellationToken cancellationToken = default)
            where T : class =>
                await this.operationService.BulkInsertAsync(objects, useTransaction, cancellationToken);

        /// <inheritdoc/>
        public async ValueTask<IEnumerable<T>> BulkReadAsync<T>(
            IEnumerable<T> objects,
            CancellationToken cancellationToken = default)
            where T : class =>
                await this.operationService.BulkReadAsync(objects, cancellationToken);

        /// <inheritdoc/>
        public async ValueTask BulkUpdateAsync<T>(
            IEnumerable<T> objects,
            bool useTransaction = true,
            CancellationToken cancellationToken = default)
            where T : class =>
                await this.operationService.BulkUpdateAsync(objects, useTransaction, cancellationToken);

        /// <inheritdoc/>
        public async ValueTask BulkDeleteAsync<T>(
            IEnumerable<T> objects,
            bool useTransaction = true,
            CancellationToken cancellationToken = default)
            where T : class =>
                await this.operationService.BulkDeleteAsync(objects, useTransaction, cancellationToken);

        /// <inheritdoc/>
        public async ValueTask BulkUpsertAsync<T>(
            IEnumerable<T> objects,
            bool useTransaction = true,
            CancellationToken cancellationToken = default)
            where T : class =>
                await this.operationService.BulkUpsertAsync(objects, useTransaction, cancellationToken);

        /// <inheritdoc/>
        public async ValueTask<bool> ExistsAsync<T>(
            object[] objectIds,
            CancellationToken cancellationToken = default)
            where T : class =>
                await this.operationService.ExistsAsync<T>(objectIds, cancellationToken);

        private static IServiceProvider RegisterServices(DbContext dbContext)
        {
            var serviceCollection = new ServiceCollection()
                .AddTransient(_ => dbContext)
                .AddTransient<IStorageBroker, StorageBroker>()
                .AddTransient<IOperationService, OperationService>();

            return serviceCollection.BuildServiceProvider();
        }
    }
}

