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

            await EnsureUserAsync(
                userManager,
                userName: "admin",
                password: "admin",
                roleName: AdministratorsRole,
                email: "admin@g2h.org",
                name: "Admin",
                surname: "User");

            await EnsureUserAsync(
                userManager,
                userName: "user",
                password: "user",
                roleName: UsersRole,
                email: "user@g2h.org",
                name: "Normal",
                surname: "User");

            await EnsureUserAsync(
                userManager,
                userName: "cjdutoit",
                password: "P@ssword!",
                roleName: AdministratorsRole,
                email: "christo@dutoit.co.uk",
                name: "Christo",
                surname: "du Toit",
                dateOfBirth: new DateOnly(1977, 10, 8));
        }

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
            string roleName,
            string email,
            string name,
            string surname,
            DateOnly? dateOfBirth = null)
        {
            AppUser existingUser = await userManager.FindByNameAsync(userName);

            if (existingUser is null)
            {
                var newUser = new AppUser
                {
                    UserName = userName,
                    Email = email,
                    EmailConfirmed = true,
                    Name = name,
                    Surname = surname,
                    DateOfBirth = dateOfBirth
                };

                await userManager.CreateAsync(newUser, password);
                await userManager.AddToRoleAsync(newUser, roleName);
            }
        }
    }
}
