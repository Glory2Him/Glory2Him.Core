using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalAndPersonalApprovalSettingTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ApprovalSettings_EntityTypeDefault",
                table: "ApprovalSettings");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "ApprovalSettings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<bool>(
                name: "IsPersonal",
                table: "ApprovalSettings",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalSettings_AssociationPersonality",
                table: "ApprovalSettings",
                columns: new[] { "EntityType", "IsPersonal" },
                unique: true,
                filter: "[IsPersonal] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalSettings_EntityTypeDefault",
                table: "ApprovalSettings",
                column: "EntityType",
                unique: true,
                filter: "[EntityType] IS NOT NULL AND [ContentType] IS NULL AND [IsPersonal] IS NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalSettings_GlobalDefault",
                table: "ApprovalSettings",
                column: "EntityType",
                unique: true,
                filter: "[EntityType] IS NULL AND [IsDeleted] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApprovalSetting_IsPersonalRequiresAssociation",
                table: "ApprovalSettings",
                sql: "(IsPersonal IS NULL OR EntityType = N'Association')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ApprovalSettings_AssociationPersonality",
                table: "ApprovalSettings");

            migrationBuilder.DropIndex(
                name: "UX_ApprovalSettings_EntityTypeDefault",
                table: "ApprovalSettings");

            migrationBuilder.DropIndex(
                name: "UX_ApprovalSettings_GlobalDefault",
                table: "ApprovalSettings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApprovalSetting_IsPersonalRequiresAssociation",
                table: "ApprovalSettings");

            migrationBuilder.DropColumn(
                name: "IsPersonal",
                table: "ApprovalSettings");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "ApprovalSettings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalSettings_EntityTypeDefault",
                table: "ApprovalSettings",
                column: "EntityType",
                unique: true,
                filter: "[ContentType] IS NULL AND [IsDeleted] = 0");
        }
    }
}
