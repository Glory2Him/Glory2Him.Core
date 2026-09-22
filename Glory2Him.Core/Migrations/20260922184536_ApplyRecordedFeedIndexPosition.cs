using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <summary>
    /// Applies the feed index position §DOM11.9 records, and drops the only index on
    /// <c>DeletedWhen</c>. One migration rather than three, so the table is touched once.
    ///
    /// <para><b>The index is the shape §DOM11.9's figures price</b> — keys
    /// <c>([EffectivePublishedWhen] DESC, [Id] DESC)</c>, <c>INCLUDE ([ApprovalStatus],
    /// [IsPublished], [PublishDate], [ContentType])</c>, <c>WHERE [IsDeleted] = 0</c>. The
    /// filter is §SEC14.1's first term, which every filtered index on this schema already
    /// carries and which <c>ContentItems</c> already carries in
    /// <c>IX_ContentItem_IsPublished</c>.</para>
    ///
    /// <para><b>The <c>DeletedWhen</c> drop is not §DOM11.9's.</b> That position says nothing
    /// about it. The reason is the one §592 states: nothing in the solution filters that
    /// column. Every soft-delete predicate, including all fourteen filtered indexes, tests
    /// <c>IsDeleted</c>, so the index was read by nothing.</para>
    ///
    /// <para><b>§DOM11.9 leaves <c>IX_ContentItems_PublishDate</c> and
    /// <c>IX_ContentItems_Feed</c> undecided</b>, so neither is touched here. Undecided is
    /// not "drop", and this migration does not guess.</para>
    ///
    /// <para><b>No row state can make this fail.</b> It creates no unique index, no
    /// constraint, no <c>NOT NULL</c> column and no backfilled default, and
    /// <c>COALESCE</c> over two <c>datetimeoffset</c> columns throws for no row. That
    /// matters because a failed <c>Database.Migrate()</c> is caught and logged at startup
    /// rather than stopping the host, so a migration that can fail on data fails silently,
    /// in production, hours later.</para>
    /// </summary>
    public partial class ApplyRecordedFeedIndexPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContentItems_DeletedWhen",
                table: "ContentItems");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EffectivePublishedWhen",
                table: "ContentItems",
                type: "datetimeoffset",
                nullable: true,
                computedColumnSql: "COALESCE([PublishDate], [CreatedWhen])",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_FeedEffective",
                table: "ContentItems",
                columns: new[] { "EffectivePublishedWhen", "Id" },
                descending: new bool[0],
                filter: "[IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "ApprovalStatus", "IsPublished", "PublishDate", "ContentType" });
        }

        /// <summary>
        /// Restores the shape that stood before <c>Up</c>: the new index and its column go,
        /// and <c>IX_ContentItems_DeletedWhen</c> comes back on the same single key.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContentItems_FeedEffective",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "EffectivePublishedWhen",
                table: "ContentItems");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_DeletedWhen",
                table: "ContentItems",
                column: "DeletedWhen");
        }
    }
}
