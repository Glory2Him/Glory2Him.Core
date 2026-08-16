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

using System.Collections.Generic;
using System.Threading.Tasks;
using Glory2Him.WebApp.Models.Foundations.Roles;
using Glory2Him.WebApp.Models.Foundations.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker
    {
        public async ValueTask<bool> RoleExistsAsync(string roleName)
        {
            using IServiceScope scope = this.webApplicationFactory.Services.CreateScope();

            RoleManager<AppRole> roleManager =
                scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

            return await roleManager.RoleExistsAsync(roleName);
        }

        public async ValueTask<IList<string>> GetSeededAdministratorRolesAsync()
        {
            using IServiceScope scope = this.webApplicationFactory.Services.CreateScope();

            UserManager<AppUser> userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            AppUser administrator =
                await userManager.FindByNameAsync(TestAuthHandler.SeededAdministratorUserName);

            return await userManager.GetRolesAsync(administrator);
        }
    }
}
