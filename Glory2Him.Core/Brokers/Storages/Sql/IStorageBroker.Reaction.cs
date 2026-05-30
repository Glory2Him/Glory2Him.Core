// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.Reactions;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<Reaction> InsertReactionAsync(Reaction reaction, CancellationToken cancellationToken = default);
        ValueTask<IQueryable<Reaction>> SelectAllReactionsAsync();
        ValueTask<Reaction> SelectReactionByIdAsync(Guid reactionId, CancellationToken cancellationToken = default);
        ValueTask<Reaction> UpdateReactionAsync(Reaction reaction, CancellationToken cancellationToken = default);
        ValueTask<Reaction> DeleteReactionAsync(Reaction reaction, CancellationToken cancellationToken = default);
        ValueTask BulkInsertReactionsAsync(List<Reaction> reactions, CancellationToken cancellationToken = default);
        ValueTask BulkUpdateReactionsAsync(List<Reaction> reactions, CancellationToken cancellationToken = default);
        ValueTask BulkDeleteReactionsAsync(List<Reaction> reactions, CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<Reaction>> BulkReadReactionsAsync(
            List<Reaction> reactions,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertReactionsAsync(
            List<Reaction> reactions,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsReactionAsync(
            Guid reactionId,
            CancellationToken cancellationToken = default);
    }
}
