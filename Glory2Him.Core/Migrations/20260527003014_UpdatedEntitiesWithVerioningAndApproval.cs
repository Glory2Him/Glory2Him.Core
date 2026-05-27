using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedEntitiesWithVerioningAndApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContentItem_IsLatest",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "IX_ContentItemAssociation_ByContentItemGroupId_ScopeAll",
                table: "ContentItemAssociations");

            migrationBuilder.DropIndex(
                name: "IX_ContentItemAssociation_ByItem_ScopeThis",
                table: "ContentItemAssociations");

            migrationBuilder.DropIndex(
                name: "IX_ContentItemAssociation_Target",
                table: "ContentItemAssociations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ContentItemAssociation_ScopeConsistency",
                table: "ContentItemAssociations");

            migrationBuilder.DropColumn(
                name: "IsLatest",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "ApprovalId",
                table: "ContentItemAssociations");

            migrationBuilder.RenameColumn(
                name: "Scope",
                table: "ContentItemAssociations",
                newName: "LinkedEntityType");

            migrationBuilder.RenameColumn(
                name: "EntityType",
                table: "ContentItemAssociations",
                newName: "LinkedContentScope");

            migrationBuilder.RenameColumn(
                name: "EntityId",
                table: "ContentItemAssociations",
                newName: "LinkedEntityId");

            migrationBuilder.RenameColumn(
                name: "ContentItemId",
                table: "ContentItemAssociations",
                newName: "LinkedContentItemId");

            migrationBuilder.RenameColumn(
                name: "ContentItemGroupId",
                table: "ContentItemAssociations",
                newName: "LinkedContentItemGroupId");

            migrationBuilder.RenameColumn(
                name: "StatusId",
                table: "Approvals",
                newName: "ApprovalStatus");

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "Tags",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Tags",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedWhen",
                table: "Tags",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                table: "Tags",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Tags",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Tags",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishDate",
                table: "Tags",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Reactions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "UnicodeEmoji",
                table: "Reactions",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Reactions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Reactions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "Reactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Reactions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedWhen",
                table: "Reactions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                table: "Reactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Reactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Reactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishDate",
                table: "Reactions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "ContentTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ContentTypes",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedWhen",
                table: "ContentTypes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                table: "ContentTypes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ContentTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "ContentTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishDate",
                table: "ContentTypes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ContentTypeId",
                table: "ContentItemSettings",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ContentItemSettings",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedWhen",
                table: "ContentItemSettings",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                table: "ContentItemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ContentItemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LimitReactionsToLoveOnly",
                table: "ContentItemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "ContentItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ContentItems",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedWhen",
                table: "ContentItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                table: "ContentItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ContentItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLatestVersion",
                table: "ContentItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "ContentItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishDate",
                table: "ContentItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "ContentItemAssociations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ContentItemAssociations",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedWhen",
                table: "ContentItemAssociations",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                table: "ContentItemAssociations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ContentItemAssociations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "ContentItemAssociations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishDate",
                table: "ContentItemAssociations",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Approvals",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedWhen",
                table: "Approvals",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                table: "Approvals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Approvals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ApprovalReviews",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedWhen",
                table: "ApprovalReviews",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                table: "ApprovalReviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ApprovalReviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ApprovalComments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedWhen",
                table: "ApprovalComments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                table: "ApprovalComments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ApprovalComments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ApprovalSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RequiredApprovals = table.Column<int>(type: "int", nullable: false),
                    AllowSelfApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    BlockOnReject = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RequireReapprovalOnChange = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AutoApproveIfThresholdMet = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MustBeInRoleToApprove = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("PK_ApprovalSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BlobUri = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DeletedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentItemGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsLatestVersion = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PublishDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BibleReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Translation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Scripture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DeletedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentItemGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsLatestVersion = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PublishDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BibleReferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DeletedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PublishDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    LinkType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DeletedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentItemGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsLatestVersion = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PublishDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Links", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalSettingRoles",
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
                    table.PrimaryKey("PK_ApprovalSettingRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalSettingRoles_ApprovalSettings_ApprovalSettingId",
                        column: x => x.ApprovalSettingId,
                        principalTable: "ApprovalSettings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_Name",
                table: "Reactions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentItem_IsLatest",
                table: "ContentItems",
                columns: new[] { "ContentItemGroupId", "IsLatestVersion" },
                unique: true,
                filter: "[IsLatestVersion] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItem_IsPublished",
                table: "ContentItems",
                columns: new[] { "ContentItemGroupId", "IsPublished" },
                unique: true,
                filter: "[IsPublished] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_DeletedWhen",
                table: "ContentItems",
                column: "DeletedWhen");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_Feed",
                table: "ContentItems",
                columns: new[] { "ApprovalStatus", "IsPublished", "PublishDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_PublishDate",
                table: "ContentItems",
                column: "PublishDate");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItemAssociation_ByAssociatedContentItemGroupId_ScopeAll",
                table: "ContentItemAssociations",
                columns: new[] { "LinkedContentScope", "LinkedContentItemGroupId" },
                filter: "[LinkedContentScope] = N'AllVersions'");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItemAssociation_ByItem_ScopeThis",
                table: "ContentItemAssociations",
                columns: new[] { "LinkedContentScope", "LinkedContentItemId" },
                filter: "[LinkedContentScope] = N'ThisVersionOnly'");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItemAssociation_Target",
                table: "ContentItemAssociations",
                columns: new[] { "LinkedEntityType", "LinkedEntityId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ContentItemAssociation_ScopeConsistency",
                table: "ContentItemAssociations",
                sql: "((LinkedContentScope = N'AllVersions' AND LinkedContentItemGroupId IS NOT NULL AND LinkedContentItemId IS NULL) OR (LinkedContentScope = N'ThisVersionOnly' AND LinkedContentItemId IS NOT NULL AND LinkedContentItemGroupId IS NULL))");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSettingRoles_ApprovalSettingId",
                table: "ApprovalSettingRoles",
                column: "ApprovalSettingId");

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalSettingRoles_ApprovalSettingId_RoleName",
                table: "ApprovalSettingRoles",
                columns: new[] { "ApprovalSettingId", "RoleName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalSettings_EntityType",
                table: "ApprovalSettings",
                column: "EntityType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_Hash",
                table: "Attachments",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "UX_Attachments_ContentItemGroupId_IsLatest",
                table: "Attachments",
                columns: new[] { "ContentItemGroupId", "IsLatestVersion" },
                unique: true,
                filter: "[IsLatestVersion] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_Attachments_ContentItemGroupId_IsPublished",
                table: "Attachments",
                columns: new[] { "ContentItemGroupId", "IsPublished" },
                unique: true,
                filter: "[IsPublished] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_Attachments_ContentItemGroupId_Version",
                table: "Attachments",
                columns: new[] { "ContentItemGroupId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_BibleReferences_ContentItemGroupId_IsLatest",
                table: "BibleReferences",
                columns: new[] { "ContentItemGroupId", "IsLatestVersion" },
                unique: true,
                filter: "[IsLatestVersion] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_BibleReferences_ContentItemGroupId_IsPublished",
                table: "BibleReferences",
                columns: new[] { "ContentItemGroupId", "IsPublished" },
                unique: true,
                filter: "[IsPublished] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_BibleReferences_ContentItemGroupId_Version",
                table: "BibleReferences",
                columns: new[] { "ContentItemGroupId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Links_ContentItemGroupId_IsLatest",
                table: "Links",
                columns: new[] { "ContentItemGroupId", "IsLatestVersion" },
                unique: true,
                filter: "[IsLatestVersion] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_Links_ContentItemGroupId_IsPublished",
                table: "Links",
                columns: new[] { "ContentItemGroupId", "IsPublished" },
                unique: true,
                filter: "[IsPublished] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_Links_ContentItemGroupId_Version",
                table: "Links",
                columns: new[] { "ContentItemGroupId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalSettingRoles");

            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "BibleReferences");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "Links");

            migrationBuilder.DropTable(
                name: "ApprovalSettings");

            migrationBuilder.DropIndex(
                name: "IX_Reactions_Name",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_ContentItem_IsLatest",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "IX_ContentItem_IsPublished",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "IX_ContentItems_DeletedWhen",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "IX_ContentItems_Feed",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "IX_ContentItems_PublishDate",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "IX_ContentItemAssociation_ByAssociatedContentItemGroupId_ScopeAll",
                table: "ContentItemAssociations");

            migrationBuilder.DropIndex(
                name: "IX_ContentItemAssociation_ByItem_ScopeThis",
                table: "ContentItemAssociations");

            migrationBuilder.DropIndex(
                name: "IX_ContentItemAssociation_Target",
                table: "ContentItemAssociations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ContentItemAssociation_ScopeConsistency",
                table: "ContentItemAssociations");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "DeletedWhen",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "PublishDate",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Reactions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Reactions");

            migrationBuilder.DropColumn(
                name: "DeletedWhen",
                table: "Reactions");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                table: "Reactions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Reactions");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Reactions");

            migrationBuilder.DropColumn(
                name: "PublishDate",
                table: "Reactions");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "ContentTypes");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ContentTypes");

            migrationBuilder.DropColumn(
                name: "DeletedWhen",
                table: "ContentTypes");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                table: "ContentTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ContentTypes");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "ContentTypes");

            migrationBuilder.DropColumn(
                name: "PublishDate",
                table: "ContentTypes");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "DeletedWhen",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "LimitReactionsToLoveOnly",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "DeletedWhen",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "IsLatestVersion",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "PublishDate",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "ContentItemAssociations");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ContentItemAssociations");

            migrationBuilder.DropColumn(
                name: "DeletedWhen",
                table: "ContentItemAssociations");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                table: "ContentItemAssociations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ContentItemAssociations");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "ContentItemAssociations");

            migrationBuilder.DropColumn(
                name: "PublishDate",
                table: "ContentItemAssociations");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Approvals");

            migrationBuilder.DropColumn(
                name: "DeletedWhen",
                table: "Approvals");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                table: "Approvals");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Approvals");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ApprovalReviews");

            migrationBuilder.DropColumn(
                name: "DeletedWhen",
                table: "ApprovalReviews");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                table: "ApprovalReviews");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ApprovalReviews");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ApprovalComments");

            migrationBuilder.DropColumn(
                name: "DeletedWhen",
                table: "ApprovalComments");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                table: "ApprovalComments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ApprovalComments");

            migrationBuilder.RenameColumn(
                name: "LinkedEntityType",
                table: "ContentItemAssociations",
                newName: "Scope");

            migrationBuilder.RenameColumn(
                name: "LinkedEntityId",
                table: "ContentItemAssociations",
                newName: "EntityId");

            migrationBuilder.RenameColumn(
                name: "LinkedContentScope",
                table: "ContentItemAssociations",
                newName: "EntityType");

            migrationBuilder.RenameColumn(
                name: "LinkedContentItemId",
                table: "ContentItemAssociations",
                newName: "ContentItemId");

            migrationBuilder.RenameColumn(
                name: "LinkedContentItemGroupId",
                table: "ContentItemAssociations",
                newName: "ContentItemGroupId");

            migrationBuilder.RenameColumn(
                name: "ApprovalStatus",
                table: "Approvals",
                newName: "StatusId");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Reactions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "UnicodeEmoji",
                table: "Reactions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(16)",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Reactions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Reactions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "ContentTypeId",
                table: "ContentItemSettings",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<bool>(
                name: "IsLatest",
                table: "ContentItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovalId",
                table: "ContentItemAssociations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ContentItem_IsLatest",
                table: "ContentItems",
                columns: new[] { "ContentItemGroupId", "IsLatest" },
                unique: true,
                filter: "[IsLatest] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItemAssociation_ByContentItemGroupId_ScopeAll",
                table: "ContentItemAssociations",
                columns: new[] { "Scope", "ContentItemGroupId" },
                filter: "[Scope] = N'AllVersions'");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItemAssociation_ByItem_ScopeThis",
                table: "ContentItemAssociations",
                columns: new[] { "Scope", "ContentItemId" },
                filter: "[Scope] = N'ThisVersionOnly'");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItemAssociation_Target",
                table: "ContentItemAssociations",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ContentItemAssociation_ScopeConsistency",
                table: "ContentItemAssociations",
                sql: "((Scope = N'AllVersions' AND ContentItemGroupId IS NOT NULL AND ContentItemId IS NULL) OR (Scope = N'ThisVersionOnly' AND ContentItemId IS NOT NULL AND ContentItemGroupId IS NULL))");
        }
    }
}
