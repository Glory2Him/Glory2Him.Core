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
using System.Security.Claims;

namespace G2H.EventEnvelope.Client.Models.Securities
{
    public class EventUser
    {
        public EventUser(
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
