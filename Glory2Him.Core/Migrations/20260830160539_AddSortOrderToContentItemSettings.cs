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
            //
            // WRAPPED IN EXEC, AND IT HAS TO BE. `dotnet ef migrations script --idempotent` —
            // how this repo actually deploys, and what the build verifies — emits an entire
            // migration as ONE batch with no GO between its statements. SQL Server compiles a
            // batch before it runs any of it, and deferred name resolution covers missing TABLES
            // only, not a missing column on a table that exists: the UPDATE would fail to compile
            // with "Invalid column name 'SortOrder'" (Msg 207) against the column the statement
            // above it is adding. EXEC defers the parse to execution time, after the ALTER has
            // run. It applies cleanly under EF's own Database.Migrate() either way — that path
            // sends each statement as its own command — so only the script path shows the fault.
            //
            // Single quotes are doubled for the EXEC literal.
            migrationBuilder.Sql(@"
                EXEC(N'
                    UPDATE [ContentItemSettings]
                    SET [SortOrder] =
                        CASE [ContentType]
                            WHEN ''Quote'' THEN 0
                            WHEN ''Story'' THEN 1
                            WHEN ''Testimony'' THEN 2
                            WHEN ''Devotional'' THEN 3
                            WHEN ''BibleStudy'' THEN 4
                            WHEN ''BlogPost'' THEN 5
                            WHEN ''Series'' THEN 100
                            WHEN ''Topic'' THEN 200
                            ELSE [SortOrder]
                        END
                    WHERE [ContentItemId] IS NULL;
                ');");
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
