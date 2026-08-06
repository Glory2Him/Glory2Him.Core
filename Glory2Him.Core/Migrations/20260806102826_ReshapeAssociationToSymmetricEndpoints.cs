using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class ReshapeAssociationToSymmetricEndpoints : Migration
    {
        // Drop and recreate rather than map columns across. Every old column either changes
        // meaning or has no counterpart: the old shape hard-wired one endpoint to a
        // ContentItem and left the other version-blind, so there is no answer to "which side
        // does LinkedEntityId become" that is true for existing rows — canonical ordering
        // (design §4.4) decides that per row, from values the old shape never stored.
        // Scaffolding guessed `LinkedContentItemId` -> `SourceBatchId` and
        // `AssociationConfidenceScore` -> `SortOrder` purely on matching CLR types, which
        // would have carried an entity id into a provenance column and a confidence score
        // into a list position. There is no data in any environment, so recreating the table
        // is both honest and free.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Associations");

            migrationBuilder.CreateTable(
                name: "Associations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityAType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EntityAKeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityAGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityAScope = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EntityAEffectiveId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false,
                        computedColumnSql:
                            "CASE WHEN [EntityAScope] = N'AllVersions' " +
                            "THEN [EntityAGroupId] ELSE [EntityAKeyId] END",
                        stored: true),
                    EntityAContentType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    EntityBType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EntityBKeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityBGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityBScope = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EntityBEffectiveId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false,
                        computedColumnSql:
                            "CASE WHEN [EntityBScope] = N'AllVersions' " +
                            "THEN [EntityBGroupId] ELSE [EntityBKeyId] END",
                        stored: true),
                    EntityBContentType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: true),
                    ConfidenceScore = table.Column<decimal>(
                        type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    ConfidenceReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModelVersion = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PublishDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DeletedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Associations", x => x.Id);

                    table.CheckConstraint(
                        "CK_Association_ConfidenceScoreRange",
                        "(ConfidenceScore IS NULL OR ConfidenceScore BETWEEN 0 AND 10)");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Associations_EndpointA",
                table: "Associations",
                columns: new[] { "EntityAType", "EntityAEffectiveId" });

            migrationBuilder.CreateIndex(
                name: "IX_Associations_EndpointB",
                table: "Associations",
                columns: new[] { "EntityBType", "EntityBEffectiveId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Associations");

            migrationBuilder.CreateTable(
                name: "Associations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkedContentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LinkedContentItemGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LinkedContentScope = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LinkedEntityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LinkedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssociationConfidenceScore = table.Column<int>(type: "int", nullable: true),
                    AssociationConfidenceReason = table.Column<string>(
                        type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PublishDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DeletedWhen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Associations", x => x.Id);

                    table.CheckConstraint(
                        "CK_Association_AssociationConfidenceScoreRange",
                        "(AssociationConfidenceScore IS NULL OR AssociationConfidenceScore BETWEEN 0 AND 10)");

                    table.CheckConstraint(
                        "CK_Association_ScopeConsistency",
                        "((LinkedContentScope = N'AllVersions' AND LinkedContentItemGroupId IS NOT NULL " +
                        "AND LinkedContentItemId IS NULL) OR (LinkedContentScope = N'ThisVersionOnly' " +
                        "AND LinkedContentItemId IS NOT NULL AND LinkedContentItemGroupId IS NULL))");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Association_ByAssociatedContentItemGroupId_ScopeAll",
                table: "Associations",
                columns: new[] { "LinkedContentScope", "LinkedContentItemGroupId" },
                filter: "[LinkedContentScope] = N'AllVersions'");

            migrationBuilder.CreateIndex(
                name: "IX_Association_ByItem_ScopeThis",
                table: "Associations",
                columns: new[] { "LinkedContentScope", "LinkedContentItemId" },
                filter: "[LinkedContentScope] = N'ThisVersionOnly'");

            migrationBuilder.CreateIndex(
                name: "IX_Association_Target",
                table: "Associations",
                columns: new[] { "LinkedEntityType", "LinkedEntityId" });
        }
    }
}
