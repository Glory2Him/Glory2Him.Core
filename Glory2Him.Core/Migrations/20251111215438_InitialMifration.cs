// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialMifration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Approvals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentItemAssociations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContentItemGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentItemAssociations", x => x.Id);
                    table.CheckConstraint("CK_ContentItemAssociation_ScopeConsistency", "((Scope = N'AllVersions' AND ContentItemGroupId IS NOT NULL AND ContentItemId IS NULL) OR (Scope = N'ThisVersionOnly' AND ContentItemId IS NOT NULL AND ContentItemGroupId IS NULL))");
                });

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
                    ShowBibleReferences = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentItemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalComments_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalReviews_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Author = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentItemGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    G2Hatest = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentItems_ContentTypes_ContentTypeId",
                        column: x => x.ContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalComments_ApprovalId",
                table: "ApprovalComments",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalReviews_ApprovalId",
                table: "ApprovalReviews",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalReviews_ApprovalId_StatusId",
                table: "ApprovalReviews",
                columns: new[] { "ApprovalId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalReviews_ApprovalId_ReviewerId",
                table: "ApprovalReviews",
                columns: new[] { "ApprovalId", "ReviewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_EntityType_StatusId",
                table: "Approvals",
                columns: new[] { "EntityType", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "UX_Approvals_EntityType_EntityId",
                table: "Approvals",
                columns: new[] { "EntityType", "EntityId" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_ContentItem_G2Hatest",
                table: "ContentItems",
                columns: new[] { "ContentItemGroupId", "G2Hatest" },
                unique: true,
                filter: "[G2Hatest] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_ContentItemGroupId_VersionDesc",
                table: "ContentItems",
                columns: new[] { "ContentItemGroupId", "Version" },
                unique: true,
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_ContentTypeId",
                table: "ContentItems",
                column: "ContentTypeId");

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

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_Name",
                table: "ContentTypes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalComments");

            migrationBuilder.DropTable(
                name: "ApprovalReviews");

            migrationBuilder.DropTable(
                name: "ContentItemAssociations");

            migrationBuilder.DropTable(
                name: "ContentItems");

            migrationBuilder.DropTable(
                name: "ContentItemSettings");

            migrationBuilder.DropTable(
                name: "Approvals");

            migrationBuilder.DropTable(
                name: "ContentTypes");
        }
    }
}
