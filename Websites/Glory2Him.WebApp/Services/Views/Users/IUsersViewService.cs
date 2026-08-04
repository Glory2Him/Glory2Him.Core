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

using Glory2Him.WebApp.Models.Views.Users;

namespace Glory2Him.WebApp.Services.Views.Users
{
    public interface IUsersViewService
    {
        ValueTask<List<UserView>> RetrieveAllUsersAsync();
        ValueTask<UserView> RetrieveUserByIdAsync(Guid userId);
        ValueTask<List<string>> RetrieveAllRoleNamesAsync();
        ValueTask ModifyUserAsync(UserView user);
        ValueTask SetUserDisabledAsync(Guid userId, bool isDisabled);
        ValueTask SetUserRoleAsync(Guid userId, string roleName, bool isInRole);
        ValueTask DeleteUserAsync(Guid userId);

        ValueTask ConfirmUserEmailAsync(Guid userId);
        ValueTask<string> GenerateEmailConfirmationTokenAsync(Guid userId);
        ValueTask<string> GeneratePasswordResetTokenAsync(Guid userId);
        ValueTask SetUserLockedOutAsync(Guid userId, bool isLockedOut);
        ValueTask ResetAccessFailedCountAsync(Guid userId);
        ValueTask SetTwoFactorEnabledAsync(Guid userId, bool isEnabled);
    }
}
