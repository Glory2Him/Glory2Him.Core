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
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<ContentItemAssociation> InsertContentItemAssociationAsync(
            ContentItemAssociation contentItemAssociation);

        ValueTask<IQueryable<ContentItemAssociation>> SelectAllContentItemAssociationsAsync();
        ValueTask<ContentItemAssociation> SelectContentItemAssociationByIdAsync(Guid contentItemAssociationId);

        ValueTask<ContentItemAssociation> UpdateContentItemAssociationAsync(
            ContentItemAssociation contentItemAssociation);

        ValueTask<ContentItemAssociation> DeleteContentItemAssociationAsync(
            ContentItemAssociation contentItemAssociation);

        ValueTask BulkInsertContentItemAssociationsAsync(List<ContentItemAssociation> contentItemAssociations);
        ValueTask BulkUpdateContentItemAssociationsAsync(List<ContentItemAssociation> contentItemAssociations);
        ValueTask BulkDeleteContentItemAssociationsAsync(List<ContentItemAssociation> contentItemAssociations);
    }
}
