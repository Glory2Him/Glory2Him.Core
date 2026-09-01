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
    /// <summary>
    /// <c>ContentType.Verses</c> became <c>ContentType.VerseImage</c>, and the enum is
    /// persisted as a STRING (§10.2, <c>HasConversion&lt;string&gt;()</c>) — so the member
    /// name is not only code, it is the value in five columns across four tables. EF materialises
    /// a stored string back into a member by NAME, so the rename and this update are one change:
    /// deploy the code without it and every row of that type throws on read.
    ///
    /// <para>THE VALUE (6) IS UNTOUCHED, which is what keeps §3.6's append-only promise intact.
    /// Nothing is renumbered, nothing is reused, and no row changes what it means — the member
    /// only says out loud what its own summary always said it was, "a verse image".</para>
    ///
    /// <para>THE ROLE NAMES ARE THE OTHER HALF and are NOT here. <c>ContentItem-Verses-*</c>
    /// (§18.6) lives in the SECURITY database, which is another context and another connection
    /// string entirely (§12.7.1), so it is renamed by the <c>SecurityDbContext</c> migration of
    /// the same name. Both must run; running only this one leaves a narrow tier nobody holds.</para>
    ///
    /// <para>Matched on the old value rather than blindly rewritten, so it is safe to run twice
    /// and cannot touch a row that already says <c>VerseImage</c>.</para>
    /// </summary>
    /// <inheritdoc />
    public partial class RenameVersesContentTypeToVerseImage : Migration
    {
        private const string OldContentType = "Verses";
        private const string NewContentType = "VerseImage";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) =>
            RewriteContentType(migrationBuilder, from: OldContentType, to: NewContentType);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            RewriteContentType(migrationBuilder, from: NewContentType, to: OldContentType);

        /// <summary>
        /// Every column the ContentType enum is persisted into. Associations carries it TWICE —
        /// once per endpoint (§4.1) — and both are denormalised copies of the same fact, so a
        /// rename that missed one would leave the two halves of an association disagreeing about
        /// what the item on the other end is.
        /// </summary>
        private static void RewriteContentType(
            MigrationBuilder migrationBuilder,
            string from,
            string to)
        {
            Rewrite(migrationBuilder, table: "ContentItems", column: "ContentType", from, to);
            Rewrite(migrationBuilder, table: "ContentItemSettings", column: "ContentType", from, to);
            Rewrite(migrationBuilder, table: "ApprovalSettings", column: "ContentType", from, to);
            Rewrite(migrationBuilder, table: "Associations", column: "EntityAContentType", from, to);
            Rewrite(migrationBuilder, table: "Associations", column: "EntityBContentType", from, to);
        }

        private static void Rewrite(
            MigrationBuilder migrationBuilder,
            string table,
            string column,
            string from,
            string to) =>
            migrationBuilder.Sql(
                $@"
                UPDATE [{table}]
                SET [{column}] = '{to}'
                WHERE [{column}] = '{from}';");
    }
}
