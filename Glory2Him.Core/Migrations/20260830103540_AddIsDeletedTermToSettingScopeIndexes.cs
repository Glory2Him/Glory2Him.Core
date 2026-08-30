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
    /// fix. No <i>resolution</i> changes answer either: §8.4 skips soft-deleted rows at every
    /// tier, §14.5 hides them from every caller, and the effective-setting resolution of §12.5.2
    /// is not built yet. The one place that observes them at all is
    /// <c>ContentItemSettingSeedData</c>'s idempotence check, which asks whether a row exists for
    /// a content type without excluding deleted ones — so a default an administrator removes is
    /// not restored on the next startup. That is left as it stands HERE, and fixed in #387: a
    /// content type must always have a default, so the rule being settled is that the default
    /// tier refuses deletion outright and the seed restores a missing one. The seed's term
    /// depends on this migration and could not have landed before it — until the filter carried
    /// <c>IsDeleted</c>, a re-seed insert would have violated the unique index a soft-deleted
    /// default was still occupying.</para>
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
