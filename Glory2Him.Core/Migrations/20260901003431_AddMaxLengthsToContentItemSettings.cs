using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxLengthsToContentItemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxAuthorLength",
                table: "ContentItemSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxContentLength",
                table: "ContentItemSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxTitleLength",
                table: "ContentItemSettings",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxAuthorLength",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "MaxContentLength",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "MaxTitleLength",
                table: "ContentItemSettings");
        }
    }
}
