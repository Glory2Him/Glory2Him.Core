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
using Glory2Him.Core.Models.Foundations.Reactions;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<Reaction> Reactions { get; set; }

        public async ValueTask<Reaction> InsertReactionAsync(Reaction reaction) =>
            await InsertAsync(reaction);

        public async ValueTask<IQueryable<Reaction>> SelectAllReactionsAsync() =>
            await SelectAllAsync<Reaction>();

        public async ValueTask<Reaction> SelectReactionByIdAsync(Guid reactionId) =>
            await SelectAsync<Reaction>(reactionId);

        public async ValueTask<Reaction> UpdateReactionAsync(Reaction reaction) =>
            await UpdateAsync(reaction);

        public async ValueTask<Reaction> DeleteReactionAsync(Reaction reaction) =>
            await DeleteAsync(reaction);

        public async ValueTask BulkInsertReactionsAsync(List<Reaction> reactions) =>
            await BulkInsertAsync(reactions);

        public async ValueTask BulkUpdateReactionsAsync(List<Reaction> reactions) =>
            await BulkUpdateAsync(reactions);

        public async ValueTask BulkDeleteReactionsAsync(List<Reaction> reactions) =>
            await BulkDeleteAsync(reactions);
    }
}