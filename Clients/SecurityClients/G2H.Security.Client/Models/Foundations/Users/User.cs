// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
// ────────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using System.Security.Claims;

namespace G2H.Security.Client.Models.Foundations.Users
{
    public class User
    {
        public User(
            string userId,
            string givenName,
            string surname,
            string displayName,
            string email,
            string jobTitle,
            IEnumerable<string> roles,
            IEnumerable<Claim> claims)
        {
            UserId = userId;
            GivenName = givenName;
            Surname = surname;
            DisplayName = displayName;
            Email = email;
            JobTitle = jobTitle;
            Roles = roles;
            Claims = claims;
        }

        public string UserId { get; private set; } = string.Empty;
        public string GivenName { get; private set; } = string.Empty;
        public string Surname { get; private set; } = string.Empty;
        public string DisplayName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string JobTitle { get; private set; } = string.Empty;
        public IEnumerable<string> Roles { get; private set; }
        public IEnumerable<Claim> Claims { get; private set; }
    }
}
