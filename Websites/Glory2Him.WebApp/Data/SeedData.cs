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

using System.Data.Common;
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Models.Foundations.Roles;
using Glory2Him.WebApp.Models.Foundations.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.WebApp.Data
{
    // Idempotent first-run seed: creates the Administrators and Users roles and the default
    // admin/user accounts (Spec Section 6.3). Default credentials are intentionally weak for
    // first-run/demo; production must enforce a strong password policy and force-change.
    public static class SeedData
    {
        private const string AdministratorsRole = "Administrators";
        private const string UsersRole = "Users";

        // Glory2Him.Core decides authorization against role NAMES it owns, compared by exact
        // ordinal equality — never by suffix. The portal's own "Administrators" is a different
        // vocabulary and satisfies none of them, so until these rows exist and somebody holds
        // them the moderation tier is unreachable: approve and hard delete answer 403 at the
        // attribute, and a moderator can neither modify another user's tag nor see non-public
        // rows. Referenced from Core rather than re-spelled here so the two cannot drift.
        //
        // Reviewer and Tag-Reviewer appear in no [Authorize(Roles = ...)] list — the gates they
        // satisfy are owner-OR-review-role and cannot be written as a fixed list — but they are
        // what makes a reviewer's write and read reach past their own rows (§14.7 posture A).
        // Both tiers are provisioned: HasReviewRole tests the global Reviewer as well as the
        // entity-scoped one, so seeding only the scoped role would leave half the rule dead.
        private static readonly string[] CoreRoles = new[]
        {
            Roles.Admin,
            Roles.Reviewer,
            Roles.Publisher,
            Roles.TagPublisher,
            Roles.TagReviewer,

            // The block tier (design §18.6): "assigned to users who misbehave, takes precedence
            // over every other role". The foundation tests for these on every write and on hard
            // delete, but SeedData is the only place a role can be minted — IIdentityBroker
            // assigns and never creates — so without these rows the sanction path is code that
            // can never be reached and an administrator has no way to restrain a contributor.
            Roles.ReadOnly,
            Roles.TagReadOnly
        };

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            IServiceProvider services = scope.ServiceProvider;

            var securityDbContext = services.GetRequiredService<SecurityDbContext>();
            await securityDbContext.Database.MigrateAsync();
            await DisableAutoCloseForLocalDbAsync(securityDbContext);

            var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();

            await EnsureRoleAsync(roleManager, AdministratorsRole);
            await EnsureRoleAsync(roleManager, UsersRole);

            foreach (string coreRole in CoreRoles)
            {
                await EnsureRoleAsync(roleManager, coreRole);
            }

            await EnsureUserAsync(
                userManager,
                userName: "admin",
                password: "admin",
                roleNames: AdministratorRoleNames(),
                email: "admin@g2h.org",
                name: "Admin",
                surname: "User");

            await EnsureUserAsync(
                userManager,
                userName: "user",
                password: "user",
                roleNames: new[] { UsersRole },
                email: "user@g2h.org",
                name: "Normal",
                surname: "User");

            await EnsureUserAsync(
                userManager,
                userName: "cjdutoit",
                password: "P@ssword!",
                roleNames: AdministratorRoleNames(),
                email: "christo@dutoit.co.uk",
                name: "Christo",
                surname: "du Toit",
                dateOfBirth: new DateOnly(1977, 10, 8));
        }

        // A site administrator holds the portal's own role AND Core's, because the two govern
        // different surfaces: "Administrators" opens /api/admin, Roles.Admin opens the tag
        // moderation tier. Granting only the first is the state issue #193 describes.
        private static string[] AdministratorRoleNames() =>
            new[] { AdministratorsRole, Roles.Admin };

        // LocalDB creates databases with AUTO_CLOSE ON (inherited from the model database), which
        // cold-starts the database on every connection and can surface as a transient 0x89c5010a on
        // connection open. Turn it off so the database stays warm. Only attempted for a (localdb)
        // data source (a no-op on real SQL Server, where AUTO_CLOSE is already OFF) and best-effort:
        // failures (e.g. the account cannot ALTER DATABASE) are ignored.
        private static async Task DisableAutoCloseForLocalDbAsync(SecurityDbContext securityDbContext)
        {
            DbConnection connection = securityDbContext.Database.GetDbConnection();

            bool isLocalDb = connection.DataSource?.Contains(
                "(localdb)", StringComparison.OrdinalIgnoreCase) is true;

            if (isLocalDb is false)
            {
                return;
            }

            try
            {
                string databaseName = connection.Database.Replace("]", "]]");

                await securityDbContext.Database.ExecuteSqlRawAsync(
                    $"ALTER DATABASE [{databaseName}] SET AUTO_CLOSE OFF WITH NO_WAIT;");
            }
            catch
            {
                // Best-effort dev-experience tweak; ignore when AUTO_CLOSE cannot be changed.
            }
        }

        private static async Task EnsureRoleAsync(
            RoleManager<AppRole> roleManager,
            string roleName)
        {
            if ((await roleManager.RoleExistsAsync(roleName)) is false)
            {
                await roleManager.CreateAsync(new AppRole { Name = roleName });
            }
        }

        private static async Task EnsureUserAsync(
            UserManager<AppUser> userManager,
            string userName,
            string password,
            string[] roleNames,
            string email,
            string name,
            string surname,
            DateOnly? dateOfBirth = null)
        {
            AppUser user = await userManager.FindByNameAsync(userName);

            if (user is null)
            {
                user = new AppUser
                {
                    UserName = userName,
                    Email = email,
                    EmailConfirmed = true,
                    Name = name,
                    Surname = surname,
                    DateOfBirth = dateOfBirth
                };

                await userManager.CreateAsync(user, password);
            }

            // Deliberately outside the creation branch. Membership used to be granted only to
            // users this seed had just created, so adding a role name to the list changed
            // nothing on any database that had already been seeded — the rows would appear,
            // nobody would hold them, and the endpoints would go on answering 403.
            foreach (string roleName in roleNames)
            {
                await EnsureUserInRoleAsync(userManager, user, roleName);
            }
        }

        private static async Task EnsureUserInRoleAsync(
            UserManager<AppUser> userManager,
            AppUser user,
            string roleName)
        {
            if ((await userManager.IsInRoleAsync(user, roleName)) is false)
            {
                await userManager.AddToRoleAsync(user, roleName);
            }
        }
    }
}
