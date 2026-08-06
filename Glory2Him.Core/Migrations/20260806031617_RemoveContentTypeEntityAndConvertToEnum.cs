using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class RemoveContentTypeEntityAndConvertToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContentItems_ContentTypes_ContentTypeId",
                table: "ContentItems");

            migrationBuilder.DropTable(
                name: "ContentTypes");

            migrationBuilder.DropIndex(
                name: "UX_ContentItemSettings_DefaultPerType",
                table: "ContentItemSettings");

            migrationBuilder.DropIndex(
                name: "IX_ContentItems_ContentTypeId",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "IX_ContentItems_ContentTypeId_ContentHash",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "UX_ApprovalSettings_EntityType",
                table: "ApprovalSettings");

            migrationBuilder.DropColumn(
                name: "AttachmentAssociationsRequireApproval",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "BibleReferenceAssociationsRequireApproval",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "CommentAssociationsRequireApproval",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "ContentTypeId",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "LinkAssociationsRequireApproval",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "ReactionAssociationsRequireApproval",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "TagAssociationsRequireApproval",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "ContentTypeId",
                table: "ContentItems");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "ContentItemSettings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "ContentItems",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "ApprovalSettings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_ContentItemSettings_DefaultPerType",
                table: "ContentItemSettings",
                column: "ContentType",
                unique: true,
                filter: "[ContentItemId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_ContentType",
                table: "ContentItems",
                column: "ContentType");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_ContentTypeId_ContentHash",
                table: "ContentItems",
                columns: new[] { "ContentType", "ContentHash" });

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalSettings_EntityTypeContentType",
                table: "ApprovalSettings",
                columns: new[] { "EntityType", "ContentType" },
                unique: true,
                filter: "[ContentType] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalSettings_EntityTypeDefault",
                table: "ApprovalSettings",
                column: "EntityType",
                unique: true,
                filter: "[ContentType] IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApprovalSetting_ContentTypeRequiresContentItem",
                table: "ApprovalSettings",
                sql: "(ContentType IS NULL OR EntityType = N'ContentItem')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ContentItemSettings_DefaultPerType",
                table: "ContentItemSettings");

            migrationBuilder.DropIndex(
                name: "IX_ContentItems_ContentType",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "IX_ContentItems_ContentTypeId_ContentHash",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "UX_ApprovalSettings_EntityTypeContentType",
                table: "ApprovalSettings");

            migrationBuilder.DropIndex(
                name: "UX_ApprovalSettings_EntityTypeDefault",
                table: "ApprovalSettings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApprovalSetting_ContentTypeRequiresContentItem",
                table: "ApprovalSettings");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "ContentItemSettings");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "ApprovalSettings");

            migrationBuilder.AddColumn<bool>(
                name: "AttachmentAssociationsRequireApproval",
                table: "ContentItemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "BibleReferenceAssociationsRequireApproval",
                table: "ContentItemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CommentAssociationsRequireApproval",
                table: "ContentItemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContentTypeId",
                table: "ContentItemSettings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "LinkAssociationsRequireApproval",
                table: "ContentItemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReactionAssociationsRequireApproval",
                table: "ContentItemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "TagAssociationsRequireApproval",
                table: "ContentItemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContentTypeId",
                table: "ContentItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ContentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DeletedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PublishDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_ContentItemSettings_DefaultPerType",
                table: "ContentItemSettings",
                column: "ContentTypeId",
                unique: true,
                filter: "[ContentItemId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_ContentTypeId",
                table: "ContentItems",
                column: "ContentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_ContentTypeId_ContentHash",
                table: "ContentItems",
                columns: new[] { "ContentTypeId", "ContentHash" });

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalSettings_EntityType",
                table: "ApprovalSettings",
                column: "EntityType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_Name",
                table: "ContentTypes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentItems_ContentTypes_ContentTypeId",
                table: "ContentItems",
                column: "ContentTypeId",
                principalTable: "ContentTypes",
                principalColumn: "Id");
        }
    }
}
