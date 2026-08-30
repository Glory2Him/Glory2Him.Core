using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddSortOrderToContentItemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "ContentItemSettings",
                type: "int",
                nullable: false,
                defaultValue: 1000);

            // THE BACKFILL IS THE POINT OF THIS MIGRATION, not the column.
            //
            // ContentItemSettingSeedData writes these same values, but it only inserts a default
            // that is MISSING — every environment that has already booted keeps the rows it has.
            // Without the update below all eight defaults would sit on the column default of
            // 1000, every tile would tie, and the picker would fall back to the arbitrary order
            // it has today. The values match the seed exactly; change both together.
            //
            // Scoped to the type DEFAULTS (ContentItemId IS NULL). A per-item override is never
            // a tile, so it keeps 1000 rather than being given a presentation order it has no
            // surface for. ContentType is persisted as a string (design §3.7), so it is matched
            // by member name here.
            migrationBuilder.Sql(@"
                UPDATE [ContentItemSettings]
                SET [SortOrder] =
                    CASE [ContentType]
                        WHEN 'Quote' THEN 0
                        WHEN 'Story' THEN 1
                        WHEN 'Testimony' THEN 2
                        WHEN 'Devotional' THEN 3
                        WHEN 'BibleStudy' THEN 4
                        WHEN 'BlogPost' THEN 5
                        WHEN 'Series' THEN 100
                        WHEN 'Topic' THEN 200
                        ELSE [SortOrder]
                    END
                WHERE [ContentItemId] IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "ContentItemSettings");
        }
    }
}
