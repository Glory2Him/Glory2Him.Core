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
using Glory2Him.Core.Models.Foundations.Links;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<Link> InsertLinkAsync(
            Link link,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<Link>> SelectAllLinksAsync(
            CancellationToken cancellationToken = default);

        ValueTask<Link> SelectLinkByIdAsync(
            Guid linkId,
            CancellationToken cancellationToken = default);

        ValueTask<Link> UpdateLinkAsync(
            Link link,
            CancellationToken cancellationToken = default);

        ValueTask<Link> DeleteLinkAsync(
            Link link,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertLinksAsync(
            List<Link> links,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateLinksAsync(
            List<Link> links,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteLinksAsync(
            List<Link> links,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<Link>> BulkReadLinksAsync(
            List<Link> links,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertLinksAsync(
            List<Link> links,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsLinkAsync(
            Guid linkId,
            CancellationToken cancellationToken = default);
    }
}
