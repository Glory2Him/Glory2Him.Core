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
using Glory2Him.WebApp.Models.Foundations.Roles;
using Glory2Him.WebApp.Models.Foundations.Users;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Glory2Him.WebApp.Tests.Acceptance.Data
{
    /// <summary>
    /// Builds a pre-#368 Identity store, applies the one rename migration to it, and reads the
    /// result back. Run once for the whole test class; the assertions live in
    /// <see cref="RoleVocabularyMigrationTests"/>.
    ///
    /// <para><b>Migrated in two steps against its own catalogue.</b> The store is first brought
    /// up to <see cref="PreviousMigration"/> — the last migration before the rename — and
    /// populated there, so the rows exist before the statements under test run. Migrating
    /// straight to the head and inserting afterwards would test nothing: every UPDATE would have
    /// matched zero rows on the way past.</para>
    ///
    /// <para><b>The arrangement is written by hand, not by <c>SeedData</c>.</b> SeedData mints
    /// the names of the release it ships in, so seeding this store would produce the plural names
    /// and the migration would find its work already done — the NOT EXISTS guards would skip
    /// every rename and the rehearsal would pass while proving nothing. The pre-state has to be
    /// the vocabulary of the release being upgraded FROM, which only a literal list can
    /// express.</para>
    /// </summary>
    public sealed class RoleVocabularyMigrationRehearsal : IAsyncLifetime
    {
        // Named rather than timestamped: EF resolves either, and the name survives a rebase that
        // renumbers the file.
        private const string PreviousMigration = "AddUserProfileFields";
        private const string RenameMigration = "PluraliseRoleNamesAndCollapseAdmin";

        /// <summary>Held both administrator roles, as <c>SeedData</c> granted them together.</summary>
        internal static readonly Guid SiteAdministratorId =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        /// <summary>
        /// Held ONLY Core's <c>Admin</c> — a moderator who was never a portal administrator.
        /// The case the merge exists for, and the one that loses everything if it is skipped.
        /// </summary>
        internal static readonly Guid CoreOnlyAdministratorId =
            Guid.Parse("22222222-2222-2222-2222-222222222222");

        /// <summary>Held two entity-scoped grants, to prove a rename carries more than one.</summary>
        internal static readonly Guid ScopedReviewerId =
            Guid.Parse("33333333-3333-3333-3333-333333333333");

        /// <summary>Held a content-type-scoped grant — the tier with two hyphens in its name.</summary>
        internal static readonly Guid NarrowReviewerId =
            Guid.Parse("44444444-4444-4444-4444-444444444444");

        // The vocabulary as main spells it today: both administrator roles, the singular
        // capability at all three tiers, and ReadOnly already singular.
        private static readonly string[] PreMigrationRoleNames = new[]
        {
            "Administrators",
            "Users",
            "Admin",
            "Reviewer",
            "Publisher",
            "ReadOnly",
            "ContentItem-ReadOnly",
            "ContentItem-Reviewer",
            "ContentItem-Publisher",
            "Tag-ReadOnly",
            "Tag-Reviewer",
            "Tag-Publisher",
            "Link-ReadOnly",
            "Link-Reviewer",
            "Link-Publisher",
            "ContentItem-Story-Reviewer",
            "ContentItem-Story-Publisher",
            "ContentItem-Testimony-Reviewer",
            "ContentItem-Testimony-Publisher"
        };

        private readonly Dictionary<Guid, List<string>> rolesByUser = new();
        private readonly Dictionary<string, string> normalizedNamesByName = new();

        /// <summary>
        /// Every surviving role, keyed by <c>Name</c> and valued by its <c>NormalizedName</c>.
        ///
        /// <para>Both halves are carried because Identity resolves a role through
        /// <c>NormalizedName</c> and displays <c>Name</c>: a migration that rewrote only one of
        /// them would leave a row that looks renamed in the admin UI and matches nothing at the
        /// gate, or the reverse. Asserting on <c>Name</c> alone cannot see that.</para>
        /// </summary>
        internal IReadOnlyDictionary<string, string> MigratedRoles => this.normalizedNamesByName;

        internal int MigratedAdminRoleClaimCount { get; private set; }

        internal IReadOnlyList<string> RolesHeldBy(Guid userId) =>
            this.rolesByUser.TryGetValue(userId, out List<string> roles)
                ? roles
                : new List<string>();

        public async ValueTask InitializeAsync()
        {
            await MigrateToAsync(PreviousMigration);
            await ArrangePreMigrationStoreAsync();
            await MigrateToAsync(RenameMigration);
            await ReadMigratedStoreAsync();
        }

        // The catalogue is dropped by AcceptanceDatabaseBroker along with the other three, so
        // there is nothing to undo here — one teardown for the suite beats a second one that can
        // disagree with it about which server the databases are on.
        public ValueTask DisposeAsync() =>
            ValueTask.CompletedTask;

        private static SecurityDbContext CreateContext()
        {
            DbContextOptions<SecurityDbContext> options =
                new DbContextOptionsBuilder<SecurityDbContext>()
                    .UseSqlServer(AcceptanceDatabaseBroker.SecurityRehearsalConnectionString)
                    .Options;

            return new SecurityDbContext(options);
        }

        private static async Task MigrateToAsync(string targetMigration)
        {
            using SecurityDbContext securityDbContext = CreateContext();
            IMigrator migrator = securityDbContext.GetService<IMigrator>();

            await migrator.MigrateAsync(targetMigration);
        }

        private static async Task ArrangePreMigrationStoreAsync()
        {
            using SecurityDbContext securityDbContext = CreateContext();

            Dictionary<string, AppRole> rolesByName = PreMigrationRoleNames.ToDictionary(
                roleName => roleName,
                roleName => new AppRole
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                });

            securityDbContext.Roles.AddRange(rolesByName.Values);

            securityDbContext.Users.AddRange(
                CreateUser(SiteAdministratorId, "siteadmin"),
                CreateUser(CoreOnlyAdministratorId, "coreadmin"),
                CreateUser(ScopedReviewerId, "tagreviewer"),
                CreateUser(NarrowReviewerId, "storyreviewer"));

            await securityDbContext.SaveChangesAsync();

            securityDbContext.UserRoles.AddRange(
                Membership(SiteAdministratorId, rolesByName["Administrators"]),
                Membership(SiteAdministratorId, rolesByName["Admin"]),
                Membership(CoreOnlyAdministratorId, rolesByName["Admin"]),
                Membership(ScopedReviewerId, rolesByName["Tag-Reviewer"]),
                Membership(ScopedReviewerId, rolesByName["Tag-Publisher"]),
                Membership(NarrowReviewerId, rolesByName["ContentItem-Story-Reviewer"]));

            // Role claims FK to the role, so the Admin drop has to clear them or fail on the
            // constraint. One is enough to prove the migration does.
            securityDbContext.RoleClaims.Add(new IdentityRoleClaim<Guid>
            {
                RoleId = rolesByName["Admin"].Id,
                ClaimType = "scope",
                ClaimValue = "admin.users"
            });

            await securityDbContext.SaveChangesAsync();
        }

        private static AppUser CreateUser(Guid userId, string userName) =>
            new AppUser
            {
                Id = userId,
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                Email = $"{userName}@g2h.test",
                NormalizedEmail = $"{userName}@g2h.test".ToUpperInvariant(),
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

        private static IdentityUserRole<Guid> Membership(Guid userId, AppRole role) =>
            new IdentityUserRole<Guid> { UserId = userId, RoleId = role.Id };

        // Read back through a FRESH context: the arranging one still tracks the rows under their
        // pre-migration names, and a rename issued as SQL underneath it never reaches its
        // change tracker.
        private async Task ReadMigratedStoreAsync()
        {
            using SecurityDbContext securityDbContext = CreateContext();

            List<AppRole> roles = await securityDbContext.Roles.AsNoTracking().ToListAsync();
            List<IdentityUserRole<Guid>> memberships =
                await securityDbContext.UserRoles.AsNoTracking().ToListAsync();

            Dictionary<Guid, string> roleNamesById =
                roles.ToDictionary(role => role.Id, role => role.Name);

            foreach (AppRole role in roles)
            {
                this.normalizedNamesByName[role.Name] = role.NormalizedName;
            }

            foreach (IdentityUserRole<Guid> membership in memberships)
            {
                if (this.rolesByUser.TryGetValue(membership.UserId, out List<string> heldRoles)
                    is false)
                {
                    heldRoles = new List<string>();
                    this.rolesByUser[membership.UserId] = heldRoles;
                }

                heldRoles.Add(roleNamesById[membership.RoleId]);
            }

            // Counted by the claim's own value rather than by joining to a role that should no
            // longer exist: the assertion is that the Admin row's claim went with it, and a join
            // to a missing row would return zero whether the claim was deleted or orphaned.
            //
            // This cannot distinguish the migration's explicit DELETE from the cascade Identity
            // configures on AspNetRoleClaims, and it is not meant to — what it pins is the END
            // state, that dropping the role leaves no claim addressing it behind.
            this.MigratedAdminRoleClaimCount =
                await securityDbContext.RoleClaims
                    .AsNoTracking()
                    .CountAsync(roleClaim => roleClaim.ClaimValue == "admin.users");
        }
    }
}
