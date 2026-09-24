using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class SplitAssociationPairIndexIntoEditorialAndPersonal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Associations_Pair",
                table: "Associations");

            migrationBuilder.CreateIndex(
                name: "UX_Associations_EditorialPair",
                table: "Associations",
                columns: new[] { "EntityAType", "EntityAEffectiveId", "EntityBType", "EntityBEffectiveId" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [UserId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Associations_PersonalPair",
                table: "Associations",
                columns: new[] { "EntityAType", "EntityAEffectiveId", "EntityBType", "UserId" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [UserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Associations_EditorialPair",
                table: "Associations");

            migrationBuilder.DropIndex(
                name: "UX_Associations_PersonalPair",
                table: "Associations");

            migrationBuilder.CreateIndex(
                name: "UX_Associations_Pair",
                table: "Associations",
                columns: new[] { "EntityAType", "EntityAEffectiveId", "EntityBType", "EntityBEffectiveId", "UserId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
