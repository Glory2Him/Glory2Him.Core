using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddContentItemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContentItemSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TagsAllowed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TagAssociationsRequireApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ShowTags = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ReactionsAllowed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReactionAssociationsRequireApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ShowReactions = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LinksAllowed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LinkAssociationsRequireApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ShowLinks = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AttachmentsAllowed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AttachmentAssociationsRequireApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ShowAttachments = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CommentsAllowed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CommentAssociationsRequireApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ShowComments = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    BibleReferenceAllowed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    BibleReferenceAssociationsRequireApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ShowBibleReferences = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentItemSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_ContentItemSettings_DefaultPerType",
                table: "ContentItemSettings",
                column: "ContentTypeId",
                unique: true,
                filter: "[ContentItemId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ContentItemSettings_OverridePerEntity",
                table: "ContentItemSettings",
                column: "ContentItemId",
                unique: true,
                filter: "[ContentItemId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentItemSettings");
        }
    }
}
