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
            ContentItemAssociation contentItemAssociation) =>
                await InsertAsync(contentItemAssociation);

        public async ValueTask<IQueryable<ContentItemAssociation>> SelectAllContentItemAssociationsAsync() =>
            await SelectAllAsync<ContentItemAssociation>();

        public async ValueTask<ContentItemAssociation> SelectContentItemAssociationByIdAsync(
            Guid contentItemAssociationId) =>
                await SelectAsync<ContentItemAssociation>(contentItemAssociationId);

        public async ValueTask<ContentItemAssociation> UpdateContentItemAssociationAsync(
            ContentItemAssociation contentItemAssociation) =>
                await UpdateAsync(contentItemAssociation);

        public async ValueTask<ContentItemAssociation> DeleteContentItemAssociationAsync(
            ContentItemAssociation contentItemAssociation) =>
                await DeleteAsync(contentItemAssociation);

        public async ValueTask BulkInsertContentItemAssociationsAsync(
            List<ContentItemAssociation> contentItemAssociations) =>
                await BulkInsertAsync(contentItemAssociations);

        public async ValueTask BulkUpdateContentItemAssociationsAsync(
            List<ContentItemAssociation> contentItemAssociations) =>
                await BulkUpdateAsync(contentItemAssociations);

        public async ValueTask BulkDeleteContentItemAssociationsAsync(
            List<ContentItemAssociation> contentItemAssociations) =>
                await BulkDeleteAsync(contentItemAssociations);
    }
}