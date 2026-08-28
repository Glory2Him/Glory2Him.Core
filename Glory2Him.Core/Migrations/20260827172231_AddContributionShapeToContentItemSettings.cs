using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddContributionShapeToContentItemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasAuthor",
                table: "ContentItemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasTitle",
                table: "ContentItemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailableAsGeneralUserContribution",
                table: "ContentItemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasAuthor",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "HasTitle",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "IsAvailableAsGeneralUserContribution",
                table: "ContentItemSettings");
        }
    }
}
