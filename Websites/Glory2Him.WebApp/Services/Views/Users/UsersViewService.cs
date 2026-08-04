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

using System.Security.Cryptography;
using Glory2Him.WebApp.Brokers.Identities;
using Glory2Him.WebApp.Brokers.Loggings;
using Glory2Him.WebApp.Models.Foundations.Users;
using Glory2Him.WebApp.Models.Views.Users;
using Glory2Him.WebApp.Models.Views.Users.Exceptions;

namespace Glory2Him.WebApp.Services.Views.Users
{
    public partial class UsersViewService : IUsersViewService
    {
        public const string AdministratorsRole = "Administrators";

        private readonly IIdentityBroker identityBroker;
        private readonly ILoggingBroker loggingBroker;

        public UsersViewService(
            IIdentityBroker identityBroker,
            ILoggingBroker loggingBroker)
        {
            this.identityBroker = identityBroker;
            this.loggingBroker = loggingBroker;
        }

        public ValueTask<List<UserView>> RetrieveAllUsersAsync() =>
            TryCatch(async () =>
            {
                List<AppUser> users =
                    this.identityBroker.SelectAllUsers()
                        .OrderBy(user => user.UserName)
                        .ToList();

                var userViews = new List<UserView>();

                foreach (AppUser user in users)
                {
                    userViews.Add(await AsUserViewAsync(user));
                }

                return userViews;
            });

        public ValueTask<UserView> RetrieveUserByIdAsync(Guid userId) =>
            TryCatch(async () =>
            {
                AppUser user = await RetrieveExistingUserAsync(userId);

                UserView userView = await AsUserViewAsync(user);
                userView.IsLockedOut = await this.identityBroker.SelectIsLockedOutAsync(user);

                return userView;
            });

        public ValueTask<List<string>> RetrieveAllRoleNamesAsync() =>
            TryCatch(() =>
            {
                List<string> roleNames =
                    this.identityBroker.SelectAllRoles()
                        .Select(role => role.Name)
                        .Where(name => name != null)
                        .Select(name => name!)
                        .OrderBy(name => name)
                        .ToList();

                return new ValueTask<List<string>>(roleNames);
            });

        public ValueTask ModifyUserAsync(UserView user) =>
            TryCatch(async () =>
            {
                AppUser existingUser = await RetrieveExistingUserAsync(user.Id);

                existingUser.Name = user.Name ?? string.Empty;
                existingUser.Surname = user.Surname ?? string.Empty;
                existingUser.PreferredName = user.PreferredName;
                existingUser.DateOfBirth = user.DateOfBirth;

                await this.identityBroker.UpdateUserAsync(existingUser);

                await this.identityBroker.SetUserNameAsync(existingUser, user.UserName);
                await this.identityBroker.SetEmailAsync(existingUser, user.Email);
                await this.identityBroker.SetPhoneNumberAsync(existingUser, user.PhoneNumber);
            });

        public ValueTask SetUserDisabledAsync(Guid userId, bool isDisabled) =>
            TryCatch(async () =>
            {
                AppUser user = await RetrieveExistingUserAsync(userId);

                if (isDisabled)
                {
                    await EnsureNotLastAdministratorAsync(user, "disabled");
                }

                user.IsDisabled = isDisabled;

                await this.identityBroker.UpdateUserAsync(user);

                // A disabled account is locked out indefinitely; enabling clears the lockout.
                await this.identityBroker.SetLockoutEnabledAsync(user, isDisabled);

                await this.identityBroker.SetLockoutEndDateAsync(
                    user,
                    isDisabled ? DateTimeOffset.MaxValue : null);
            });

        public ValueTask SetUserRoleAsync(Guid userId, string roleName, bool isInRole) =>
            TryCatch(async () =>
            {
                AppUser user = await RetrieveExistingUserAsync(userId);

                if (isInRole)
                {
                    await this.identityBroker.InsertUserToRoleAsync(user, roleName);

                    return;
                }

                if (roleName == AdministratorsRole)
                {
                    await EnsureNotLastAdministratorAsync(user, "removed from the administrators");
                }

                await this.identityBroker.DeleteUserFromRoleAsync(user, roleName);
            });

        public ValueTask DeleteUserAsync(Guid userId) =>
            TryCatch(async () =>
            {
                AppUser user = await RetrieveExistingUserAsync(userId);

                await EnsureNotLastAdministratorAsync(user, "deleted");

                await this.identityBroker.DeleteUserAsync(user);
            });

        public ValueTask ConfirmUserEmailAsync(Guid userId) =>
            TryCatch(async () =>
            {
                AppUser user = await RetrieveExistingUserAsync(userId);

                string token =
                    await this.identityBroker.GenerateEmailConfirmationTokenAsync(user);

                await this.identityBroker.ConfirmEmailAsync(user, token);
            });

        public ValueTask<string> GenerateEmailConfirmationTokenAsync(Guid userId) =>
            TryCatch(async () =>
            {
                AppUser user = await RetrieveExistingUserAsync(userId);

                return await this.identityBroker.GenerateEmailConfirmationTokenAsync(user);
            });

        public ValueTask<string> GeneratePasswordResetTokenAsync(Guid userId) =>
            TryCatch(async () =>
            {
                AppUser user = await RetrieveExistingUserAsync(userId);

                return await this.identityBroker.GeneratePasswordResetTokenAsync(user);
            });

        public ValueTask SetUserLockedOutAsync(Guid userId, bool isLockedOut) =>
            TryCatch(async () =>
            {
                AppUser user = await RetrieveExistingUserAsync(userId);

                if (isLockedOut)
                {
                    await EnsureNotLastAdministratorAsync(user, "locked out");

                    await this.identityBroker.SetLockoutEnabledAsync(user, true);

                    await this.identityBroker.SetLockoutEndDateAsync(
                        user, DateTimeOffset.MaxValue);

                    return;
                }

                await this.identityBroker.SetLockoutEndDateAsync(user, lockoutEnd: null);
            });

        public ValueTask ResetAccessFailedCountAsync(Guid userId) =>
            TryCatch(async () =>
            {
                AppUser user = await RetrieveExistingUserAsync(userId);

                await this.identityBroker.ResetAccessFailedCountAsync(user);
            });

        public ValueTask SetTwoFactorEnabledAsync(Guid userId, bool isEnabled) =>
            TryCatch(async () =>
            {
                AppUser user = await RetrieveExistingUserAsync(userId);

                await this.identityBroker.SetTwoFactorEnabledAsync(user, isEnabled);

                // Turning two-factor off leaves a stale authenticator key behind; clearing it means
                // re-enrolling starts from a fresh secret.
                if (isEnabled is false)
                {
                    await this.identityBroker.ResetAuthenticatorKeyAsync(user);
                }
            });

        // The id always comes from a rendered list or a route the admin followed, so a miss is a
        // stale link — a not-found to report, never a null to hand on to the next call.
        private async ValueTask<AppUser> RetrieveExistingUserAsync(Guid userId) =>
            await this.identityBroker.SelectUserByIdAsync(userId)
                ?? throw new UsersViewValidationException(
                    "That user no longer exists. It may have been deleted already.");

        // Locking, disabling, deleting or demoting the only administrator would leave nobody able
        // to administer the site, so each of those paths checks first.
        private async ValueTask EnsureNotLastAdministratorAsync(AppUser user, string action)
        {
            IList<string> roles = await this.identityBroker.SelectUserRolesAsync(user);

            if (!roles.Contains(AdministratorsRole))
            {
                return;
            }

            IList<AppUser> administrators =
                await this.identityBroker.SelectUsersInRoleAsync(AdministratorsRole);

            if (administrators.Count <= 1)
            {
                throw new UsersViewValidationException(
                    $"This is the last administrator, so it cannot be {action}. "
                        + "Give another account the administrators role first.");
            }
        }

        private async ValueTask<UserView> AsUserViewAsync(AppUser user)
        {
            IList<string> roles =
                await this.identityBroker.SelectUserRolesAsync(user);

            return new UserView
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Name = user.Name,
                Surname = user.Surname,
                PreferredName = user.PreferredName,
                DateOfBirth = user.DateOfBirth,
                EmailConfirmed = user.EmailConfirmed,
                AccessFailedCount = user.AccessFailedCount,
                TwoFactorEnabled = user.TwoFactorEnabled,
                IsDisabled = user.IsDisabled,
                Roles = roles.ToList(),
                HasProfileImage = user.ProfileImage is { Length: > 0 },
                ImageVersion = ComputeImageVersion(user.ProfileImage),
            };
        }

        private static string? ComputeImageVersion(byte[]? bytes)
        {
            if (bytes is null || bytes.Length == 0)
            {
                return null;
            }

            byte[] hash = SHA256.HashData(bytes);

            return Convert.ToHexString(hash, 0, 4).ToLowerInvariant();
        }
    }
}
