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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Tests.Integration.Brokers
{
    /// <summary>
    /// A real <see cref="StorageBroker"/> over LocalDB for the SortOrder store-default tests.
    ///
    /// <para>No service is wired in on purpose. What is under test is what the DATABASE ends up
    /// holding after an insert EF composed, and every layer above the broker is mocked in the
    /// unit suite — which is exactly why #395 reached a fresh database unnoticed.</para>
    ///
    /// <para>Every read below goes through raw SQL rather than through the tracked entity. The
    /// entity keeps the value the caller set whatever the database did with it, so reading it
    /// back would assert the caller's own input and pass against the bug.</para>
    /// </summary>
    public sealed class ContentItemSettingQueryBroker : IDisposable
    {
        // Its own catalogue: xUnit runs collections in parallel and each fixture here creates
        // and DROPS a schema, so a shared database would let one delete another's rows mid-run.
        private const string CatalogueSuffix = "_ContentItemSettings";

        private readonly StorageBroker storageBroker;

        public ContentItemSettingQueryBroker()
        {
            this.storageBroker = new StorageBroker(
                IntegrationDatabase.BuildConfiguration(CatalogueSuffix));

            IntegrationDatabase.EnsureSchema(this.storageBroker);
        }

        /// <summary>
        /// Inserts through the broker — the same call, on the same model, that
        /// ContentItemSettingSeedData makes at startup.
        /// </summary>
        public async ValueTask InsertAsync(ContentItemSetting contentItemSetting) =>
            await this.storageBroker.InsertContentItemSettingAsync(
                contentItemSetting, CancellationToken.None);

        /// <summary>
        /// Inserts a row in raw SQL naming no SortOrder, which is the one caller the column
        /// default still serves — a hand-written script, or a migration backfill.
        /// </summary>
        public async ValueTask InsertNamingNoSortOrderAsync(Guid id, ContentType contentType)
        {
            string contentTypeName = contentType.ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string authoredBy = "integration";

            await this.storageBroker.Database.ExecuteSqlAsync(
                $@"INSERT INTO [ContentItemSettings]
                       ([Id], [ContentType], [CreatedBy], [CreatedWhen], [UpdatedBy], [UpdatedWhen])
                   VALUES
                       ({id}, {contentTypeName}, {authoredBy}, {now}, {authoredBy}, {now})");
        }

        /// <summary>
        /// Reads the value the COLUMN holds, straight out of the table and past the change
        /// tracker.
        /// </summary>
        public async ValueTask<int> GetStoredSortOrderAsync(Guid id)
        {
            List<int> storedSortOrders = await this.storageBroker.Database
                .SqlQuery<int>(
                    $@"SELECT [SortOrder] AS [Value]
                       FROM [ContentItemSettings]
                       WHERE [Id] = {id}")
                .ToListAsync();

            return storedSortOrders[0];
        }

        /// <summary>
        /// Builds the default-scope row for a content type — the shape the seed writes: no
        /// ContentItemId, so it lands in the per-type default scope that
        /// UX_ContentItemSettings_DefaultPerType owns.
        /// </summary>
        public static ContentItemSetting CreateDefaultSetting(ContentType contentType)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return new ContentItemSetting
            {
                Id = Guid.NewGuid(),
                ContentType = contentType,
                ContentItemId = null,
                ContentTypeName = contentType.ToString(),
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
        public async ValueTask ClearAsync(IEnumerable<Guid> contentItemSettingIds)
        {
            foreach (Guid contentItemSettingId in contentItemSettingIds)
            {
                await this.storageBroker.Database.ExecuteSqlAsync(
                    $"DELETE FROM [ContentItemSettings] WHERE [Id] = {contentItemSettingId}");
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
    }

    /// <summary>
    /// Binds <see cref="ContentItemSettingQueryBroker"/> to a collection so xUnit builds it
    /// once, shares it across every test in the collection, and disposes it once at the end.
    /// </summary>
    [CollectionDefinition(ContentItemSettingCollection.Name)]
    public sealed class ContentItemSettingCollection
        : ICollectionFixture<ContentItemSettingQueryBroker>
    {
        public const string Name = "ContentItemSetting schema integration";
    }
}
