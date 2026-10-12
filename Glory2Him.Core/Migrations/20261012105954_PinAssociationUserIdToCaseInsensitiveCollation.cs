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

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class PinAssociationUserIdToCaseInsensitiveCollation : Migration
    {
        // UX_Associations_PersonalPair keys on UserId and UX_Associations_EditorialPair filters
        // on it, and SQL Server refuses ALTER COLUMN on a column an index depends on, so both
        // indexes are dropped and recreated around the alter. EF generates the ALTER alone,
        // which fails; the drops and the recreates below are deliberate hand-edits, each index
        // recreated with the key, uniqueness, filter and name §DOM4.6 rule 2 gives it.
        //
        // All of it is ONE batch and must stay that way. No column is added here, so nothing
        // below names something the batch itself creates, and no statement needs EXEC.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Associations_EditorialPair",
                table: "Associations");

            migrationBuilder.DropIndex(
                name: "UX_Associations_PersonalPair",
                table: "Associations");

            // A user id ignores letter case (§DOM4.6 rule 3). The collation is today's catalogue
            // default, so on those catalogues no comparison changes; what the pin removes is the
            // reliance on that default.
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Associations",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                collation: "SQL_Latin1_General_CP1_CI_AS",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            // Recreated over the altered column. On a catalogue whose default compares exactly,
            // two live personal rows holding one reader's id in two letter cases collide here,
            // and the migration fails rather than keep both.
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
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Associations",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true,
                oldCollation: "SQL_Latin1_General_CP1_CI_AS");
        }
    }
}
