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
using System.Data.Common;
using System.Threading.Tasks;
using Glory2Him.Core.Brokers.Storages.Sql;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Tests.Integration.Brokers
{
    /// <summary>
    /// A real <see cref="StorageBroker"/> over LocalDB, read for its CATALOGUE METADATA rather
    /// than its rows: which indexes the schema really ends up with, on which columns, in which
    /// order, and with which filter.
    ///
    /// <para>No row is ever written here. The subject is the shape the model declares once
    /// SQL Server has created it, so every read below goes to <c>sys.indexes</c>,
    /// <c>sys.index_columns</c> and <c>sys.computed_columns</c>.</para>
    ///
    /// <para><b>Existence is counted, never inferred from a filter.</b>
    /// <c>AttachmentSchemaQueryBroker.GetIndexFilterDefinitionAsync</c> returns <c>null</c>
    /// both when no index of that name exists and when the index exists but is unfiltered —
    /// <c>sys.indexes.filter_definition</c> is <c>NULL</c> for every non-filtered index — so an
    /// "it was dropped" assertion written on that helper passes before the drop. The indexes
    /// this fixture is built for are all unfiltered, so it reads rows out of
    /// <c>sys.index_columns</c> instead.</para>
    /// </summary>
    public sealed class ContentItemIndexQueryBroker : IDisposable
    {
        // Its own catalogue: this fixture creates and drops a schema, and xUnit runs
        // collections in parallel, so sharing one database would let either drop the other's
        // mid-run.
        private const string CatalogueSuffix = "_ContentItemIndexes";

        private readonly StorageBroker storageBroker;

        public ContentItemIndexQueryBroker()
        {
            this.storageBroker = new StorageBroker(
                IntegrationDatabase.BuildConfiguration(CatalogueSuffix));

            IntegrationDatabase.EnsureSchema(this.storageBroker);
        }

        /// <summary>
        /// Every index column SQL Server created for a table — keys and includes alike, with
        /// the key order and sort direction it stored.
        /// </summary>
        public async ValueTask<List<DeployedIndexColumn>> GetIndexColumnsAsync(string tableName)
        {
            const string commandText =
                @"SELECT i.name AS IndexName,
                         c.name AS ColumnName,
                         ic.key_ordinal AS KeyOrdinal,
                         ic.is_descending_key AS IsDescending,
                         ic.is_included_column AS IsIncluded,
                         i.is_unique AS IsUnique,
                         ISNULL(i.filter_definition, N'') AS FilterDefinition
                  FROM sys.indexes AS i
                  INNER JOIN sys.index_columns AS ic
                      ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                  INNER JOIN sys.columns AS c
                      ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                  WHERE i.object_id = OBJECT_ID(@tableName)
                  ORDER BY i.name, ic.is_included_column, ic.key_ordinal, c.name;";

            var indexColumns = new List<DeployedIndexColumn>();

            await ReadAsync(
                commandText,
                reader => indexColumns.Add(new DeployedIndexColumn
                {
                    IndexName = reader.GetString(0),
                    ColumnName = reader.GetString(1),
                    KeyOrdinal = reader.GetByte(2),
                    IsDescending = reader.GetBoolean(3),
                    IsIncluded = reader.GetBoolean(4),
                    IsUnique = reader.GetBoolean(5),
                    FilterDefinition = reader.GetString(6)
                }),
                ("@tableName", tableName));

            return indexColumns;
        }

        /// <summary>
        /// Every FILTERED index in the whole catalogue, as the name and the predicate SQL
        /// Server stored for it. Catalogue-wide on purpose: the soft-delete filters sit on
        /// eleven different tables and the point is that this change left all of them alone.
        /// </summary>
        public async ValueTask<List<DeployedFilteredIndex>> GetFilteredIndexesAsync()
        {
            const string commandText =
                @"SELECT i.name AS IndexName,
                         i.filter_definition AS FilterDefinition
                  FROM sys.indexes AS i
                  INNER JOIN sys.tables AS t ON t.object_id = i.object_id
                  WHERE i.has_filter = 1
                  ORDER BY i.name;";

            var filteredIndexes = new List<DeployedFilteredIndex>();

            await ReadAsync(
                commandText,
                reader => filteredIndexes.Add(new DeployedFilteredIndex
                {
                    IndexName = reader.GetString(0),
                    FilterDefinition = reader.GetString(1)
                }));

            return filteredIndexes;
        }

        /// <summary>
        /// The computed columns SQL Server created for a table, as the definition it stored and
        /// whether it materialised the value.
        /// </summary>
        public async ValueTask<List<DeployedComputedColumn>> GetComputedColumnsAsync(
            string tableName)
        {
            const string commandText =
                @"SELECT c.name AS ColumnName,
                         cc.definition AS Definition,
                         cc.is_persisted AS IsPersisted,
                         t.name AS TypeName
                  FROM sys.computed_columns AS cc
                  INNER JOIN sys.columns AS c
                      ON c.object_id = cc.object_id AND c.column_id = cc.column_id
                  INNER JOIN sys.types AS t ON t.user_type_id = c.user_type_id
                  WHERE cc.object_id = OBJECT_ID(@tableName)
                  ORDER BY c.name;";

            var computedColumns = new List<DeployedComputedColumn>();

            await ReadAsync(
                commandText,
                reader => computedColumns.Add(new DeployedComputedColumn
                {
                    ColumnName = reader.GetString(0),
                    Definition = reader.GetString(1),
                    IsPersisted = reader.GetBoolean(2),
                    TypeName = reader.GetString(3)
                }),
                ("@tableName", tableName));

            return computedColumns;
        }

        private async ValueTask ReadAsync(
            string commandText,
            Action<DbDataReader> readRow,
            params (string Name, string Value)[] parameters)
        {
            DbConnection connection = this.storageBroker.Database.GetDbConnection();

            await this.storageBroker.Database.OpenConnectionAsync();

            try
            {
                using DbCommand command = connection.CreateCommand();
                command.CommandText = commandText;

                foreach ((string name, string value) in parameters)
                {
                    DbParameter parameter = command.CreateParameter();
                    parameter.ParameterName = name;
                    parameter.Value = value;
                    command.Parameters.Add(parameter);
                }

                using DbDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    readRow(reader);
                }
            }
            finally
            {
                await this.storageBroker.Database.CloseConnectionAsync();
            }
        }

        // xUnit disposes a collection fixture once, after the last test in the collection.
        public void Dispose()
        {
            IntegrationDatabase.Drop(this.storageBroker);
            this.storageBroker.Dispose();
        }
    }

    public sealed class DeployedIndexColumn
    {
        public string IndexName { get; set; }
        public string ColumnName { get; set; }
        public byte KeyOrdinal { get; set; }
        public bool IsDescending { get; set; }
        public bool IsIncluded { get; set; }
        public bool IsUnique { get; set; }
        public string FilterDefinition { get; set; }
    }

    public sealed class DeployedFilteredIndex
    {
        public string IndexName { get; set; }
        public string FilterDefinition { get; set; }
    }

    public sealed class DeployedComputedColumn
    {
        public string ColumnName { get; set; }
        public string Definition { get; set; }
        public bool IsPersisted { get; set; }
        public string TypeName { get; set; }
    }

    /// <summary>
    /// Binds <see cref="ContentItemIndexQueryBroker"/> to a collection so xUnit builds it once,
    /// shares it across every test in the collection, and disposes it once at the end.
    /// </summary>
    [CollectionDefinition(ContentItemIndexCollection.Name)]
    public sealed class ContentItemIndexCollection
        : ICollectionFixture<ContentItemIndexQueryBroker>
    {
        public const string Name = "ContentItem index integration";
    }
}
