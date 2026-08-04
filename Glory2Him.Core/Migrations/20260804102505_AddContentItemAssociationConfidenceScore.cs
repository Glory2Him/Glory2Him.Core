using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddContentItemAssociationConfidenceScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssociationConfidenceReason",
                table: "ContentItemAssociations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssociationConfidenceScore",
                table: "ContentItemAssociations",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ContentItemAssociation_AssociationConfidenceScoreRange",
                table: "ContentItemAssociations",
                sql: "(AssociationConfidenceScore IS NULL OR AssociationConfidenceScore BETWEEN 0 AND 10)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ContentItemAssociation_AssociationConfidenceScoreRange",
                table: "ContentItemAssociations");

            migrationBuilder.DropColumn(
                name: "AssociationConfidenceReason",
                table: "ContentItemAssociations");

            migrationBuilder.DropColumn(
                name: "AssociationConfidenceScore",
                table: "ContentItemAssociations");
        }
    }
}
