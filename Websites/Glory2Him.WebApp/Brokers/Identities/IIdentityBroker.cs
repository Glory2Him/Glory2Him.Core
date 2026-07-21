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

namespace Glory2Him.WebApp.Brokers.Identities
{
    // ASP.NET Core Identity (UserManager/RoleManager) is an external component, so it is
    // wrapped by this broker. Services depend on the broker, never on the managers directly.
    public interface IIdentityBroker
    {
        IQueryable<AppUser> SelectAllUsers();

        ValueTask<AppUser> SelectUserByIdAsync(Guid userId);

        ValueTask<IdentityResult> InsertUserAsync(AppUser user, string password);

        ValueTask<IdentityResult> DeleteUserAsync(AppUser user);

        ValueTask<IList<string>> SelectUserRolesAsync(AppUser user);

        ValueTask<IdentityResult> InsertUserToRoleAsync(AppUser user, string roleName);

        ValueTask<IdentityResult> DeleteUserFromRoleAsync(AppUser user, string roleName);

        ValueTask<IList<AppUser>> SelectUsersInRoleAsync(string roleName);

        ValueTask<bool> SelectIsLockedOutAsync(AppUser user);

        ValueTask<IdentityResult> UpdateUserAsync(AppUser user);

        ValueTask<IdentityResult> SetUserNameAsync(AppUser user, string userName);

        ValueTask<IdentityResult> SetEmailAsync(AppUser user, string email);

        ValueTask<IdentityResult> SetPhoneNumberAsync(AppUser user, string phoneNumber);

        ValueTask<string> GenerateEmailConfirmationTokenAsync(AppUser user);

        ValueTask<IdentityResult> ConfirmEmailAsync(AppUser user, string token);

        ValueTask<string> GeneratePasswordResetTokenAsync(AppUser user);

        ValueTask<IdentityResult> SetLockoutEnabledAsync(AppUser user, bool enabled);

        ValueTask<IdentityResult> SetLockoutEndDateAsync(
            AppUser user,
            DateTimeOffset? lockoutEnd);

        ValueTask<IdentityResult> ResetAccessFailedCountAsync(AppUser user);

        ValueTask<IdentityResult> SetTwoFactorEnabledAsync(AppUser user, bool enabled);

        ValueTask<IdentityResult> ResetAuthenticatorKeyAsync(AppUser user);

        IQueryable<AppRole> SelectAllRoles();
    }
}
