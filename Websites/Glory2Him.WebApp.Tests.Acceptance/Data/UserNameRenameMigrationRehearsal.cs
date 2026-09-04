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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Glory2Him.WebApp.Data;
using Glory2Him.WebApp.Models.Foundations.Users;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Glory2Him.WebApp.Tests.Acceptance.Data
{
    /// <summary>
    /// Builds a pre-#378 Identity store, applies the username rename to it, and reads the result
    /// back — then builds a SECOND store the migration must refuse, and captures the refusal.
    /// Run once for the whole test class; the assertions live in
    /// <see cref="UserNameRenameMigrationTests"/>. Modelled on
    /// <see cref="RoleVocabularyMigrationRehearsal"/>, for the same reason it exists.
    ///
    /// <para><b>Why this is worth its weight.</b> The migration's correctness lives in SQL that no
    /// other test executes: the suite's own Security catalogue is created empty and migrated on the
    /// way up, so every statement in the rename matches zero rows there. Two defects had already
    /// shipped into this migration and been caught by hand — a character class that admitted
    /// accented letters, and a <c>STRING_AGG</c> that silently disarmed a precondition once the
    /// blocked population passed 105 rows. Neither would have failed a single test.</para>
    ///
    /// <para><b>Rows are inserted at the PREVIOUS migration, not at head.</b> Migrating straight up
    /// and inserting afterwards would prove nothing — every UPDATE would have matched zero rows on
    /// the way past. They are also written straight through the DbContext rather than through
    /// <c>UserManager</c>, because <c>UserManager</c> is the thing that would refuse them: the
    /// pre-state under test is exactly the data the old build allowed and the new one does not.</para>
    ///
    /// <para><b>The blocked store is a second catalogue</b> because the precondition throws. A store
    /// that fails to migrate cannot also serve the rewrite assertions.</para>
    /// </summary>
    public sealed class UserNameRenameMigrationRehearsal : IAsyncLifetime
    {
        // Named rather than timestamped: EF resolves either, and the name survives a rebase that
        // renumbers the file.
        private const string PreviousMigration = "RenameVersesContentTypeToVerseImage";
        private const string RenameMigration = "SeparateUserNamesFromEmailAddresses";

        /// <summary>An ordinary address whose local part is free, legal and long enough.</summary>
        internal static readonly Guid ClaimsLocalPartId =
            Guid.Parse("aaaa0001-0000-0000-0000-000000000001");

        /// <summary>Wants the same local part as <see cref="RivalForLocalPartId"/>.</summary>
        internal static readonly Guid CollidesOnLocalPartId =
            Guid.Parse("aaaa0002-0000-0000-0000-000000000002");

        /// <summary>The other half of that collision.</summary>
        internal static readonly Guid RivalForLocalPartId =
            Guid.Parse("aaaa0003-0000-0000-0000-000000000003");

        /// <summary>
        /// The defect that shipped: an ASCII email-shaped username beside an address whose local
        /// part carries an accented letter. The old class admitted it under the database collation
        /// and minted a username Identity refuses.
        /// </summary>
        internal static readonly Guid AccentedLocalPartId =
            Guid.Parse("aaaa0004-0000-0000-0000-000000000004");

        /// <summary>A legal username that must be left exactly as it is.</summary>
        internal static readonly Guid UntouchedId =
            Guid.Parse("aaaa0005-0000-0000-0000-000000000005");

        /// <summary>
        /// Two rows whose <c>NormalizedUserName</c> was never populated — what a raw SQL insert or
        /// a legacy import that filled <c>UserName</c> alone leaves behind. <c>UserNameIndex</c> is
        /// FILTERED (<c>WHERE NormalizedUserName IS NOT NULL</c>), so more than one such row is
        /// legal, and both addresses share the local part <c>drew</c>.
        ///
        /// <para>This pair is the reason the rehearsal exists. The migration's ADMIN exclusion is
        /// UNKNOWN for a NULL normalized name, so before <see cref="NeedsRename"/> was shared these
        /// two were selected for rename by the outer statement and invisible to the rival subquery
        /// that decides whether either may take the contested name — and both took it, against a
        /// unique index.</para>
        /// </summary>
        internal static readonly Guid NullNormalizedFirstId =
            Guid.Parse("aaaa0006-0000-0000-0000-000000000006");

        internal static readonly Guid NullNormalizedSecondId =
            Guid.Parse("aaaa0007-0000-0000-0000-000000000007");

        internal static readonly Guid BlockedWithoutEmailId =
            Guid.Parse("bbbb0001-0000-0000-0000-000000000001");

        // U+00E9. Written as a code point rather than a literal so no editor or encoding round-trip
        // can quietly turn it into a plain 'e' and take the test's teeth with it.
        private static readonly string AccentedLocalPart = "jos" + (char)0x00E9;

        internal IReadOnlyDictionary<Guid, string> UserNamesById => this.userNamesById;

        /// <summary>
        /// Carried alongside the plain name because <c>NormalizedUserName</c> is the column the
        /// sign-in lookup resolves against — a row renamed in one and not the other would look
        /// repaired in the admin UI and match nothing at the gate.
        /// </summary>
        internal IReadOnlyDictionary<Guid, string> NormalizedUserNamesById =>
            this.normalizedUserNamesById;

        internal string BlockedFailureMessage { get; private set; } = string.Empty;

        internal static int BlockedAccounts => BlockedAccountCount;

        private readonly Dictionary<Guid, string> userNamesById = new Dictionary<Guid, string>();

        private readonly Dictionary<Guid, string> normalizedUserNamesById =
            new Dictionary<Guid, string>();

        public async ValueTask InitializeAsync()
        {
            string rewritten = AcceptanceDatabaseBroker.UserNameRehearsalConnectionString;

            await MigrateToAsync(rewritten, PreviousMigration);
            await ArrangeRewrittenStoreAsync(rewritten);
            await MigrateToAsync(rewritten, RenameMigration);
            await ReadUserNamesAsync(rewritten);

            string blocked = AcceptanceDatabaseBroker.UserNameBlockedRehearsalConnectionString;

            await MigrateToAsync(blocked, PreviousMigration);
            await ArrangeBlockedStoreAsync(blocked);

            try
            {
                await MigrateToAsync(blocked, RenameMigration);
            }
            catch (Exception migrationException)
            {
                // The whole chain: EF wraps the SqlException, and the THROW's text is what the
                // operator will actually read.
                BlockedFailureMessage = string.Join(
                    " | ",
                    Unwind(migrationException).Select(exception => exception.Message));
            }
        }

        // Both catalogues are dropped by AcceptanceDatabaseBroker along with the rest of
        // DatabaseNames, so there is nothing to undo here.
        public ValueTask DisposeAsync() =>
            ValueTask.CompletedTask;

        private static IEnumerable<Exception> Unwind(Exception exception)
        {
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                yield return current;
            }
        }

        private static SecurityDbContext CreateContext(string connectionString)
        {
            DbContextOptions<SecurityDbContext> options =
                new DbContextOptionsBuilder<SecurityDbContext>()
                    .UseSqlServer(connectionString)
                    .Options;

            return new SecurityDbContext(options);
        }

        private static async Task MigrateToAsync(string connectionString, string targetMigration)
        {
            using SecurityDbContext securityDbContext = CreateContext(connectionString);
            IMigrator migrator = securityDbContext.GetService<IMigrator>();

            await migrator.MigrateAsync(targetMigration);
        }

        private static async Task ArrangeRewrittenStoreAsync(string connectionString)
        {
            using SecurityDbContext securityDbContext = CreateContext(connectionString);

            securityDbContext.Users.AddRange(
                CreateUser(ClaimsLocalPartId, "pat@c.example", "pat@c.example"),
                CreateUser(CollidesOnLocalPartId, "chris@a.example", "chris@a.example"),
                CreateUser(RivalForLocalPartId, "chris@b.example", "chris@b.example"),
                CreateUser(AccentedLocalPartId, "jose@old.example", AccentedLocalPart + "@new.example"),
                CreateUser(UntouchedId, "already.legal-1_x", "already@legal.example"),
                CreateUserWithoutNormalizedName(NullNormalizedFirstId, "drew@a.example"),
                CreateUserWithoutNormalizedName(NullNormalizedSecondId, "drew@b.example"));

            await securityDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Deliberately MORE THAN 105 rows, which is not padding.
        ///
        /// <para>The precondition builds its operator message with <c>STRING_AGG</c>, and that
        /// aggregate returns <c>nvarchar(4000)</c> unless its input is already a LOB type. At 106
        /// ids the aggregate overflows with error 9829 — which terminates only the assignment, so
        /// the variable is left NULL, <c>IF @strandedIds IS NOT NULL</c> is false, and the THROW
        /// never fires. A precondition that silently disarms itself exactly when the blocked
        /// population is large is worse than not having one, and a one-row arrangement cannot see
        /// it: the bug shipped, and every test passed.</para>
        /// </summary>
        private const int BlockedAccountCount = 110;

        private static async Task ArrangeBlockedStoreAsync(string connectionString)
        {
            using SecurityDbContext securityDbContext = CreateContext(connectionString);

            // The first id is fixed so the assertion can name it; the rest only have to exist.
            securityDbContext.Users.Add(
                CreateUser(BlockedWithoutEmailId, "nomail@x.example", email: null));

            for (var index = 1; index < BlockedAccountCount; index++)
            {
                securityDbContext.Users.Add(
                    CreateUser(
                        Guid.Parse($"bbbb0002-0000-0000-0000-{index:D12}"),
                        $"nomail{index}@x.example",
                        email: null));
            }

            await securityDbContext.SaveChangesAsync();
        }

        private async Task ReadUserNamesAsync(string connectionString)
        {
            // A FRESH context: the arranging one still tracks the rows under their pre-migration
            // names, and a rename issued as SQL underneath it never reaches its change tracker.
            using SecurityDbContext securityDbContext = CreateContext(connectionString);

            List<AppUser> users = await securityDbContext.Users.AsNoTracking().ToListAsync();

            foreach (AppUser user in users)
            {
                this.userNamesById[user.Id] = user.UserName ?? string.Empty;
                this.normalizedUserNamesById[user.Id] = user.NormalizedUserName ?? string.Empty;
            }
        }

        // NormalizedUserName left NULL on purpose. UserManager could never produce this; a direct
        // insert can, and the filtered unique index permits any number of them.
        private static AppUser CreateUserWithoutNormalizedName(Guid userId, string userName)
        {
            AppUser user = CreateUser(userId, userName, $"{userName}");
            user.NormalizedUserName = null;

            return user;
        }

        private static AppUser CreateUser(Guid userId, string userName, string? email) =>
            new AppUser
            {
                Id = userId,
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email?.ToUpperInvariant(),
                Name = string.Empty,
                Surname = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
    }
}
