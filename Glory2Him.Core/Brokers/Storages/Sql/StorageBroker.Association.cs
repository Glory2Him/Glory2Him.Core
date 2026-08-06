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
using Glory2Him.Core.Models.Foundations.Associations;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        public DbSet<Association> Associations { get; set; }

        public async ValueTask<Association> InsertAssociationAsync(
            Association association, CancellationToken cancellationToken = default) =>
                await InsertAsync(association, cancellationToken);

        public async ValueTask<IQueryable<Association>> SelectAllAssociationsAsync(
            CancellationToken cancellationToken = default) =>
            await SelectAllAsync<Association>(cancellationToken);

        public async ValueTask<Association> SelectAssociationByIdAsync(
            Guid associationId, CancellationToken cancellationToken = default) =>
                await SelectAsync<Association>(new object[] { associationId }, cancellationToken);

        public async ValueTask<Association> UpdateAssociationAsync(
            Association association, CancellationToken cancellationToken = default) =>
                await UpdateAsync(association, cancellationToken);

        public async ValueTask<Association> DeleteAssociationAsync(
            Association association, CancellationToken cancellationToken = default) =>
                await DeleteAsync(association, cancellationToken);

        public async ValueTask BulkInsertAssociationsAsync(
            List<Association> associations, CancellationToken cancellationToken = default) =>
                await BulkInsertAsync(associations, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateAssociationsAsync(
            List<Association> associations, CancellationToken cancellationToken = default) =>
                await BulkUpdateAsync(associations, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteAssociationsAsync(
            List<Association> associations, CancellationToken cancellationToken = default) =>
                await BulkDeleteAsync(associations, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<Association>> BulkReadAssociationsAsync(
            List<Association> associations,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(associations, cancellationToken);

        public async ValueTask BulkUpsertAssociationsAsync(
            List<Association> associations,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(associations, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsAssociationAsync(
            Guid associationId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<Association>(new object[] { associationId }, cancellationToken);
    }
}