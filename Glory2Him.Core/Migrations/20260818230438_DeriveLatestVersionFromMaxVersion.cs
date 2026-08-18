using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class DeriveLatestVersionFromMaxVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Links_GroupId_IsLatestVersion",
                table: "Links");

            migrationBuilder.DropIndex(
                name: "IX_ContentItem_IsLatestVersion",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "UX_Attachments_GroupId_IsLatestVersion",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "IsLatestVersion",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "IsLatestVersion",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "IsLatestVersion",
                table: "Attachments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLatestVersion",
                table: "Links",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLatestVersion",
                table: "ContentItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLatestVersion",
                table: "Attachments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "UX_Links_GroupId_IsLatestVersion",
                table: "Links",
                columns: new[] { "GroupId", "IsLatestVersion" },
                unique: true,
                filter: "[IsLatestVersion] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItem_IsLatestVersion",
                table: "ContentItems",
                columns: new[] { "GroupId", "IsLatestVersion" },
                unique: true,
                filter: "[IsLatestVersion] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_Attachments_GroupId_IsLatestVersion",
                table: "Attachments",
                columns: new[] { "GroupId", "IsLatestVersion" },
                unique: true,
                filter: "[IsLatestVersion] = 1");
        }
    }
}
