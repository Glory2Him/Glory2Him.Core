using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddSortOrderToReactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Reactions",
                type: "int",
                nullable: false,
                defaultValue: 1000);

            // THE BACKFILL IS THE POINT OF THIS MIGRATION, not the column.
            //
            // ReactionSeedData writes these same values, but it only inserts a reaction that is
            // MISSING — every environment that has already booted keeps the rows it has.
            // Without the update below all five would sit on the column default of 1000 and be
            // presented in an order nobody chose. The values match the seed exactly; change
            // both together.
            //
            // Matched by the seed's fixed ids, not by name: a reaction's name can be changed
            // (§ARC12.3.1 shared rule 2a), its id cannot. Any other reaction keeps 1000.
            //
            // WRAPPED IN EXEC, AND IT HAS TO BE. The idempotent script this repo deploys emits
            // a migration as ONE batch, and SQL Server compiles the whole batch before the
            // ALTER above has added the column — see AddSortOrderToContentItemSettings.
            //
            // Single quotes are doubled for the EXEC literal.
            migrationBuilder.Sql(@"
                EXEC(N'
                    UPDATE [Reactions]
                    SET [SortOrder] =
                        CASE [Id]
                            WHEN ''7b2d90c1-4e6a-4f3b-8d21-000000000001'' THEN 10
                            WHEN ''7b2d90c1-4e6a-4f3b-8d21-000000000002'' THEN 20
                            WHEN ''7b2d90c1-4e6a-4f3b-8d21-000000000003'' THEN 30
                            WHEN ''7b2d90c1-4e6a-4f3b-8d21-000000000004'' THEN 40
                            WHEN ''7b2d90c1-4e6a-4f3b-8d21-000000000005'' THEN 50
                            ELSE [SortOrder]
                        END;
                ');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Reactions");
        }
    }
}
