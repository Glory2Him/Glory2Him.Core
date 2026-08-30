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
using G2H.Security.Client.Clients;
using G2H.Security.Client.Models.Foundations.Users;
using Tynamix.ObjectFiller;

namespace G2H.Security.Client.Tests.Unit.Clients.Users
{
    public partial class UserClientTests
    {
        private readonly ISecurityClient securityClient;

        public UserClientTests()
        {
            this.securityClient = new SecurityClient();
        }


        private static ClaimsPrincipal CreateRandomClaimsPrincipal(bool isAuthenticated = true)
        {
            Guid securityOid = Guid.NewGuid();
            string givenName = GetRandomString();
            string surname = GetRandomString();
            string displayName = GetRandomString();
            string email = GetRandomString();
            string jobTitle = GetRandomString();

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, securityOid.ToString()),
                new Claim(ClaimTypes.GivenName, GetRandomString()),
                new Claim(ClaimTypes.Surname, GetRandomString()),
                new Claim("displayName", GetRandomString()),
                new Claim(ClaimTypes.Email, GetRandomString()),
                new Claim("jobTitle", GetRandomString()),
                new Claim(ClaimTypes.Name, GetRandomString()),
                new Claim(ClaimTypes.Role, "Users")
            };

            string? authenticationType = isAuthenticated ? "TestScheme" : null;
            var identity = new ClaimsIdentity(claims, authenticationType);
            var principal = new ClaimsPrincipal(identity);

            return principal;
        }

        private User GetUser(ClaimsPrincipal claimsPrincipal)
        {
            return new User(
            userId: claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value!,
            userName: claimsPrincipal.FindFirst(ClaimTypes.Name)?.Value!,
            givenName: claimsPrincipal.FindFirst(ClaimTypes.GivenName)?.Value!,
            surname: claimsPrincipal.FindFirst(ClaimTypes.Surname)?.Value!,
            displayName: claimsPrincipal.FindFirst("displayName")?.Value!,
            email: claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value!,
            jobTitle: claimsPrincipal.FindFirst("jobTitle")?.Value!,
            roles: claimsPrincipal.FindAll(ClaimTypes.Role).Select(role => role.Value).ToList(),
            claims: claimsPrincipal.Claims.ToList());
        }

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();
    }
}
