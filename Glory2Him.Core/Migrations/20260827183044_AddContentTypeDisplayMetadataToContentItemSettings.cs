using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddContentTypeDisplayMetadataToContentItemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentTypeDescription",
                table: "ContentItemSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentTypeIconCssClass",
                table: "ContentItemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentTypeName",
                table: "ContentItemSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentTypeDescription",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "ContentTypeIconCssClass",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "ContentTypeName",
                table: "ContentItemSettings");
        }
    }
}
