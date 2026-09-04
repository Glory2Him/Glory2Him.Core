using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class MakeApprovalSettingScopeConstraintsNullSafe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ApprovalSetting_ContentTypeRequiresContentItem",
                table: "ApprovalSettings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApprovalSetting_IsPersonalRequiresAssociation",
                table: "ApprovalSettings");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApprovalSetting_ContentTypeRequiresContentItem",
                table: "ApprovalSettings",
                sql: "(ContentType IS NULL OR (EntityType IS NOT NULL AND EntityType = N'ContentItem'))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApprovalSetting_IsPersonalRequiresAssociation",
                table: "ApprovalSettings",
                sql: "(IsPersonal IS NULL OR (EntityType IS NOT NULL AND EntityType = N'Association'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ApprovalSetting_ContentTypeRequiresContentItem",
                table: "ApprovalSettings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApprovalSetting_IsPersonalRequiresAssociation",
                table: "ApprovalSettings");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApprovalSetting_ContentTypeRequiresContentItem",
                table: "ApprovalSettings",
                sql: "(ContentType IS NULL OR EntityType = N'ContentItem')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApprovalSetting_IsPersonalRequiresAssociation",
                table: "ApprovalSettings",
                sql: "(IsPersonal IS NULL OR EntityType = N'Association')");
        }
    }
}
