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

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial interface IStorageBroker
    {
        ValueTask<Association> InsertAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<Association>> SelectAllAssociationsAsync(
            CancellationToken cancellationToken = default);

        ValueTask<Association> SelectAssociationByIdAsync(
            Guid associationId,
            CancellationToken cancellationToken = default);

        ValueTask<Association> UpdateAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default);

        ValueTask<Association> DeleteAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertAssociationsAsync(
            List<Association> associations,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateAssociationsAsync(
            List<Association> associations,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteAssociationsAsync(
            List<Association> associations,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<Association>> BulkReadAssociationsAsync(
            List<Association> associations,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertAssociationsAsync(
            List<Association> associations,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsAssociationAsync(
            Guid associationId,
            CancellationToken cancellationToken = default);
    }
}
