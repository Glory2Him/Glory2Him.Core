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
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Users;

namespace G2H.Security.Client.Clients.Users
{
    /// <summary>
    /// Provides helper methods for working with user information from a <see cref="ClaimsPrincipal"/>.
    /// This includes retrieving user details, roles, and claims.
    /// </summary>
    public interface IUserClient
    {
        /// <summary>
        /// Gets the <see cref="User"/> object associated with the given claims principal.
        /// </summary>
        /// <param name="claimsPrincipal">The user context containing claims.</param>
        /// <returns>The resolved <see cref="User"/> instance.</returns>
        /// <remarks>
        /// Typically extracts values such as <c>UserId</c>, <c>UserName</c>, and <c>Email</c> 
        /// from the claims to construct the <see cref="User"/> object.
        /// </remarks>
        /// <example>
        /// <code>
        /// User currentUser = await userClient.GetUserAsync(claimsPrincipal);
        /// Console.WriteLine(currentUser.UserId);   // e.g. "12345"
        /// Console.WriteLine(currentUser.Email);    // e.g. "alice@example.com"
        /// </code>
        /// </example>
        ValueTask<User> GetUserAsync(ClaimsPrincipal claimsPrincipal);

        /// <summary>
        /// Retrieves the current user identifier from the given claims principal.
        /// </summary>
        /// <param name="claimsPrincipal">The user context containing claims.</param>
        /// <returns>The user identifier string.</returns>
        /// <remarks>
        /// If no valid user identifier is found, a fallback (such as <c>"Anonymous"</c>) may be returned.
        /// </remarks>
        /// <example>
        /// <code>
        /// string userId = await userClient.GetUserIdAsync(claimsPrincipal);
        /// // e.g. "Alice" or "Anonymous"
        /// </code>
        /// </example>
        ValueTask<string> GetUserIdAsync(ClaimsPrincipal claimsPrincipal);

        /// <summary>
        /// Determines whether the user represented by the claims principal is authenticated.
        /// </summary>
        /// <param name="claimsPrincipal">The user context.</param>
        /// <returns><c>true</c> if the user is authenticated; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// This typically checks the <see cref="ClaimsPrincipal.Identity.IsAuthenticated"/> flag.
        /// </remarks>
        /// <example>
        /// <code>
        /// bool isAuthenticated = await userClient.IsUserAuthenticatedAsync(claimsPrincipal);
        /// if (isAuthenticated) { /* proceed */ }
        /// </code>
        /// </example>
        ValueTask<bool> IsUserAuthenticatedAsync(ClaimsPrincipal claimsPrincipal);

        /// <summary>
        /// Determines whether the user belongs to the specified role.
        /// </summary>
        /// <param name="claimsPrincipal">The user context.</param>
        /// <param name="roleName">The role name to check.</param>
        /// <returns><c>true</c> if the user is in the role; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// Typically looks for a role claim (e.g., <c>ClaimTypes.Role</c>).
        /// </remarks>
        /// <example>
        /// <code>
        /// bool isAdmin = await userClient.IsUserInRoleAsync(claimsPrincipal, "Admin");
        /// </code>
        /// </example>
        ValueTask<bool> IsUserInRoleAsync(ClaimsPrincipal claimsPrincipal, string roleName);

        /// <summary>
        /// Determines whether the user has a claim of the specified type and value.
        /// </summary>
        /// <param name="claimsPrincipal">The user context.</param>
        /// <param name="type">The claim type (e.g., "Permission").</param>
        /// <param name="value">The expected claim value.</param>
        /// <returns><c>true</c> if the claim exists; otherwise <c>false</c>.</returns>
        /// <example>
        /// <code>
        /// bool hasPermission = await userClient.UserHasClaimAsync(claimsPrincipal, "Permission", "Read");
        /// </code>
        /// </example>
        ValueTask<bool> UserHasClaimAsync(ClaimsPrincipal claimsPrincipal, string type, string value);

        /// <summary>
        /// Determines whether the user has at least one claim of the specified type.
        /// </summary>
        /// <param name="claimsPrincipal">The user context.</param>
        /// <param name="type">The claim type (e.g., "Permission").</param>
        /// <returns><c>true</c> if one or more claims of the type exist; otherwise <c>false</c>.</returns>
        /// <example>
        /// <code>
        /// bool hasAnyPermission = await userClient.UserHasClaimAsync(User, "Permission");
        /// </code>
        /// </example>
        ValueTask<bool> UserHasClaimAsync(ClaimsPrincipal claimsPrincipal, string type);

        /// <summary>
        /// Gets the first claim value of the specified type.
        /// </summary>
        /// <param name="claimsPrincipal">The user context.</param>
        /// <param name="type">The claim type (e.g., <c>ClaimTypes.Email</c>).</param>
        /// <returns>The first claim value as a string, or <c>null</c> if not found.</returns>
        /// <example>
        /// <code>
        /// string email = await userClient.GetUserClaimValueAsync(User, ClaimTypes.Email);
        /// </code>
        /// </example>
        ValueTask<string> GetUserClaimValueAsync(ClaimsPrincipal claimsPrincipal, string type);

        /// <summary>
        /// Gets all claim values of the specified type.
        /// </summary>
        /// <param name="claimsPrincipal">The user context.</param>
        /// <param name="type">The claim type (e.g., "Permission").</param>
        /// <returns>A read-only list of claim values. Returns an empty list if none exist.</returns>
        /// <example>
        /// <code>
        /// IReadOnlyList&lt;string&gt; roles = await userClient.GetUserClaimValuesAsync(User, ClaimTypes.Role);
        /// foreach (var role in roles)
        /// {
        ///     Console.WriteLine(role); // e.g. "Admin", "Manager"
        /// }
        /// </code>
        /// </example>
        ValueTask<IReadOnlyList<string>> GetUserClaimValuesAsync(ClaimsPrincipal claimsPrincipal, string type);
    }

}
