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

using Glory2Him.WebApp.Models.Foundations.Roles;
using Glory2Him.WebApp.Models.Foundations.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Glory2Him.WebApp.Data
{
    // Design-time factory so `dotnet ef migrations` can construct the context without the
    // application's runtime DI wiring (keeps the DATA migration independent of EXPOSERS).
    // It mirrors the runtime Identity configuration — crucially SchemaVersion Version3 — so the
    // generated migration includes the passkey schema, matching what the app maps at runtime.
    public class SecurityDbContextFactory : IDesignTimeDbContextFactory<SecurityDbContext>
    {
        public SecurityDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration =
                new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

            string connectionString =
                configuration.GetConnectionString("Glory2HimSecurityConnection")!;

            var services = new ServiceCollection();

            services.AddDbContext<SecurityDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddIdentityCore<AppUser>(options =>
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3)
                    .AddRoles<AppRole>()
                    .AddEntityFrameworkStores<SecurityDbContext>();

            ServiceProvider provider = services.BuildServiceProvider();

            return provider.GetRequiredService<SecurityDbContext>();
        }
    }
}
