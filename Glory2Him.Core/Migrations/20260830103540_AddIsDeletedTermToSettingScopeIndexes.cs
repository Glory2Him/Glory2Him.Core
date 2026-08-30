using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <summary>
    /// Adds an <c>IsDeleted</c> term to all four setting-scope unique indexes, so a soft-deleted
    /// row releases the scope it occupies instead of holding it forever (#326).
    ///
    /// <para><b>Up is safe against existing data.</b> Widening a filter only ever removes rows
    /// from a unique index, so no scope can be found in violation: under the old filters a scope
    /// could hold at most one row, deleted or not, and a live row therefore never shared its
    /// scope with a soft-deleted one. Rows already trapped simply stop blocking, which is the
    /// fix. Nothing reads a soft-deleted setting — §8.4 resolution skips them at every tier and
    /// §14.5 hides them from every caller — so no resolution changes answer.</para>
    ///
    /// <para><b>Down can fail, and deliberately is not defended.</b> Once the fix is live a scope
    /// may legitimately hold one live row alongside soft-deleted predecessors, and restoring the
    /// narrower filters would then violate uniqueness. Reverting means deciding which of those
    /// rows to remove first — a data decision, not a schema one.</para>
    /// </summary>
    public partial class AddIsDeletedTermToSettingScopeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ContentItemSettings_DefaultPerType",
                table: "ContentItemSettings");

            migrationBuilder.DropIndex(
                name: "UX_ContentItemSettings_OverridePerEntity",
                table: "ContentItemSettings");

            migrationBuilder.DropIndex(
                name: "UX_ApprovalSettings_EntityTypeContentType",
                table: "ApprovalSettings");

            migrationBuilder.DropIndex(
                name: "UX_ApprovalSettings_EntityTypeDefault",
                table: "ApprovalSettings");

            migrationBuilder.CreateIndex(
                name: "UX_ContentItemSettings_DefaultPerType",
                table: "ContentItemSettings",
                column: "ContentType",
                unique: true,
                filter: "[ContentItemId] IS NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_ContentItemSettings_OverridePerEntity",
                table: "ContentItemSettings",
                column: "ContentItemId",
                unique: true,
                filter: "[ContentItemId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalSettings_EntityTypeContentType",
                table: "ApprovalSettings",
                columns: new[] { "EntityType", "ContentType" },
                unique: true,
                filter: "[ContentType] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalSettings_EntityTypeDefault",
                table: "ApprovalSettings",
                column: "EntityType",
                unique: true,
                filter: "[ContentType] IS NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ContentItemSettings_DefaultPerType",
                table: "ContentItemSettings");

            migrationBuilder.DropIndex(
                name: "UX_ContentItemSettings_OverridePerEntity",
                table: "ContentItemSettings");

            migrationBuilder.DropIndex(
                name: "UX_ApprovalSettings_EntityTypeContentType",
                table: "ApprovalSettings");

            migrationBuilder.DropIndex(
                name: "UX_ApprovalSettings_EntityTypeDefault",
                table: "ApprovalSettings");

            migrationBuilder.CreateIndex(
                name: "UX_ContentItemSettings_DefaultPerType",
                table: "ContentItemSettings",
                column: "ContentType",
                unique: true,
                filter: "[ContentItemId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ContentItemSettings_OverridePerEntity",
                table: "ContentItemSettings",
                column: "ContentItemId",
                unique: true,
                filter: "[ContentItemId] IS NOT NULL");

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
        }
    }
}
