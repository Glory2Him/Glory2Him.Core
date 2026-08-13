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
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Securities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Glory2Him.WebApp.Tests.Acceptance
{
    public class TestAuthHandler : AuthenticationHandler<CustomAuthenticationSchemeOptions>
    {
        private static readonly Guid securityOid = Guid.Parse("9f2c5a41-6b3d-4e18-9a77-0c4f1de8b512");
        private static readonly string givenName = "TestGivenName";
        private static readonly string surname = "TestSurname";
        private static readonly string displayName = "TestDisplayName";
        private static readonly string email = "TestEmail@test.com";

        private static List<Claim> claims = new List<Claim>
        {
            new Claim("oid", securityOid.ToString()),
            new Claim(ClaimTypes.NameIdentifier, securityOid.ToString()),
            new Claim(ClaimTypes.GivenName, givenName),
            new Claim(ClaimTypes.Surname, surname),
            new Claim("displayName", displayName),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim(ClaimTypes.Role, Roles.Admin)
        };

        public TestAuthHandler(
            IOptionsMonitor<CustomAuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(claims, "TestScheme");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestScheme");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
