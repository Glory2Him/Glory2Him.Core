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

        /// <summary>
        /// Criterion 3: §DOM11.9 LEAVES BOTH FEED INDEXES UNDECIDED, so both survive exactly
        /// as they stood at the branch point. Undecided is not "drop" and this change does not
        /// guess; the position measured one predicate shape on one selectivity and says nothing
        /// about whichever other query evaluates either index's cited purpose.
        ///
        /// <para>Keys and key order alone do not settle "unchanged": an index that keeps both
        /// while becoming unique, or while gaining an <c>INCLUDE</c>, is a different index with
        /// a different write cost. Uniqueness, includes and the filter are asserted too.</para>
        /// </summary>
        [Theory]
        [InlineData("IX_ContentItems_PublishDate", "PublishDate ASC")]
        [InlineData(
            "IX_ContentItems_Feed",
            "ApprovalStatus ASC|IsPublished ASC|PublishDate ASC")]
        public async Task ShouldLeaveEachFeedIndexInTheStateTheRecordedPositionNames_AfterTheMigrationAsync(
            string indexName,
            string expectedKeys)
        {
            // given
            List<DeployedIndexColumn> indexColumns =
                await this.broker.GetIndexColumnsAsync(ContentItemsTable);

            // when
            List<DeployedIndexColumn> declaredColumns = indexColumns
                .Where(indexColumn => indexColumn.IndexName == indexName)
                .ToList();

            List<DeployedIndexColumn> keys = declaredColumns
                .Where(indexColumn => indexColumn.IsIncluded == false)
                .OrderBy(indexColumn => indexColumn.KeyOrdinal)
                .ToList();

            // then
            keys.Should().NotBeEmpty(
                because: $"§DOM11.9 leaves {indexName} undecided, so it survives");

            string.Join("|", keys.Select(DescribeKey)).Should().Be(
                expectedKeys,
                because: "an undecided index keeps the keys and the key order it had at the "
                    + "branch point");

            declaredColumns.Where(indexColumn => indexColumn.IsIncluded)
                .Should().BeEmpty(
                    because: "an index that gains an INCLUDE is a different index with a "
                        + "different write cost");

            declaredColumns.Should().OnlyContain(
                indexColumn => indexColumn.IsUnique == false,
                because: "an index that becomes unique is a different index and a new "
                    + "constraint on the rows");

            declaredColumns.Should().OnlyContain(
                indexColumn => indexColumn.FilterDefinition == string.Empty,
                because: "neither feed index was filtered at the branch point");
        }

        /// <summary>
        /// Criterion 5: THE FOURTEEN SOFT-DELETE FILTERED INDEXES ARE OUT OF SCOPE AND STAY
        /// THAT WAY, AND EXACTLY ONE FILTERED INDEX JOINS THEM. Read catalogue-wide rather
        /// than per table, and as the (name, filter) PAIRS SQL Server stored rather than as a
        /// count of them: a count survives a diff that renames one index or rewrites one
        /// filter into a different filter still carrying the term, which is exactly the diff
        /// this criterion exists to refuse.
        ///
        /// <para><b>Catalogue-wide and not an allowlist of the fourteen.</b> An allowlist
        /// cannot see a name it does not already list, so a stray fifteenth filtered index —
        /// or the ruled one misspelled, or the ruled one carrying the wrong filter — would
        /// pass unseen. The whole selection is read and the single permitted addition is named
        /// here instead.</para>
        /// </summary>
        [Fact]
        public async Task ShouldLeaveTheFourteenSoftDeleteFilteredIndexesUnchanged_AfterTheMigrationAsync()
        {
            // given
            var expectedPairs = new[]
            {
                "IX_ContentItem_IsPublished	([IsPublished]=(1) AND [IsDeleted]=(0))",

                // The one permitted addition, named rather than counted: the ruled feed index
                // and nothing else, carrying §SEC14.1's first term as its filter.
                "IX_ContentItems_FeedEffective	([IsDeleted]=(0))",

                "UX_AIReviewerAssignments_ApprovalId	([IsDeleted]=(0))",
                "UX_ApprovalReviewRequests_ApprovalId_RequestedUserId	([IsDeleted]=(0))",
                "UX_ApprovalReviews_ApprovalId_CreatedBy	([StatusId]<>(4) AND [IsDeleted]=(0))",
                "UX_ApprovalSettings_AssociationPersonality	([IsPersonal] IS NOT NULL AND [IsDeleted]=(0))",
                "UX_ApprovalSettings_EntityTypeContentType	([ContentType] IS NOT NULL AND [IsDeleted]=(0))",
                "UX_ApprovalSettings_EntityTypeDefault	([EntityType] IS NOT NULL AND [ContentType] IS NULL AND [IsPersonal] IS NULL AND [IsDeleted]=(0))",
                "UX_ApprovalSettings_GlobalDefault	([EntityType] IS NULL AND [IsDeleted]=(0))",
                "UX_Associations_Pair	([IsDeleted]=(0))",
                "UX_Attachments_GroupId_IsPublished	([IsPublished]=(1) AND [IsDeleted]=(0))",
                "UX_BibleReferences_USFM	([IsDeleted]=(0))",
                "UX_ContentItemSettings_DefaultPerType	([ContentItemId] IS NULL AND [IsDeleted]=(0))",
                "UX_ContentItemSettings_OverridePerEntity	([ContentItemId] IS NOT NULL AND [IsDeleted]=(0))",
                "UX_Links_GroupId_IsPublished	([IsPublished]=(1) AND [IsDeleted]=(0))"
            };

            List<DeployedFilteredIndex> filteredIndexes =
                await this.broker.GetFilteredIndexesAsync();

            // when
            List<string> deployedPairs = filteredIndexes
                .Where(filteredIndex =>
                    filteredIndex.FilterDefinition.Contains("[IsDeleted]=(0)"))
                .Select(filteredIndex =>
                    $"{filteredIndex.IndexName}	{filteredIndex.FilterDefinition}")
                .OrderBy(pair => pair, System.StringComparer.Ordinal)
                .ToList();

            // then
            deployedPairs.Should().Equal(
                expectedPairs,
                because: "every one of the fourteen baseline filtered indexes is out of this "
                    + "change's scope, name and filter text alike, and the only addition is "
                    + "the ruled feed index under exactly that name and filter");
        }

        private static string DescribeKey(DeployedIndexColumn indexColumn) =>
            $"{indexColumn.ColumnName} {(indexColumn.IsDescending ? "DESC" : "ASC")}";
    }
}
