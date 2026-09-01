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

namespace Glory2Him.WebApp.Data.Migrations
{
    /// <summary>
    /// The security half of renaming <c>ContentType.Verses</c> to <c>ContentType.VerseImage</c>.
    /// <c>SeedData</c> composes the narrow §18.6 tier from the enum — <c>ContentItem-Verses-Reviewers</c>,
    /// <c>ContentItem-Verses-Publishers</c> — so the member's new name is a new set of role names,
    /// and the rows carrying the old ones are left granting nothing.
    ///
    /// <para>RENAMED IN PLACE, NOT RE-SEEDED, for the reason
    /// <c>PluraliseRoleNamesAndCollapseAdmin</c> gives at length: <c>AspNetUserRoles</c> keys on
    /// <c>RoleId</c>, so rewriting the row's <c>Name</c> carries every existing membership across.
    /// Letting the seed mint the new names instead would leave each holder on a row nothing checks
    /// any more — they would keep a role and silently lose the capability it granted, which is the
    /// failure this repository has already been bitten by once.</para>
    ///
    /// <para>MATCHED ON THE INFIX rather than listed. The capability suffix is not fixed —
    /// Reviewers and Publishers are what the seed mints today, and §18.6's tiers are still
    /// growing (#366, #367) — so rewriting <c>-Verses-</c> wherever it appears catches names this
    /// migration cannot know about, including any an administrator minted by hand. No other
    /// vocabulary in the role grammar contains the word, so the infix cannot collide.</para>
    ///
    /// <para>THE OTHER HALF IS IN CORE. The same member is persisted as a string in five columns
    /// of the CONTENT database (§10.2), renamed by the <c>StorageBroker</c> migration of this same
    /// name. The two run against different connection strings (§12.7.1) and neither implies the
    /// other — deploy one alone and either the reads throw or the narrow tier is held by nobody.</para>
    /// </summary>
    /// <inheritdoc />
    public partial class RenameVersesContentTypeToVerseImage : Migration
    {
        private const string OldSegment = "-Verses-";
        private const string NewSegment = "-VerseImage-";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) =>
            RenameRolesByInfix(migrationBuilder, from: OldSegment, to: NewSegment);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            RenameRolesByInfix(migrationBuilder, from: NewSegment, to: OldSegment);

        // The NOT EXISTS guard is what makes the rename re-runnable and collision-safe: Identity's
        // unique index on NormalizedName would fail the statement outright rather than skip the
        // row, so without the guard a second run — or a store where the new name already exists —
        // takes the whole migration down with it.
        private static void RenameRolesByInfix(
            MigrationBuilder migrationBuilder,
            string from,
            string to)
        {
            string normalizedFrom = from.ToUpperInvariant();
            string normalizedTo = to.ToUpperInvariant();

            migrationBuilder.Sql(
                $@"UPDATE role
                   SET [Name] = REPLACE(role.[Name], N'{from}', N'{to}'),
                       [NormalizedName] =
                           REPLACE(role.[NormalizedName], N'{normalizedFrom}', N'{normalizedTo}')
                   FROM [AspNetRoles] role
                   WHERE role.[NormalizedName] LIKE N'%{normalizedFrom}%'
                       AND NOT EXISTS (
                           SELECT 1
                           FROM [AspNetRoles] existing
                           WHERE existing.[NormalizedName] =
                               REPLACE(
                                   role.[NormalizedName],
                                   N'{normalizedFrom}',
                                   N'{normalizedTo}'));");
        }
    }
}
