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
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Glory2Him.WebApp.Models.Foundations.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Glory2Him.WebApp.Tests.Acceptance
{
    /// <summary>
    /// Stands in for the portal's Identity cookie.
    ///
    /// <para><b>Default caller.</b> With no override headers the request is the seeded
    /// administrator, and the roles are read from that account's REAL Identity membership rather
    /// than hand-written — so the happy-path suite proves what a synthetic claim cannot: that a
    /// real signed-in administrator holds the names Core's gates test for.</para>
    ///
    /// <para><b>Overrides.</b> A gate is only proven by the callers it turns away, and those
    /// callers cannot be expressed by one fixed principal. The headers below let a test act as
    /// nobody, as an ordinary contributor with no roles, or as a specific user id — which is what
    /// makes "not the owner" and "not an Admin" testable over real HTTP. They are read only here,
    /// in the test host; the production pipeline has no such path.</para>
    /// </summary>
    public class TestAuthHandler : AuthenticationHandler<CustomAuthenticationSchemeOptions>
    {
        public const string SeededAdministratorUserName = "admin";
        public const string AnonymousHeader = "X-Test-Anonymous";
        public const string UserIdHeader = "X-Test-UserId";
        public const string RolesHeader = "X-Test-Roles";

        // The default identity is FIXED: the tag audit trail keys ownership off it, so every
        // happy-path test's CreatedBy depends on it not moving.
        public static readonly Guid DefaultUserId =
            Guid.Parse("9f2c5a41-6b3d-4e18-9a77-0c4f1de8b512");

        private static readonly string givenName = "TestGivenName";
        private static readonly string surname = "TestSurname";
        private static readonly string displayName = "TestDisplayName";
        private static readonly string email = "TestEmail@test.com";

        private readonly UserManager<AppUser> userManager;

        public TestAuthHandler(
            IOptionsMonitor<CustomAuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            UserManager<AppUser> userManager)
            : base(options, logger, encoder) => this.userManager = userManager;

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Request.Headers.ContainsKey(AnonymousHeader))
            {
                // No identity at all, so [Authorize] challenges and [AllowAnonymous] proceeds —
                // which is precisely the distinction the read endpoints need to prove.
                return AuthenticateResult.NoResult();
            }

            string userId = Request.Headers.TryGetValue(UserIdHeader, out var userIdValues)
                ? userIdValues.ToString()
                : DefaultUserId.ToString();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.GivenName, givenName),
                new Claim(ClaimTypes.Surname, surname),
                new Claim("displayName", displayName),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, "TestUser")
            };

            foreach (string roleName in await ResolveRoleNamesAsync())
            {
                claims.Add(new Claim(ClaimTypes.Role, roleName));
            }

            var identity = new ClaimsIdentity(claims, "TestScheme");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestScheme");

            return AuthenticateResult.Success(ticket);
        }

        private async ValueTask<IList<string>> ResolveRoleNamesAsync()
        {
            // The USER ID header is the sentinel for "the test named its own caller", not the
            // roles header. An HttpClient drops a header whose value is empty, so a caller
            // holding no roles at all would otherwise be indistinguishable from a caller who
            // supplied nothing — and would silently fall back to the seeded administrator,
            // turning every negative security test into a false pass.
            if (Request.Headers.ContainsKey(UserIdHeader))
            {
                bool hasRoles = Request.Headers.TryGetValue(RolesHeader, out var roleValues);

                return hasRoles is false
                    ? new List<string>()
                    : roleValues.ToString()
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(roleName => roleName.Trim())
                        .Where(roleName => string.IsNullOrEmpty(roleName) is false)
                        .ToList();
            }

            AppUser seededAdministrator =
                await this.userManager.FindByNameAsync(SeededAdministratorUserName);

            return seededAdministrator is null
                ? new List<string>()
                : await this.userManager.GetRolesAsync(seededAdministrator);
        }
    }
}
