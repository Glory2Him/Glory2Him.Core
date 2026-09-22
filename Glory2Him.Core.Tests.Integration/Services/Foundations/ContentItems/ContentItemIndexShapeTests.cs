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
        private const string EffectivePublishedWhenColumn = "EffectivePublishedWhen";
        private const string FeedEffectiveIndex = "IX_ContentItems_FeedEffective";

        private readonly ContentItemIndexQueryBroker broker;

        public ContentItemIndexShapeTests(ContentItemIndexQueryBroker broker) =>
            this.broker = broker;

        /// <summary>
        /// Criterion 2: §DOM11.9 RULES THE PERSISTED COMPUTED COLUMN WARRANTED, so the column
        /// exists, it is materialised, and an index reads it.
        ///
        /// <para><b>The expression is asserted as <c>COALESCE</c>, and that is the whole
        /// assertion rather than a stylistic one.</b> <c>COALESCE</c> normalises to a
        /// <c>CASE</c> tree and <c>ISNULL</c> is an intrinsic that does not, so a column
        /// defined with <c>ISNULL(PublishDate, CreatedWhen)</c> matches nothing a query issuing
        /// <c>COALESCE(...)</c> asks for. The optimiser would simply never use the index, the
        /// table would pay the write cost for nothing, and the plans would look exactly like
        /// the column having been a bad idea.</para>
        ///
        /// <para>The column is reached by no expression in the solution — it is an EF shadow
        /// property, read by the OPTIMISER through expression matching against the feed's own
        /// <c>COALESCE(PublishDate, CreatedWhen)</c>, and it appears on no API contract.</para>
        /// </summary>
        [Fact]
        public async Task
            ShouldAddTheComputedColumnOnlyWhenTheRecordedPositionWarrantsIt_WhenTheMigrationIsAppliedAsync()
        {
            // given
            List<DeployedComputedColumn> computedColumns =
                await this.broker.GetComputedColumnsAsync(ContentItemsTable);

            List<DeployedIndexColumn> indexColumns =
                await this.broker.GetIndexColumnsAsync(ContentItemsTable);

            // when
            DeployedComputedColumn effectivePublishedWhen = computedColumns
                .SingleOrDefault(computedColumn =>
                    computedColumn.ColumnName == EffectivePublishedWhenColumn);

            List<DeployedIndexColumn> feedEffectiveKeys = indexColumns
                .Where(indexColumn =>
                    indexColumn.IndexName == FeedEffectiveIndex
                        && indexColumn.IsIncluded == false)
                .OrderBy(indexColumn => indexColumn.KeyOrdinal)
                .ToList();

            // then
            effectivePublishedWhen.Should().NotBeNull(
                because: "§DOM11.9 rules the persisted computed column warranted");

            effectivePublishedWhen.Definition.Should().Be(
                "(coalesce([PublishDate],[CreatedWhen]))",
                because: "§DOM11.9 records the expression verbatim as COALESCE, and an "
                    + "ISNULL column matches no COALESCE query");

            effectivePublishedWhen.IsPersisted.Should().BeTrue(
                because: "an index may only be built over a materialised value here");

            effectivePublishedWhen.TypeName.Should().Be("datetimeoffset");

            feedEffectiveKeys
                .Select(DescribeKey)
                .Should().Equal(
                    new[] { $"{EffectivePublishedWhenColumn} DESC", "Id DESC" },
                    because: "the index supplies §DOM11.3's order INCLUDING its Id "
                        + "terminator, so the order it serves is total");
        }

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

        private static string DescribeKey(DeployedIndexColumn indexColumn) =>
            $"{indexColumn.ColumnName} {(indexColumn.IsDescending ? "DESC" : "ASC")}";
    }
}
