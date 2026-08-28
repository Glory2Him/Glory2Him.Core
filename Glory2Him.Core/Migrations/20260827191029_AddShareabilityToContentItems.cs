using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddShareabilityToContentItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SharePermission",
                table: "ContentItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShareabilityBasis",
                table: "ContentItems",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Owned");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SharePermission",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "ShareabilityBasis",
                table: "ContentItems");
        }
    }
}
