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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Users;
using G2H.Security.Client.Models.Foundations.Users.Exceptions;

namespace G2H.Security.Client.Services.Foundations.Users
{
    internal partial class UserService : IUserService
    {
        public ValueTask<User> GetUserAsync(ClaimsPrincipal claimsPrincipal) =>
        TryCatch(async () =>
        {
            ValidateOnGetUser(claimsPrincipal);

            return GetUserFromClaimsPrincipal(claimsPrincipal);
        });

        public ValueTask<string> GetUserIdAsync(ClaimsPrincipal claimsPrincipal) =>
        TryCatch(async () =>
        {
            ValidateOnGetUserId(claimsPrincipal);
            var user = GetUserFromClaimsPrincipal(claimsPrincipal);
            var isAuthenticated = claimsPrincipal.Identity?.IsAuthenticated ?? false;

            string userId = isAuthenticated
                ? user.UserId
                : string.IsNullOrEmpty(user.UserId)
                    ? "anonymous" : user.UserId;

            return userId;
        });

        public ValueTask<bool> UserHasClaimAsync(
            ClaimsPrincipal claimsPrincipal,
            string claimType,
            string claimValue) =>
        TryCatch(async () =>
        {
            ValidateOnUserHasClaimType(claimsPrincipal, claimType, claimValue);

            return claimsPrincipal.HasClaim(claimType, claimValue);
        });

        public ValueTask<bool> UserHasClaimAsync(ClaimsPrincipal claimsPrincipal, string claimType) =>
        TryCatch(async () =>
        {
            ValidateOnUserHasClaimType(claimsPrincipal, claimType);

            return claimsPrincipal.FindFirst(claimType) != null;
        });

        public ValueTask<bool> IsUserAuthenticatedAsync(ClaimsPrincipal claimsPrincipal) =>
        TryCatch(async () =>
        {
            ValidateOnIsUserAuthenticated(claimsPrincipal);

            return claimsPrincipal.Identity?.IsAuthenticated ?? false;
        });

        public ValueTask<bool> IsUserInRoleAsync(ClaimsPrincipal claimsPrincipal, string roleName) =>
        TryCatch(async () =>
        {
            ValidateOnIsUserInRole(claimsPrincipal, roleName);
            var roles = claimsPrincipal.FindAll(ClaimTypes.Role).Select(role => role.Value);

            return roles.Contains(roleName);
        });

        public ValueTask<string> GetUserClaimValueAsync(ClaimsPrincipal claimsPrincipal, string type) =>
        TryCatch(async () =>
        {
            ValidateOnGetUserClaimValue(claimsPrincipal, type);

            var claim = claimsPrincipal.FindFirst(type);

            if (claim is null)
            {
                throw new ClaimNotFoundUserException($"Claim with type '{type}' not found.");
            }

            return claim.Value;
        });

        public ValueTask<IReadOnlyList<string>> GetUserClaimValuesAsync(ClaimsPrincipal claimsPrincipal, string type) =>
        TryCatch<IReadOnlyList<string>>(async () =>
        {
            ValidateOnGetUserClaimValue(claimsPrincipal, type);

            var values = claimsPrincipal.FindAll(type)
                .Select(c => c.Value)
                .ToArray();

            if (values.Count() == 0)
            {
                throw new ClaimNotFoundUserException($"Claim with type '{type}' not found.");
            }

            return values;
        });

        private static User GetUserFromClaimsPrincipal(ClaimsPrincipal claimsPrincipal)
        {
            var userIdString = claimsPrincipal.FindFirst("oid")?.Value
                ?? claimsPrincipal
                    .FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value

                ?? claimsPrincipal
                    .FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value

                ?? claimsPrincipal
                    .FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;

            var userId = userIdString;
            var givenName = claimsPrincipal.FindFirst(ClaimTypes.GivenName)?.Value;
            var surname = claimsPrincipal.FindFirst(ClaimTypes.Surname)?.Value;
            var displayName = claimsPrincipal.FindFirst("displayName")?.Value;
            var email = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value;
            var jobTitle = claimsPrincipal.FindFirst("jobTitle")?.Value;
            var roles = claimsPrincipal.FindAll(ClaimTypes.Role).Select(role => role.Value).ToList();
            var claimsList = claimsPrincipal.Claims;

            return new User(
                userId: userId!,
                givenName: givenName!,
                surname: surname!,
                displayName: displayName!,
                email: email!,
                jobTitle: jobTitle!,
                roles: roles,
                claims: claimsList);
        }
    }
}
