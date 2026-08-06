using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class DeversionBibleReferenceAndAddUsfmUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_BibleReferences_ContentItemGroupId_G2Hatest",
                table: "BibleReferences");

            migrationBuilder.DropIndex(
                name: "UX_BibleReferences_ContentItemGroupId_IsPublished",
                table: "BibleReferences");

            migrationBuilder.DropIndex(
                name: "UX_BibleReferences_ContentItemGroupId_Version",
                table: "BibleReferences");

            migrationBuilder.DropColumn(
                name: "ContentItemGroupId",
                table: "BibleReferences");

            migrationBuilder.DropColumn(
                name: "IsLatestVersion",
                table: "BibleReferences");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "BibleReferences");

            migrationBuilder.AddColumn<string>(
                name: "USFM",
                table: "BibleReferences",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "UX_BibleReferences_USFM",
                table: "BibleReferences",
                column: "USFM",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_BibleReferences_USFM",
                table: "BibleReferences");

            migrationBuilder.DropColumn(
                name: "USFM",
                table: "BibleReferences");

            migrationBuilder.AddColumn<Guid>(
                name: "ContentItemGroupId",
                table: "BibleReferences",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsLatestVersion",
                table: "BibleReferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "BibleReferences",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "UX_BibleReferences_ContentItemGroupId_G2Hatest",
                table: "BibleReferences",
                columns: new[] { "ContentItemGroupId", "IsLatestVersion" },
                unique: true,
                filter: "[IsLatestVersion] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_BibleReferences_ContentItemGroupId_IsPublished",
                table: "BibleReferences",
                columns: new[] { "ContentItemGroupId", "IsPublished" },
                unique: true,
                filter: "[IsPublished] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_BibleReferences_ContentItemGroupId_Version",
                table: "BibleReferences",
                columns: new[] { "ContentItemGroupId", "Version" },
                unique: true);
        }
    }
}
