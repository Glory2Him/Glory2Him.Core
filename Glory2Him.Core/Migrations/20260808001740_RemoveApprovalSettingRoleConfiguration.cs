using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class RemoveApprovalSettingRoleConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalSettingPublisherRoles");

            migrationBuilder.DropTable(
                name: "ApprovalSettingReviewerRoles");

            migrationBuilder.DropColumn(
                name: "RestrictWhoCanApprove",
                table: "ApprovalSettings");

            migrationBuilder.DropColumn(
                name: "RestrictWhoCanReview",
                table: "ApprovalSettings");

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByBypassReason",
                table: "Approvals",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedByBypassReason",
                table: "Approvals");

            migrationBuilder.AddColumn<bool>(
                name: "RestrictWhoCanApprove",
                table: "ApprovalSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RestrictWhoCanReview",
                table: "ApprovalSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ApprovalSettingPublisherRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalSettingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DeletedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RoleName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalSettingPublisherRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalSettingPublisherRoles_ApprovalSettings_ApprovalSettingId",
                        column: x => x.ApprovalSettingId,
                        principalTable: "ApprovalSettings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalSettingReviewerRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalSettingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DeletedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RoleName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalSettingReviewerRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalSettingReviewerRoles_ApprovalSettings_ApprovalSettingId",
                        column: x => x.ApprovalSettingId,
                        principalTable: "ApprovalSettings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSettingPublisherRoles_ApprovalSettingId",
                table: "ApprovalSettingPublisherRoles",
                column: "ApprovalSettingId");

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalSettingPublisherRoles_ApprovalSettingId_RoleName",
                table: "ApprovalSettingPublisherRoles",
                columns: new[] { "ApprovalSettingId", "RoleName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSettingReviewerRoles_ApprovalSettingId",
                table: "ApprovalSettingReviewerRoles",
                column: "ApprovalSettingId");

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalSettingReviewerRoles_ApprovalSettingId_RoleName",
                table: "ApprovalSettingReviewerRoles",
                columns: new[] { "ApprovalSettingId", "RoleName" },
                unique: true);
        }
    }
}
