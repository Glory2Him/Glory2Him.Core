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
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Users;

namespace G2H.Security.Client.Services.Foundations.Users
{
    internal interface IUserService
    {
        ValueTask<User> GetUserAsync(ClaimsPrincipal claimsPrincipal);
        ValueTask<string> GetUserIdAsync(ClaimsPrincipal claimsPrincipal);
        ValueTask<bool> IsUserAuthenticatedAsync(ClaimsPrincipal claimsPrincipal);
        ValueTask<bool> IsUserInRoleAsync(ClaimsPrincipal claimsPrincipal, string roleName);
        ValueTask<bool> UserHasClaimAsync(ClaimsPrincipal claimsPrincipal, string claimType, string claimValue);
        ValueTask<bool> UserHasClaimAsync(ClaimsPrincipal claimsPrincipal, string claimType);
        ValueTask<string> GetUserClaimValueAsync(ClaimsPrincipal claimsPrincipal, string type);
        ValueTask<IReadOnlyList<string>> GetUserClaimValuesAsync(ClaimsPrincipal claimsPrincipal, string type);
    }
}
