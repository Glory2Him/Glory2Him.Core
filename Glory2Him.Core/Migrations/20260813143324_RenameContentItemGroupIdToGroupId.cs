using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class RenameContentItemGroupIdToGroupId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ContentItemGroupId",
                table: "Links",
                newName: "GroupId");

            migrationBuilder.RenameIndex(
                name: "UX_Links_ContentItemGroupId_Version",
                table: "Links",
                newName: "UX_Links_GroupId_Version");

            migrationBuilder.RenameIndex(
                name: "UX_Links_ContentItemGroupId_IsPublished",
                table: "Links",
                newName: "UX_Links_GroupId_IsPublished");

            migrationBuilder.RenameIndex(
                name: "UX_Links_ContentItemGroupId_G2Hatest",
                table: "Links",
                newName: "UX_Links_GroupId_IsLatestVersion");

            migrationBuilder.RenameColumn(
                name: "ContentItemGroupId",
                table: "ContentItems",
                newName: "GroupId");

            migrationBuilder.RenameIndex(
                name: "IX_ContentItems_ContentItemGroupId_VersionDesc",
                table: "ContentItems",
                newName: "IX_ContentItems_GroupId_VersionDesc");

            migrationBuilder.RenameIndex(
                name: "IX_ContentItem_G2Hatest",
                table: "ContentItems",
                newName: "IX_ContentItem_IsLatestVersion");

            migrationBuilder.RenameColumn(
                name: "ContentItemGroupId",
                table: "Attachments",
                newName: "GroupId");

            migrationBuilder.RenameIndex(
                name: "UX_Attachments_ContentItemGroupId_Version",
                table: "Attachments",
                newName: "UX_Attachments_GroupId_Version");

            migrationBuilder.RenameIndex(
                name: "UX_Attachments_ContentItemGroupId_IsPublished",
                table: "Attachments",
                newName: "UX_Attachments_GroupId_IsPublished");

            migrationBuilder.RenameIndex(
                name: "UX_Attachments_ContentItemGroupId_G2Hatest",
                table: "Attachments",
                newName: "UX_Attachments_GroupId_IsLatestVersion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GroupId",
                table: "Links",
                newName: "ContentItemGroupId");

            migrationBuilder.RenameIndex(
                name: "UX_Links_GroupId_Version",
                table: "Links",
                newName: "UX_Links_ContentItemGroupId_Version");

            migrationBuilder.RenameIndex(
                name: "UX_Links_GroupId_IsPublished",
                table: "Links",
                newName: "UX_Links_ContentItemGroupId_IsPublished");

            migrationBuilder.RenameIndex(
                name: "UX_Links_GroupId_IsLatestVersion",
                table: "Links",
                newName: "UX_Links_ContentItemGroupId_G2Hatest");

            migrationBuilder.RenameColumn(
                name: "GroupId",
                table: "ContentItems",
                newName: "ContentItemGroupId");

            migrationBuilder.RenameIndex(
                name: "IX_ContentItems_GroupId_VersionDesc",
                table: "ContentItems",
                newName: "IX_ContentItems_ContentItemGroupId_VersionDesc");

            migrationBuilder.RenameIndex(
                name: "IX_ContentItem_IsLatestVersion",
                table: "ContentItems",
                newName: "IX_ContentItem_G2Hatest");

            migrationBuilder.RenameColumn(
                name: "GroupId",
                table: "Attachments",
                newName: "ContentItemGroupId");

            migrationBuilder.RenameIndex(
                name: "UX_Attachments_GroupId_Version",
                table: "Attachments",
                newName: "UX_Attachments_ContentItemGroupId_Version");

            migrationBuilder.RenameIndex(
                name: "UX_Attachments_GroupId_IsPublished",
                table: "Attachments",
                newName: "UX_Attachments_ContentItemGroupId_IsPublished");

            migrationBuilder.RenameIndex(
                name: "UX_Attachments_GroupId_IsLatestVersion",
                table: "Attachments",
                newName: "UX_Attachments_ContentItemGroupId_G2Hatest");
        }
    }
}
