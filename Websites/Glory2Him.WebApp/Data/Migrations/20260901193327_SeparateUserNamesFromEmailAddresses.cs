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
    /// Issue #378 — a username may never be an email address (design §18.3.1), so any row that
    /// already holds one is renamed here. The code fix alone does not reach these: every display
    /// name in the system still ends at the username when a person has set no personal details,
    /// and that fallback is deliberately kept, so what makes it safe is that no username is an
    /// address — which is a statement about the DATA, not only about the paths that write it.
    ///
    /// <para>THE SAME RELEASE NARROWS <c>User.AllowedUserNameCharacters</c> (<c>PortalRegistration</c>),
    /// which is why this cannot be deferred. Once <c>@</c> is not an allowed character, Identity
    /// refuses every write to such an account — a profile edit, an admin change, anything routed
    /// through <c>UserManager</c> — because it re-validates the whole user, not only the field
    /// being changed. An unrenamed row would still sign in and would simply become unmaintainable.
    /// Migration first, then the option: the ordering is the deploy, not a preference.</para>
    ///
    /// <para>EVERY TEST HERE IS THE SAME TEST IDENTITY WILL APPLY, and that is deliberate. The
    /// predicate is not "contains <c>@</c>" but "is not spellable in the character set this release
    /// installs" — <see cref="IsIllegalUserName"/>. Matching on <c>@</c> alone was wrong twice over: it
    /// would leave a username Identity refuses for some OTHER reason unrepaired, and — worse — the
    /// final guard would then certify that row as fixed. A guard that asks a narrower question than
    /// the one that matters is how a bad row ships looking green.</para>
    ///
    /// <para>THE CHARACTER CLASS IS COLLATION-PINNED AND ITS DASH COMES FIRST, both the hard way.
    /// A <c>LIKE</c> range is resolved by COLLATION ORDER, not code point, so under this database's
    /// <c>SQL_Latin1_General_CP1_CI_AS</c> the range <c>a-z</c> swallows accented Latin letters:
    /// <c>N'josé' NOT LIKE N'%[^a-zA-Z0-9._+-]%'</c> is TRUE, and 482 characters between U+00AA and
    /// U+02BC pass a test meant to admit ASCII. <c>COLLATE Latin1_General_BIN2</c> makes the ranges
    /// code-point ranges again. Separately, a trailing <c>+-]</c> is read as the RANGE <c>+</c> to
    /// <c>]</c> rather than as <c>+</c> plus a literal dash — which under a binary collation admits
    /// <c>,</c> <c>/</c> <c>:</c> <c>;</c> <c>&lt;</c> <c>=</c> <c>&gt;</c> <c>?</c> <c>@</c>
    /// <c>[</c> <c>\</c> <c>]</c>, <c>@</c> INCLUDED. Putting the dash immediately after the
    /// <c>^</c> makes it literal. Verified by enumerating <c>NCHAR(32..1000)</c> against the exact
    /// set <c>PortalRegistration</c> installs: the form used here wrongly accepts zero of them.</para>
    ///
    /// <para>TWO PRECONDITIONS RUN BEFORE ANYTHING IS WRITTEN, so a database this migration cannot
    /// repair correctly is refused whole rather than half-rewritten. Each throws with what the
    /// operator has to decide, because none of them has an answer a migration is entitled to invent:
    /// an account with no address to fall back on, and an address that does not identify one
    /// account, are both questions about who a person is.</para>
    ///
    /// <para>TWO PASSES, AND THE ORDER IS THE POINT. The first claims the email's local part —
    /// <c>christo@example.org</c> becomes <c>christo</c> — because the renamed username is what
    /// those accounts will be shown as, and a readable one keeps a reviewer picker usable. It is
    /// taken only when it is already a legal username and genuinely free, checked against every
    /// existing row AND against the other rows this same statement is about to rename, which is
    /// the collision <c>UserNameIndex</c> would otherwise refuse: <c>chris@a.org</c> and
    /// <c>chris@b.org</c> both want <c>chris</c>. The second pass takes whatever the first left
    /// and derives a name from the row's own id, which cannot collide with anything.</para>
    ///
    /// <para>NOT DERIVED FROM Name/Surname, though <c>SuggestUsernamesAsync</c> exists and does
    /// exactly that. Those fields are precisely what is blank for this population — an account
    /// that had completed its personal details would not be reaching the username fallback in the
    /// first place — so <c>BuildCandidates</c> would return nothing for most of them and the
    /// migration would have no answer.</para>
    ///
    /// <para>THE SEEDED <c>admin</c> IS EXCLUDED BY NAME. <c>TestAuthHandler</c> and
    /// <c>ApiBroker.Roles</c> bind to that literal and the acceptance suite depends on it, so
    /// renaming it would break the build rather than close a leak. It does not hold an email today
    /// (<c>SeedData</c> pairs <c>admin</c> with <c>admin@g2h.org</c>), so the exclusion guards
    /// against a future seed change rather than skipping a repair that is owed.</para>
    ///
    /// <para>THE FINAL CHECK THROWS rather than reporting success over a row it could not rename.
    /// A leak that survives a migration named after closing it is worse than a failed deploy,
    /// because nothing afterwards looks wrong.</para>
    /// </summary>
    /// <inheritdoc />
    public partial class SeparateUserNamesFromEmailAddresses : Migration
    {
        // The one place the character set is spelled: Identity's default minus '@', exactly what
        // PortalRegistration installs. Written as "does this expression contain something outside
        // the set", pinned to a binary collation and with the dash leading, for the two reasons the
        // class summary gives. Every test below composes from here so none of them can drift.
        private static string IsIllegalUserName(string columnExpression) =>
            $@"{columnExpression} COLLATE Latin1_General_BIN2 LIKE N'%[^-a-zA-Z0-9._+]%'";

        // The rows this migration owes a rename. Not "contains @": the test is the one Identity
        // itself will apply, so a row this migration leaves behind is exactly a row the application
        // cannot write to.
        private static string AffectedRows =>
            IsIllegalUserName("affected.[UserName]")
                + @"
                       AND affected.[NormalizedUserName] <> N'ADMIN'";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PRECONDITION ONE — every account being renamed must keep a way in.
            //
            // Sign-in resolves FindByNameAsync then FindByEmailAsync (AccountApiEndpoints). Renaming
            // is therefore only safe while the address still finds the account: take the username
            // away from a row with no usable address and the person has neither of the two things
            // they could type. Pass two would happily do that — its candidate is derived from the
            // row's id, so it does not care whether the Email column has anything in it.
            //
            // Deliberately NOT "the address must contain '@'". A row whose Email column holds
            // something odd still resolves through FindByEmailAsync, because that lookup matches
            // NormalizedEmail rather than parsing an address — so refusing it would stop a deploy
            // over an account that signs in perfectly well. The provable stranding is an empty
            // column, and that is all this refuses.
            //
            // There is no repair a migration could choose here. Inventing an address, or leaving the
            // account with an unusable username, are both worse than stopping and saying so.
            migrationBuilder.Sql(
                $@"IF EXISTS (
                       SELECT 1
                       FROM [AspNetUsers] affected
                       WHERE {AffectedRows}
                           AND (affected.[Email] IS NULL
                               OR LTRIM(RTRIM(affected.[Email])) = N''))
                   BEGIN
                       THROW 50378, N'Issue #378: an account whose username must be renamed has no usable email address, and sign-in falls back to the address once the username is gone. Give it an address, or remove the account, then deploy again.', 1;
                   END;");

            // PRECONDITION TWO — the address it falls back to has to identify ONE account.
            //
            // AspNetUsers.EmailIndex is NOT unique and RequireUniqueEmail is left at its default of
            // false, so two accounts may hold one address. That is tolerable only while the username
            // is the primary way in. This migration makes the address the ONLY way in for the rows it
            // renames, and Identity's FindByEmailAsync is SingleOrDefaultAsync — so a shared address
            // turns a correct password into an unhandled 500 rather than a sign-in.
            //
            // Design §18.3.1 leaves RequireUniqueEmail deliberately unsettled. This does not settle it:
            // it refuses only the rows whose safety would depend on the answer, and leaves every other
            // duplicate exactly as it found it.
            migrationBuilder.Sql(
                $@"IF EXISTS (
                       SELECT 1
                       FROM [AspNetUsers] affected
                       WHERE {AffectedRows}
                           AND affected.[NormalizedEmail] IS NOT NULL
                           AND EXISTS (
                               SELECT 1
                               FROM [AspNetUsers] other
                               WHERE other.[Id] <> affected.[Id]
                                   AND other.[NormalizedEmail] = affected.[NormalizedEmail]))
                   BEGIN
                       THROW 50379, N'Issue #378: an account whose username must be renamed shares its email address with another account, and sign-in by address cannot tell them apart once the username is gone. Resolve the duplicate, then deploy again.', 1;
                   END;");

            // Pass one — the email's local part, where it is legal, long enough and unclaimed.
            //
            // ConcurrencyStamp is rolled with the rename because that is what tells an AppUser
            // instance loaded before this migration that its copy is stale; without it a request
            // in flight across the deploy could save the old username straight back.
            migrationBuilder.Sql(
                $@"UPDATE affected
                   SET [UserName] = candidate.[UserName],
                       [NormalizedUserName] = UPPER(candidate.[UserName]),
                       [ConcurrencyStamp] = CONVERT(nvarchar(50), NEWID())
                   FROM [AspNetUsers] affected
                   CROSS APPLY (
                       SELECT LEFT(
                           affected.[Email],
                           NULLIF(CHARINDEX(N'@', affected.[Email]), 0) - 1)
                   ) candidate([UserName])
                   WHERE {AffectedRows}
                       AND candidate.[UserName] IS NOT NULL
                       AND LEN(candidate.[UserName]) >= 3
                       AND NOT ({IsIllegalUserName("candidate.[UserName]")})
                       AND NOT EXISTS (
                           SELECT 1
                           FROM [AspNetUsers] taken
                           WHERE taken.[NormalizedUserName] = UPPER(candidate.[UserName]))
                       AND NOT EXISTS (
                           SELECT 1
                           FROM [AspNetUsers] rival
                           WHERE rival.[Id] <> affected.[Id]
                               AND {IsIllegalUserName("rival.[UserName]")}
                               AND rival.[NormalizedUserName] <> N'ADMIN'
                               AND UPPER(LEFT(
                                   rival.[Email],
                                   NULLIF(CHARINDEX(N'@', rival.[Email]), 0) - 1))
                                   = UPPER(candidate.[UserName]));");

            // Pass two — whatever pass one could not name. The id is already unique, so this
            // cannot collide with another affected row; the NOT EXISTS covers only the absurd
            // case of an existing account having chosen this exact shape by hand. Precondition one
            // has already established that every row reaching here still has an address to sign in
            // with, which is what makes discarding the old username safe.
            migrationBuilder.Sql(
                $@"UPDATE affected
                   SET [UserName] = candidate.[UserName],
                       [NormalizedUserName] = UPPER(candidate.[UserName]),
                       [ConcurrencyStamp] = CONVERT(nvarchar(50), NEWID())
                   FROM [AspNetUsers] affected
                   CROSS APPLY (
                       SELECT N'user-'
                           + LOWER(REPLACE(CONVERT(nvarchar(36), affected.[Id]), N'-', N''))
                   ) candidate([UserName])
                   WHERE {AffectedRows}
                       AND NOT EXISTS (
                           SELECT 1
                           FROM [AspNetUsers] taken
                           WHERE taken.[NormalizedUserName] = UPPER(candidate.[UserName]));");

            // The same question Identity will ask on the next write to each row, asked once here
            // while somebody is still watching.
            migrationBuilder.Sql(
                $@"IF EXISTS (SELECT 1 FROM [AspNetUsers] affected WHERE {AffectedRows})
                   BEGIN
                       THROW 50380, N'Issue #378: a username is still not spellable in the character set this release installs. Deploying past this would leave an account nobody can edit, and would ship the leak the migration exists to close.', 1;
                   END;");
        }

        /// <summary>
        /// Deliberately empty. The usernames this migration replaced are recorded nowhere — it
        /// rewrites them in place, exactly as the role rename did — so there is nothing to put
        /// back, and inventing an address to restore would recreate the leak on the way out.
        ///
        /// <para>Nobody is stranded by that, and precondition one is what makes the claim true
        /// rather than hopeful: sign-in falls back from <c>FindByNameAsync</c> to
        /// <c>FindByEmailAsync</c> (<c>AccountApiEndpoints</c>), and no row is renamed unless it
        /// has an address that reaches it. A renamed account still signs in with the address it
        /// always used — on the old build as much as the new one.</para>
        /// </summary>
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
