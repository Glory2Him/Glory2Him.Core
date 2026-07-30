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
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial interface IStorageBroker
    {
        ValueTask<ContentItemAssociation> InsertContentItemAssociationAsync(
            ContentItemAssociation contentItemAssociation,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ContentItemAssociation>> SelectAllContentItemAssociationsAsync(
            CancellationToken cancellationToken = default);

        ValueTask<ContentItemAssociation> SelectContentItemAssociationByIdAsync(
            Guid contentItemAssociationId,
            CancellationToken cancellationToken = default);

        ValueTask<ContentItemAssociation> UpdateContentItemAssociationAsync(
            ContentItemAssociation contentItemAssociation,
            CancellationToken cancellationToken = default);

        ValueTask<ContentItemAssociation> DeleteContentItemAssociationAsync(
            ContentItemAssociation contentItemAssociation,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertContentItemAssociationsAsync(
            List<ContentItemAssociation> contentItemAssociations,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateContentItemAssociationsAsync(
            List<ContentItemAssociation> contentItemAssociations,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteContentItemAssociationsAsync(
            List<ContentItemAssociation> contentItemAssociations,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<ContentItemAssociation>> BulkReadContentItemAssociationsAsync(
            List<ContentItemAssociation> contentItemAssociations,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertContentItemAssociationsAsync(
            List<ContentItemAssociation> contentItemAssociations,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsContentItemAssociationAsync(
            Guid contentItemAssociationId,
            CancellationToken cancellationToken = default);
    }
}
