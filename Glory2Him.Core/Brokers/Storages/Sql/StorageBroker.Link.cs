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
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        public DbSet<Link> Links { get; set; }

        public async ValueTask<Link> InsertLinkAsync(
            Link link,
            CancellationToken cancellationToken = default) =>
            await InsertAsync(link, cancellationToken);

        public async ValueTask<IQueryable<Link>> SelectAllLinksAsync(
            CancellationToken cancellationToken = default) =>
            await SelectAllAsync<Link>(cancellationToken);

        public async ValueTask<Link> SelectLinkByIdAsync(
            Guid linkId,
            CancellationToken cancellationToken = default) =>
            await SelectAsync<Link>(new object[] { linkId }, cancellationToken);

        public async ValueTask<Link> UpdateLinkAsync(
            Link link,
            CancellationToken cancellationToken = default) =>
            await UpdateAsync(link, cancellationToken);

        public async ValueTask<Link> DeleteLinkAsync(
            Link link,
            CancellationToken cancellationToken = default) =>
            await DeleteAsync(link, cancellationToken);

        public async ValueTask BulkInsertLinksAsync(
            List<Link> links,
            CancellationToken cancellationToken = default) =>
            await BulkInsertAsync(links, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateLinksAsync(
            List<Link> links,
            CancellationToken cancellationToken = default) =>
            await BulkUpdateAsync(links, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteLinksAsync(
            List<Link> links,
            CancellationToken cancellationToken = default) =>
            await BulkDeleteAsync(links, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<Link>> BulkReadLinksAsync(
            List<Link> links,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(links, cancellationToken);

        public async ValueTask BulkUpsertLinksAsync(
            List<Link> links,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(links, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsLinkAsync(
            Guid linkId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<Link>(new object[] { linkId }, cancellationToken);
    }
}
