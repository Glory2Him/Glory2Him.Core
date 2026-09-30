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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Foundations.Reactions;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Tests.Integration.Brokers
{
    /// <summary>
    /// A real <see cref="StorageBroker"/> over LocalDB for the Reaction SortOrder tests.
    ///
    /// <para>No service is wired in on purpose. What is under test is what the DATABASE ends up
    /// holding, and every layer above the broker is mocked in the unit suite.</para>
    ///
    /// <para>Every read below goes through raw SQL rather than through the tracked entity. The
    /// entity keeps the value the caller set whatever the database did with it, so reading it
    /// back would assert the caller's own input.</para>
    /// </summary>
    public sealed class ReactionQueryBroker : IDisposable
    {
        // Its own catalogue: xUnit runs collections in parallel and each fixture here creates
        // and DROPS a schema, so a shared database would let one delete another's rows mid-run.
        private const string CatalogueSuffix = "_Reactions";

        private readonly StorageBroker storageBroker;

        public ReactionQueryBroker()
        {
            this.storageBroker = new StorageBroker(
                IntegrationDatabase.BuildConfiguration(CatalogueSuffix));

            IntegrationDatabase.EnsureSchema(this.storageBroker);
        }

        /// <summary>
        /// Inserts through the broker — the same call, on the same model, that
        /// ReactionSeedData makes at startup.
        /// </summary>
        public async ValueTask InsertAsync(Reaction reaction) =>
            await this.storageBroker.InsertReactionAsync(reaction, CancellationToken.None);

        /// <summary>
        /// Reads the value the COLUMN holds, straight out of the table and past the change
        /// tracker.
        /// </summary>
        public async ValueTask<int> GetStoredSortOrderAsync(Guid id) =>
            await GetStoredSortOrderAsync(this.storageBroker, id);

        /// <summary>
        /// Builds a reaction nobody set an order on. The name is unique per call because
        /// IX_Reactions_Name is.
        /// </summary>
        public static Reaction CreateReaction()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Guid id = Guid.NewGuid();

            return new Reaction
            {
                Id = id,
                Name = id.ToString("N")[..30],
                UnicodeEmoji = "🙂",
                CreatedBy = "integration",
                CreatedWhen = now,
                UpdatedBy = "integration",
                UpdatedWhen = now
            };
        }

        /// <summary>
        /// Removes every row a test left behind, by id and in raw SQL so it reaches the rows
        /// the change tracker never knew about.
        /// </summary>
        public async ValueTask ClearAsync(IEnumerable<Guid> reactionIds)
        {
            foreach (Guid reactionId in reactionIds)
            {
                await this.storageBroker.Database.ExecuteSqlAsync(
                    $"DELETE FROM [Reactions] WHERE [Id] = {reactionId}");
            }

            // The fixture's context outlives the test. Leaving the inserted entities tracked
            // would put them back in the next SaveChanges, against rows just deleted.
            this.storageBroker.ChangeTracker.Clear();
        }

        // xUnit disposes a collection fixture once, after the last test in the collection.
        public void Dispose()
        {
            IntegrationDatabase.Drop(this.storageBroker);
            this.storageBroker.Dispose();
        }

        private static async ValueTask<int> GetStoredSortOrderAsync(
            StorageBroker storageBroker, Guid id)
        {
            List<int> storedSortOrders = await storageBroker.Database
                .SqlQuery<int>(
                    $@"SELECT [SortOrder] AS [Value]
                       FROM [Reactions]
                       WHERE [Id] = {id}")
                .ToListAsync();

            return storedSortOrders[0];
        }
    }

    /// <summary>
    /// Binds <see cref="ReactionQueryBroker"/> to a collection so xUnit builds it once, shares
    /// it across every test in the collection, and disposes it once at the end.
    /// </summary>
    [CollectionDefinition(ReactionCollection.Name)]
    public sealed class ReactionCollection : ICollectionFixture<ReactionQueryBroker>
    {
        public const string Name = "Reaction schema integration";
    }
}
