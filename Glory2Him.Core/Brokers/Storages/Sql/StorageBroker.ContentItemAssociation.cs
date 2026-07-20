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
using EFxceptions;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<ContentItemAssociation> ContentItemAssociations { get; set; }

        public async ValueTask<ContentItemAssociation> InsertContentItemAssociationAsync(
            ContentItemAssociation contentItemAssociation, CancellationToken cancellationToken = default) =>
                await InsertAsync(contentItemAssociation, cancellationToken);

        public async ValueTask<IQueryable<ContentItemAssociation>> SelectAllContentItemAssociationsAsync() =>
            await SelectAllAsync<ContentItemAssociation>();

        public async ValueTask<ContentItemAssociation> SelectContentItemAssociationByIdAsync(
            Guid contentItemAssociationId, CancellationToken cancellationToken = default) =>
                await SelectAsync<ContentItemAssociation>(new object[] { contentItemAssociationId }, cancellationToken);

        public async ValueTask<ContentItemAssociation> UpdateContentItemAssociationAsync(
            ContentItemAssociation contentItemAssociation, CancellationToken cancellationToken = default) =>
                await UpdateAsync(contentItemAssociation, cancellationToken);

        public async ValueTask<ContentItemAssociation> DeleteContentItemAssociationAsync(
            ContentItemAssociation contentItemAssociation, CancellationToken cancellationToken = default) =>
                await DeleteAsync(contentItemAssociation, cancellationToken);

        public async ValueTask BulkInsertContentItemAssociationsAsync(
            List<ContentItemAssociation> contentItemAssociations, CancellationToken cancellationToken = default) =>
                await BulkInsertAsync(contentItemAssociations, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateContentItemAssociationsAsync(
            List<ContentItemAssociation> contentItemAssociations, CancellationToken cancellationToken = default) =>
                await BulkUpdateAsync(contentItemAssociations, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteContentItemAssociationsAsync(
            List<ContentItemAssociation> contentItemAssociations, CancellationToken cancellationToken = default) =>
                await BulkDeleteAsync(contentItemAssociations, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<ContentItemAssociation>> BulkReadContentItemAssociationsAsync(
            List<ContentItemAssociation> contentItemAssociations,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(contentItemAssociations, cancellationToken);

        public async ValueTask BulkUpsertContentItemAssociationsAsync(
            List<ContentItemAssociation> contentItemAssociations,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(contentItemAssociations, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsContentItemAssociationAsync(
            Guid contentItemAssociationId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<ContentItemAssociation>(new object[] { contentItemAssociationId }, cancellationToken);
    }
}