using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class FilterPublishedSlotIndexesOnLiveRows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Narrowing only. Every row the old filter indexed is either still indexed or
            // now excluded, so no existing data can violate the new predicate and this half
            // needs no data fix-up. The rollback below is the half that does.
            migrationBuilder.DropIndex(
                name: "UX_Links_GroupId_IsPublished",
                table: "Links");

            migrationBuilder.DropIndex(
                name: "IX_ContentItem_IsPublished",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "UX_Attachments_GroupId_IsPublished",
                table: "Attachments");

            migrationBuilder.CreateIndex(
                name: "UX_Links_GroupId_IsPublished",
                table: "Links",
                column: "GroupId",
                unique: true,
                filter: "[IsPublished] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItem_IsPublished",
                table: "ContentItems",
                column: "GroupId",
                unique: true,
                filter: "[IsPublished] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Attachments_GroupId_IsPublished",
                table: "Attachments",
                column: "GroupId",
                unique: true,
                filter: "[IsPublished] = 1 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Links_GroupId_IsPublished",
                table: "Links");

            migrationBuilder.DropIndex(
                name: "IX_ContentItem_IsPublished",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "UX_Attachments_GroupId_IsPublished",
                table: "Attachments");

            // The old filter cannot simply be recreated over the data the new one permits.
            // A group may now hold a live published row AND a soft-deleted row still carrying
            // IsPublished, which the flag-only index counts as two — the create fails with
            // error 1505 and the rollback aborts part-applied.
            //
            // Clearing the flag on removed rows is not a workaround invented for the rollback:
            // §9.7.6 rule 1 says a removed row must not keep IsPublished at all, so this only
            // settles rows that should never have been in that state. Nothing is undeleted and
            // no live row is touched.
            migrationBuilder.Sql(
                "UPDATE [Links] SET [IsPublished] = 0 "
                    + "WHERE [IsDeleted] = 1 AND [IsPublished] = 1;");

            migrationBuilder.Sql(
                "UPDATE [ContentItems] SET [IsPublished] = 0 "
                    + "WHERE [IsDeleted] = 1 AND [IsPublished] = 1;");

            migrationBuilder.Sql(
                "UPDATE [Attachments] SET [IsPublished] = 0 "
                    + "WHERE [IsDeleted] = 1 AND [IsPublished] = 1;");

            migrationBuilder.CreateIndex(
                name: "UX_Links_GroupId_IsPublished",
                table: "Links",
                columns: new[] { "GroupId", "IsPublished" },
                unique: true,
                filter: "[IsPublished] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItem_IsPublished",
                table: "ContentItems",
                columns: new[] { "GroupId", "IsPublished" },
                unique: true,
                filter: "[IsPublished] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_Attachments_GroupId_IsPublished",
                table: "Attachments",
                columns: new[] { "GroupId", "IsPublished" },
                unique: true,
                filter: "[IsPublished] = 1");
        }
    }
}
