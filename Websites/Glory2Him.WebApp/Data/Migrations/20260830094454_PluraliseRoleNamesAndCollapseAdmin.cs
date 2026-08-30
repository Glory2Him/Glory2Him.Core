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
    /// Issue #368 — the role vocabulary went plural and the two administrator roles became one.
    ///
    /// <para>RENAMED IN PLACE, NOT RE-SEEDED. <c>AspNetUserRoles</c> keys on <c>RoleId</c>, so
    /// rewriting a role row's <c>Name</c> and <c>NormalizedName</c> carries every existing
    /// membership across untouched. Leaving the seed to mint the plural names instead would
    /// have left every current assignment pointing at a row nothing checks any more — the
    /// holders would keep a role and silently lose the capability it granted.</para>
    ///
    /// <para>MATCHED BY SUFFIX RATHER THAN LISTED. The scoped names are composed from the
    /// <c>EntityType</c> and <c>ContentType</c> enums at seed time, so no fixed list written
    /// here could stay right as those grow, and a name minted by a release later than this
    /// migration would be missed by a list but is caught by the suffix. Same reasoning
    /// <c>SeedData</c> gives for walking the enums instead of hand-writing the array.</para>
    ///
    /// <para><c>Admin</c> CANNOT SIMPLY BE RENAMED: <c>Administrators</c> already exists and
    /// <c>NormalizedName</c> is unique. Its members move onto the <c>Administrators</c> row and
    /// the <c>Admin</c> row is then dropped, which is the widening the issue records — every
    /// holder of <c>Administrators</c> now carries Core's moderation authority as well as the
    /// portal's <c>/api/admin</c>. In practice the seeded site administrators already held
    /// both. Only where <c>Administrators</c> is somehow absent is <c>Admin</c> renamed into
    /// it instead, so no membership is ever dropped on the floor.</para>
    /// </summary>
    /// <para>ROLLING THE APP BACK NEEDS THIS MIGRATION ROLLED BACK FIRST, and that is not the
    /// usual app-only rollback. The previous build's <c>SeedData</c> mints the singular names it
    /// knows, and <c>EnsureRoleAsync</c> creates any it cannot find — so an old build started
    /// against a migrated store re-creates <c>Reviewer</c>, <c>Tag-Reviewer</c> and the rest as
    /// EMPTY rows while every holder stays on the plural row, which is the silent
    /// loss-of-capability this migration exists to prevent, arrived at from the other side. It
    /// then cannot be repaired by migrating either way: the <c>NOT EXISTS</c> guards below read a
    /// re-seeded target as "already done" and skip, in <c>Up</c> and <c>Down</c> alike. So a
    /// rollback runs <c>dotnet ef database update AddUserProfileFields</c> BEFORE the old build
    /// starts, not after.</para>
    /// <inheritdoc />
    public partial class PluraliseRoleNamesAndCollapseAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            RenameRole(migrationBuilder, from: "Reviewer", to: "Reviewers");
            RenameRole(migrationBuilder, from: "Publisher", to: "Publishers");

            RenameRolesBySuffix(migrationBuilder, from: "-Reviewer", to: "-Reviewers");
            RenameRolesBySuffix(migrationBuilder, from: "-Publisher", to: "-Publishers");

            // Only reached where the portal's own row never existed — otherwise the merge below
            // runs instead. Either way nobody loses a membership.
            RenameRole(migrationBuilder, from: "Admin", to: "Administrators");

            migrationBuilder.Sql(
                @"INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
                  SELECT adminMembership.[UserId], administrators.[Id]
                  FROM [AspNetUserRoles] adminMembership
                  INNER JOIN [AspNetRoles] admin
                      ON admin.[Id] = adminMembership.[RoleId]
                      AND admin.[NormalizedName] = N'ADMIN'
                  INNER JOIN [AspNetRoles] administrators
                      ON administrators.[NormalizedName] = N'ADMINISTRATORS'
                  WHERE NOT EXISTS (
                      SELECT 1
                      FROM [AspNetUserRoles] held
                      WHERE held.[UserId] = adminMembership.[UserId]
                          AND held.[RoleId] = administrators.[Id]);");

            migrationBuilder.Sql(
                @"DELETE adminMembership
                  FROM [AspNetUserRoles] adminMembership
                  INNER JOIN [AspNetRoles] admin
                      ON admin.[Id] = adminMembership.[RoleId]
                  WHERE admin.[NormalizedName] = N'ADMIN';");

            migrationBuilder.Sql(
                @"DELETE adminClaim
                  FROM [AspNetRoleClaims] adminClaim
                  INNER JOIN [AspNetRoles] admin
                      ON admin.[Id] = adminClaim.[RoleId]
                  WHERE admin.[NormalizedName] = N'ADMIN';");

            migrationBuilder.Sql(
                @"DELETE FROM [AspNetRoles] WHERE [NormalizedName] = N'ADMIN';");
        }

        /// <summary>
        /// The plural names go back to singular, and every <c>Administrators</c> holder is given
        /// an <c>Admin</c> row again — which is what the seed did for a site administrator, and
        /// the closest this can get. Which of the two a user held before the merge is recorded
        /// nowhere, so a down migration cannot restore the split exactly: anyone who had been
        /// granted <c>Administrators</c> alone comes back holding both.
        /// </summary>
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = N'ADMIN')
                  BEGIN
                      INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
                      VALUES (NEWID(), N'Admin', N'ADMIN', CONVERT(nvarchar(50), NEWID()));
                  END;");

            migrationBuilder.Sql(
                @"INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
                  SELECT administratorMembership.[UserId], admin.[Id]
                  FROM [AspNetUserRoles] administratorMembership
                  INNER JOIN [AspNetRoles] administrators
                      ON administrators.[Id] = administratorMembership.[RoleId]
                      AND administrators.[NormalizedName] = N'ADMINISTRATORS'
                  INNER JOIN [AspNetRoles] admin
                      ON admin.[NormalizedName] = N'ADMIN'
                  WHERE NOT EXISTS (
                      SELECT 1
                      FROM [AspNetUserRoles] held
                      WHERE held.[UserId] = administratorMembership.[UserId]
                          AND held.[RoleId] = admin.[Id]);");

            RenameRolesBySuffix(migrationBuilder, from: "-Reviewers", to: "-Reviewer");
            RenameRolesBySuffix(migrationBuilder, from: "-Publishers", to: "-Publisher");

            RenameRole(migrationBuilder, from: "Reviewers", to: "Reviewer");
            RenameRole(migrationBuilder, from: "Publishers", to: "Publisher");
        }

        // The NOT EXISTS guard is what makes every rename here re-runnable and collision-safe:
        // Identity's unique index on NormalizedName would fail the statement rather than quietly
        // skip it, so the guard is what lets this be re-applied at all.
        //
        // It cannot tell "the rename has already happened" from "something else minted the target
        // name while the source row is still populated" — the state an app-only rollback creates.
        // See the rollback note on the class: that state is not reachable from here in either
        // direction, and the guard is why.
        private static void RenameRole(MigrationBuilder migrationBuilder, string from, string to) =>
            migrationBuilder.Sql(
                $@"UPDATE [AspNetRoles]
                   SET [Name] = N'{to}', [NormalizedName] = N'{to.ToUpperInvariant()}'
                   WHERE [NormalizedName] = N'{from.ToUpperInvariant()}'
                       AND NOT EXISTS (
                           SELECT 1
                           FROM [AspNetRoles] existing
                           WHERE existing.[NormalizedName] = N'{to.ToUpperInvariant()}');");

        // Rewrites the capability segment of every scoped name at once — Tag-Reviewer,
        // ContentItem-Reviewer and ContentItem-Story-Reviewer alike — so neither the entity type
        // nor the content type has to be enumerated by a migration that cannot see the enums.
        private static void RenameRolesBySuffix(
            MigrationBuilder migrationBuilder,
            string from,
            string to)
        {
            string normalizedFrom = from.ToUpperInvariant();
            string normalizedTo = to.ToUpperInvariant();
            int suffixLength = from.Length;

            migrationBuilder.Sql(
                $@"UPDATE role
                   SET [Name] = LEFT(role.[Name], LEN(role.[Name]) - {suffixLength}) + N'{to}',
                       [NormalizedName] =
                           LEFT(role.[NormalizedName], LEN(role.[NormalizedName]) - {suffixLength})
                               + N'{normalizedTo}'
                   FROM [AspNetRoles] role
                   WHERE role.[NormalizedName] LIKE N'%{normalizedFrom}'
                       AND NOT EXISTS (
                           SELECT 1
                           FROM [AspNetRoles] existing
                           WHERE existing.[NormalizedName] =
                               LEFT(role.[NormalizedName], LEN(role.[NormalizedName]) - {suffixLength})
                                   + N'{normalizedTo}');");
        }
    }
}
