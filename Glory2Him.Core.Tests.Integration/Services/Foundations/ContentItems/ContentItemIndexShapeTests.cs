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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Tests.Integration.Brokers;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.ContentItems
{
    /// <summary>
    /// THE SHAPE OF THE ContentItems TABLE, read off the catalogue SQL Server actually built.
    ///
    /// <para>These assertions sit at integration level because nothing above the broker can
    /// see an index. A unit test asserting a <c>HasIndex</c> call would be reading the
    /// configuration back to itself; only the database can say whether the index the model
    /// declares is the index that exists.</para>
    ///
    /// <para>The fixture builds its schema with <c>EnsureCreated</c>, so what is proved here is
    /// what the MODEL declares. That a DEPLOYMENT gets the same shape is proved by a different
    /// pair of checks — <c>dotnet ef migrations has-pending-model-changes</c> reporting none,
    /// and the generated script applying — and all three together are the chain.</para>
    /// </summary>
    [Collection(ContentItemIndexCollection.Name)]
    public sealed class ContentItemIndexShapeTests
    {
        private const string ContentItemsTable = "ContentItems";

        private readonly ContentItemIndexQueryBroker broker;

        public ContentItemIndexShapeTests(ContentItemIndexQueryBroker broker) =>
            this.broker = broker;

        /// <summary>
        /// Criterion 4: NOTHING INDEXES DeletedWhen ANY MORE. Nothing in the solution filters
        /// that column — every soft-delete predicate, including all fourteen filtered indexes,
        /// tests <c>IsDeleted</c> — so the only index on it earns nothing and is dropped.
        ///
        /// <para>Included columns are checked as well as keys. A key-list-only reading would
        /// permit an index carrying <c>DeletedWhen</c> as an <c>INCLUDE</c>, which the same
        /// reason refuses just as squarely, and <c>sys.index_columns</c> reads both either
        /// way.</para>
        /// </summary>
        [Fact]
        public async Task ShouldDropTheOnlyIndexOnDeletedWhen_AfterTheMigrationAsync()
        {
            // given
            List<DeployedIndexColumn> indexColumns =
                await this.broker.GetIndexColumnsAsync(ContentItemsTable);

            // when
            List<string> indexesTouchingDeletedWhen = indexColumns
                .Where(indexColumn => indexColumn.ColumnName == "DeletedWhen")
                .Select(indexColumn => indexColumn.IndexName)
                .Distinct()
                .ToList();

            // then
            indexesTouchingDeletedWhen.Should().BeEmpty(
                because: "no index on ContentItems may key on or include DeletedWhen — "
                    + "nothing in the solution filters that column");
        }
    }
}
