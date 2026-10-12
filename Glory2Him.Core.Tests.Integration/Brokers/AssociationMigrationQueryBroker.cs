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
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Foundations.Associations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Glory2Him.Core.Tests.Integration.Brokers
{
    /// <summary>
    /// A real <see cref="StorageBroker"/> over an EMPTY LocalDB catalogue of its own, for a test
    /// that builds the Associations schema by migrating it rather than from the current model.
    ///
    /// <para>Every read below goes to the catalogue's own views — <c>sys.columns</c>,
    /// <c>sys.indexes</c>, <c>sys.index_columns</c> — because what is under test is what the
    /// migrations left there. A migration that writes an index by hand is seen by nothing else:
    /// the shared fixtures build their catalogues from the model.</para>
    ///
    /// <para>Dispose it to drop the catalogue.</para>
    /// </summary>
    public sealed class AssociationMigrationQueryBroker : IDisposable
    {
        private const string AssociationsTableName = "Associations";

        private readonly StorageBroker storageBroker;

        private AssociationMigrationQueryBroker(string catalogueSuffix)
        {
            this.storageBroker = new StorageBroker(
                IntegrationDatabase.BuildConfiguration(catalogueSuffix));
        }

        /// <param name="catalogueSuffix">
        /// One per test: each test migrates and drops a catalogue, so two sharing one would
        /// migrate it under each other.
        /// </param>
        public static AssociationMigrationQueryBroker CreateOverAnEmptyDatabase(
            string catalogueSuffix)
        {
            var associationMigrationQueryBroker =
                new AssociationMigrationQueryBroker(catalogueSuffix);

            // drops a stale catalogue from a previous run that reused this process id
            IntegrationDatabase.Drop(associationMigrationQueryBroker.storageBroker);

            return associationMigrationQueryBroker;
        }

        /// <summary>
        /// Migrates the catalogue to the named migration — up to and including it, or back down
        /// to it — through the same migrator Database.Migrate() uses.
        /// </summary>
        public async ValueTask MigrateToAsync(string targetMigration)
        {
            IMigrator migrator = this.storageBroker.GetService<IMigrator>();

            await migrator.MigrateAsync(targetMigration);
        }

        public async ValueTask InsertAsync(params Association[] associations)
        {
            foreach (Association association in associations)
            {
                await this.storageBroker.InsertAssociationAsync(
                    association, CancellationToken.None);
            }
        }

        /// <summary>
        /// Counts the rows the table holds of the given ids, straight out of the table and past
        /// the change tracker.
        /// </summary>
        public async ValueTask<int> CountStoredAsync(params Guid[] associationIds)
        {
            int storedCount = 0;

            foreach (Guid associationId in associationIds)
            {
                List<int> counts = await this.storageBroker.Database
                    .SqlQuery<int>(
                        $@"SELECT COUNT(*) AS [Value]
                           FROM [Associations]
                           WHERE [Id] = {associationId}")
                    .ToListAsync();

                storedCount += counts[0];
            }

            return storedCount;
        }

        /// <summary>
        /// The collation the catalogue reports for a column of the Associations table.
        /// </summary>
        public async ValueTask<string> GetColumnCollationAsync(string columnName)
        {
            List<string> collations = await this.storageBroker.Database
                .SqlQuery<string>(
                    $@"SELECT collation_name AS [Value]
                       FROM sys.columns
                       WHERE object_id = OBJECT_ID({AssociationsTableName})
                         AND name = {columnName}")
                .ToListAsync();

            return collations.Single();
        }

        /// <summary>
        /// Whether an index of that name exists on the Associations table and is unique, or
        /// <c>null</c> when none of that name exists.
        /// </summary>
        public async ValueTask<bool?> GetIndexIsUniqueAsync(string indexName)
        {
            List<bool> uniqueness = await this.storageBroker.Database
                .SqlQuery<bool>(
                    $@"SELECT is_unique AS [Value]
                       FROM sys.indexes
                       WHERE object_id = OBJECT_ID({AssociationsTableName})
                         AND name = {indexName}")
                .ToListAsync();

            return uniqueness.Count == 0 ? null : uniqueness[0];
        }

        /// <summary>
        /// An index's key columns, in key order. Included columns are not key columns, and
        /// carry a key ordinal of zero.
        /// </summary>
        public async ValueTask<List<string>> GetIndexKeyColumnsAsync(string indexName)
        {
            return await this.storageBroker.Database
                .SqlQuery<string>(
                    $@"SELECT columns.name AS [Value]
                       FROM sys.indexes AS indexes
                       JOIN sys.index_columns AS indexColumns
                         ON indexColumns.object_id = indexes.object_id
                        AND indexColumns.index_id = indexes.index_id
                       JOIN sys.columns AS columns
                         ON columns.object_id = indexColumns.object_id
                        AND columns.column_id = indexColumns.column_id
                       WHERE indexes.object_id = OBJECT_ID({AssociationsTableName})
                         AND indexes.name = {indexName}
                         AND indexColumns.key_ordinal > 0
                       ORDER BY indexColumns.key_ordinal")
                .ToListAsync();
        }

        /// <summary>
        /// An index's filter, split into its terms. The catalogue stores a filter in its own
        /// normalised text — <c>([IsDeleted]=(0) AND [UserId] IS NULL)</c> for
        /// <c>[IsDeleted] = 0 AND [UserId] IS NULL</c> — so the terms are read from that text
        /// rather than compared with the string EF was given.
        /// </summary>
        public async ValueTask<List<string>> GetIndexFilterTermsAsync(string indexName)
        {
            List<string> filterDefinitions = await this.storageBroker.Database
                .SqlQuery<string>(
                    $@"SELECT filter_definition AS [Value]
                       FROM sys.indexes
                       WHERE object_id = OBJECT_ID({AssociationsTableName})
                         AND name = {indexName}")
                .ToListAsync();

            string filterDefinition = (filterDefinitions.Single() ?? string.Empty).Trim();

            // the one pair of parentheses the catalogue wraps the whole filter in
            string filterBody = filterDefinition.StartsWith('(') && filterDefinition.EndsWith(')')
                ? filterDefinition[1..^1]
                : filterDefinition;

            return filterBody
                .Split(" AND ", StringSplitOptions.TrimEntries)
                .ToList();
        }

        // the test disposes its own; the catalogue goes with it
        public void Dispose()
        {
            IntegrationDatabase.Drop(this.storageBroker);
            this.storageBroker.Dispose();
        }
    }
}
