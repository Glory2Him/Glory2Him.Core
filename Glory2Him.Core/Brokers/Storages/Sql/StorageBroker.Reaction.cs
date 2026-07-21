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
using Glory2Him.Core.Models.Foundations.Reactions;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<Reaction> Reactions { get; set; }

        public async ValueTask<Reaction> InsertReactionAsync(
            Reaction reaction,
            CancellationToken cancellationToken = default) =>
            await InsertAsync(reaction, cancellationToken);

        public async ValueTask<IQueryable<Reaction>> SelectAllReactionsAsync() =>
            await SelectAllAsync<Reaction>();

        public async ValueTask<Reaction> SelectReactionByIdAsync(
            Guid reactionId,
            CancellationToken cancellationToken = default) =>
            await SelectAsync<Reaction>(new object[] { reactionId }, cancellationToken);

        public async ValueTask<Reaction> UpdateReactionAsync(
            Reaction reaction,
            CancellationToken cancellationToken = default) =>
            await UpdateAsync(reaction, cancellationToken);

        public async ValueTask<Reaction> DeleteReactionAsync(
            Reaction reaction,
            CancellationToken cancellationToken = default) =>
            await DeleteAsync(reaction, cancellationToken);

        public async ValueTask BulkInsertReactionsAsync(
            List<Reaction> reactions,
            CancellationToken cancellationToken = default) =>
            await BulkInsertAsync(reactions, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateReactionsAsync(
            List<Reaction> reactions,
            CancellationToken cancellationToken = default) =>
            await BulkUpdateAsync(reactions, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteReactionsAsync(
            List<Reaction> reactions,
            CancellationToken cancellationToken = default) =>
            await BulkDeleteAsync(reactions, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<Reaction>> BulkReadReactionsAsync(
            List<Reaction> reactions,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(reactions, cancellationToken);

        public async ValueTask BulkUpsertReactionsAsync(
            List<Reaction> reactions,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(reactions, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsReactionAsync(
            Guid reactionId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<Reaction>(new object[] { reactionId }, cancellationToken);
    }
}