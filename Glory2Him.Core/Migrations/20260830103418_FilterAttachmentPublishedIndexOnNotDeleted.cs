using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class FilterAttachmentPublishedIndexOnNotDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Attachments_GroupId_IsPublished",
                table: "Attachments");

            migrationBuilder.CreateIndex(
                name: "UX_Attachments_GroupId_IsPublished",
                table: "Attachments",
                columns: new[] { "GroupId", "IsPublished" },
                unique: true,
                filter: "[IsPublished] = 1 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Attachments_GroupId_IsPublished",
                table: "Attachments");

            migrationBuilder.CreateIndex(
                name: "UX_Attachments_GroupId_IsPublished",
                table: "Attachments",
                columns: new[] { "GroupId", "IsPublished" },
                unique: true,
                filter: "[IsPublished] = 1");
        }
    }
}
