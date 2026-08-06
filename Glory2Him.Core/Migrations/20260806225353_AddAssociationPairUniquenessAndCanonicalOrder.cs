using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddAssociationPairUniquenessAndCanonicalOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_Associations_Pair",
                table: "Associations",
                columns: new[] { "EntityAType", "EntityAEffectiveId", "EntityBType", "EntityBEffectiveId", "UserId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Association_CanonicalOrder",
                table: "Associations",
                sql: "[EntityAType] COLLATE Latin1_General_BIN2 < [EntityBType] COLLATE Latin1_General_BIN2 OR ([EntityAType] COLLATE Latin1_General_BIN2 = [EntityBType] COLLATE Latin1_General_BIN2 AND [EntityAGroupId] < [EntityBGroupId])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Association_NotSameGroup",
                table: "Associations",
                sql: "[EntityAGroupId] <> [EntityBGroupId]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Associations_Pair",
                table: "Associations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Association_CanonicalOrder",
                table: "Associations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Association_NotSameGroup",
                table: "Associations");
        }
    }
}
