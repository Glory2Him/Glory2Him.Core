using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class SplitApprovalSettingRolesAddBypassAndContentHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalSettingRoles");

            migrationBuilder.DropIndex(
                name: "UX_Links_ContentItemGroupId_G2Hatest",
                table: "Links");

            migrationBuilder.DropIndex(
                name: "IX_ContentItem_G2Hatest",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "UX_BibleReferences_ContentItemGroupId_G2Hatest",
                table: "BibleReferences");

            migrationBuilder.DropIndex(
                name: "UX_Attachments_ContentItemGroupId_G2Hatest",
                table: "Attachments");

            migrationBuilder.RenameColumn(
                name: "G2HatestVersion",
                table: "Links",
                newName: "IsLatestVersion");

            migrationBuilder.RenameColumn(
                name: "G2HatestVersion",
                table: "ContentItems",
                newName: "IsLatestVersion");

            migrationBuilder.RenameColumn(
                name: "G2HatestVersion",
                table: "BibleReferences",
                newName: "IsLatestVersion");

            migrationBuilder.RenameColumn(
                name: "G2HatestVersion",
                table: "Attachments",
                newName: "IsLatestVersion");

            migrationBuilder.RenameColumn(
                name: "RequiredApprovals",
                table: "ApprovalSettings",
                newName: "RequiredNumberOfApprovals");

            migrationBuilder.RenameColumn(
                name: "MustBeInRoleToApprove",
                table: "ApprovalSettings",
                newName: "RestrictWhoCanReview");

            migrationBuilder.RenameColumn(
                name: "AutoApproveIfThresholdMet",
                table: "ApprovalSettings",
                newName: "AutoApproveIfAllApprovalRequirementsMet");

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "ContentItems",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RestrictWhoCanApprove",
                table: "ApprovalSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DoNotAllowBypassingSettings",
                table: "ApprovalSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireApprovalCommentResolutionBeforeApproval",
                table: "ApprovalSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequireApprovals",
                table: "ApprovalSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApprovedByBypass",
                table: "Approvals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ApprovalSettingPublisherRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalSettingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DeletedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    RoleName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DeletedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                name: "UX_Links_ContentItemGroupId_G2Hatest",
                table: "Links",
                columns: new[] { "ContentItemGroupId", "IsLatestVersion" },
                unique: true,
                filter: "[IsLatestVersion] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItem_G2Hatest",
                table: "ContentItems",
                columns: new[] { "ContentItemGroupId", "IsLatestVersion" },
                unique: true,
                filter: "[IsLatestVersion] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_ContentTypeId_ContentHash",
                table: "ContentItems",
                columns: new[] { "ContentTypeId", "ContentHash" });

            migrationBuilder.CreateIndex(
                name: "UX_BibleReferences_ContentItemGroupId_G2Hatest",
                table: "BibleReferences",
                columns: new[] { "ContentItemGroupId", "IsLatestVersion" },
                unique: true,
                filter: "[IsLatestVersion] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_Attachments_ContentItemGroupId_G2Hatest",
                table: "Attachments",
                columns: new[] { "ContentItemGroupId", "IsLatestVersion" },
                unique: true,
                filter: "[IsLatestVersion] = 1");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalSettingPublisherRoles");

            migrationBuilder.DropTable(
                name: "ApprovalSettingReviewerRoles");

            migrationBuilder.DropIndex(
                name: "UX_Links_ContentItemGroupId_G2Hatest",
                table: "Links");

            migrationBuilder.DropIndex(
                name: "IX_ContentItem_G2Hatest",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "IX_ContentItems_ContentTypeId_ContentHash",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "UX_BibleReferences_ContentItemGroupId_G2Hatest",
                table: "BibleReferences");

            migrationBuilder.DropIndex(
                name: "UX_Attachments_ContentItemGroupId_G2Hatest",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "RestrictWhoCanApprove",
                table: "ApprovalSettings");

            migrationBuilder.DropColumn(
                name: "DoNotAllowBypassingSettings",
                table: "ApprovalSettings");

            migrationBuilder.DropColumn(
                name: "RequireApprovalCommentResolutionBeforeApproval",
                table: "ApprovalSettings");

            migrationBuilder.DropColumn(
                name: "RequireApprovals",
                table: "ApprovalSettings");

            migrationBuilder.DropColumn(
                name: "IsApprovedByBypass",
                table: "Approvals");

            migrationBuilder.RenameColumn(
                name: "IsLatestVersion",
                table: "Links",
                newName: "G2HatestVersion");

            migrationBuilder.RenameColumn(
                name: "IsLatestVersion",
                table: "ContentItems",
                newName: "G2HatestVersion");

            migrationBuilder.RenameColumn(
                name: "IsLatestVersion",
                table: "BibleReferences",
                newName: "G2HatestVersion");

            migrationBuilder.RenameColumn(
                name: "IsLatestVersion",
                table: "Attachments",
                newName: "G2HatestVersion");

            migrationBuilder.RenameColumn(
                name: "RestrictWhoCanReview",
                table: "ApprovalSettings",
                newName: "MustBeInRoleToApprove");

            migrationBuilder.RenameColumn(
                name: "AutoApproveIfAllApprovalRequirementsMet",
                table: "ApprovalSettings",
                newName: "AutoApproveIfThresholdMet");

            migrationBuilder.RenameColumn(
                name: "RequiredNumberOfApprovals",
                table: "ApprovalSettings",
                newName: "RequiredApprovals");

            migrationBuilder.CreateTable(
                name: "ApprovalSettingRoles",
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
                    table.PrimaryKey("PK_ApprovalSettingRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalSettingRoles_ApprovalSettings_ApprovalSettingId",
                        column: x => x.ApprovalSettingId,
                        principalTable: "ApprovalSettings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "UX_Links_ContentItemGroupId_G2Hatest",
                table: "Links",
                columns: new[] { "ContentItemGroupId", "G2HatestVersion" },
                unique: true,
                filter: "[G2HatestVersion] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItem_G2Hatest",
                table: "ContentItems",
                columns: new[] { "ContentItemGroupId", "G2HatestVersion" },
                unique: true,
                filter: "[G2HatestVersion] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_BibleReferences_ContentItemGroupId_G2Hatest",
                table: "BibleReferences",
                columns: new[] { "ContentItemGroupId", "G2HatestVersion" },
                unique: true,
                filter: "[G2HatestVersion] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_Attachments_ContentItemGroupId_G2Hatest",
                table: "Attachments",
                columns: new[] { "ContentItemGroupId", "G2HatestVersion" },
                unique: true,
                filter: "[G2HatestVersion] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSettingRoles_ApprovalSettingId",
                table: "ApprovalSettingRoles",
                column: "ApprovalSettingId");

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalSettingRoles_ApprovalSettingId_RoleName",
                table: "ApprovalSettingRoles",
                columns: new[] { "ApprovalSettingId", "RoleName" },
                unique: true);
        }
    }
}
