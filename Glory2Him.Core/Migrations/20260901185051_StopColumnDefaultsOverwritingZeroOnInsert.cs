using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class StopColumnDefaultsOverwritingZeroOnInsert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NO SCHEMA CHANGE, AND THAT IS THE WHOLE POINT.
            //
            // The fix behind this migration is model-side: SortOrder, and the three Version
            // columns, are now ValueGeneratedNever, so EF stops omitting a CLR-default 0 from
            // the insert and letting the column default win (#395). The columns keep their
            // defaults, so there is nothing to alter — this migration exists to carry the
            // snapshot, which the build compares the model against.
            //
            // The repair below is the part with teeth. AddSortOrderToContentItemSettings
            // backfilled the curated order, but on a database created AFTER it the backfill ran
            // against an empty table and the seed that followed inserted Quote's 0 straight into
            // the column default. Those environments — a rebuilt local database, a new
            // developer, a fresh acceptance store — carry Quote at 1000 and will never be
            // corrected by the seed, which only inserts a default that is MISSING.
            //
            // Narrowed to a row that still holds the default it was given by accident. A Quote
            // default somebody has deliberately moved is not 1000, so this leaves it alone.
            migrationBuilder.Sql(@"
                UPDATE [ContentItemSettings]
                SET [SortOrder] = 0
                WHERE [ContentType] = N'Quote'
                  AND [ContentItemId] IS NULL
                  AND [SortOrder] = 1000;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately empty. There is no schema to put back, and moving Quote to the end
            // of the picker again would be reinstating the defect rather than reversing a
            // change — the value 0 is what the seed always meant to write.
        }
    }
}
