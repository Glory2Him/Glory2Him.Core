using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class RenameContentItemAssociationToAssociation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No data exists in any environment (design decision D5), but this is a pure
            // rename with no shape change, so use RENAME operations rather than drop and
            // recreate — the table, its columns and its constraints are unchanged.
            migrationBuilder.DropCheckConstraint(
                name: "CK_ContentItemAssociation_ScopeConsistency",
                table: "ContentItemAssociations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ContentItemAssociation_AssociationConfidenceScoreRange",
                table: "ContentItemAssociations");

            migrationBuilder.RenameTable(
                name: "ContentItemAssociations",
                newName: "Associations");

            migrationBuilder.RenameIndex(
                table: "Associations",
                name: "IX_ContentItemAssociation_Target",
                newName: "IX_Association_Target");

            migrationBuilder.RenameIndex(
                table: "Associations",
                name: "IX_ContentItemAssociation_ByAssociatedContentItemGroupId_ScopeAll",
                newName: "IX_Association_ByAssociatedContentItemGroupId_ScopeAll");

            migrationBuilder.RenameIndex(
                table: "Associations",
                name: "IX_ContentItemAssociation_ByItem_ScopeThis",
                newName: "IX_Association_ByItem_ScopeThis");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Association_ScopeConsistency",
                table: "Associations",
                sql: "((LinkedContentScope = N'AllVersions' AND LinkedContentItemGroupId IS NOT NULL AND LinkedContentItemId IS NULL) OR (LinkedContentScope = N'ThisVersionOnly' AND LinkedContentItemId IS NOT NULL AND LinkedContentItemGroupId IS NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Association_AssociationConfidenceScoreRange",
                table: "Associations",
                sql: "(AssociationConfidenceScore IS NULL OR AssociationConfidenceScore BETWEEN 0 AND 10)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Association_ScopeConsistency",
                table: "Associations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Association_AssociationConfidenceScoreRange",
                table: "Associations");

            migrationBuilder.RenameIndex(
                table: "Associations",
                name: "IX_Association_Target",
                newName: "IX_ContentItemAssociation_Target");

            migrationBuilder.RenameIndex(
                table: "Associations",
                name: "IX_Association_ByAssociatedContentItemGroupId_ScopeAll",
                newName: "IX_ContentItemAssociation_ByAssociatedContentItemGroupId_ScopeAll");

            migrationBuilder.RenameIndex(
                table: "Associations",
                name: "IX_Association_ByItem_ScopeThis",
                newName: "IX_ContentItemAssociation_ByItem_ScopeThis");

            migrationBuilder.RenameTable(
                name: "Associations",
                newName: "ContentItemAssociations");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ContentItemAssociation_ScopeConsistency",
                table: "ContentItemAssociations",
                sql: "((LinkedContentScope = N'AllVersions' AND LinkedContentItemGroupId IS NOT NULL AND LinkedContentItemId IS NULL) OR (LinkedContentScope = N'ThisVersionOnly' AND LinkedContentItemId IS NOT NULL AND LinkedContentItemGroupId IS NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ContentItemAssociation_AssociationConfidenceScoreRange",
                table: "ContentItemAssociations",
                sql: "(AssociationConfidenceScore IS NULL OR AssociationConfidenceScore BETWEEN 0 AND 10)");
        }
    }
}
