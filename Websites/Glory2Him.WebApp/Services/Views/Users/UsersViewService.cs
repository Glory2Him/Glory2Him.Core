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
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Brokers.Identities;
using Glory2Him.WebApp.Brokers.Loggings;
using Glory2Him.WebApp.Models.Foundations.Users;
using Glory2Him.WebApp.Models.Views.Users;
using Glory2Him.WebApp.Models.Views.Users.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace Glory2Him.WebApp.Services.Views.Users
{
    public partial class UsersViewService : IUsersViewService
    {
        public const string AdministratorsRole = "Administrators";

        // Two vocabularies, two surfaces, and both need protecting. "Administrators" opens
        // /api/admin; Core's Roles.Admin opens the moderation tier — hard delete, approve, and
        // removing another user's row. Guarding only the first let an administrator strip the
        // last holder of the second and silently restore the state issue #193 described, with
        // no warning and no route back but a re-seed.
        public static readonly string[] ProtectedAdministratorRoles =
            new[] { AdministratorsRole, Roles.Admin };

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
                    IdentityResult insertResult =
                        await this.identityBroker.InsertUserToRoleAsync(user, roleName);

                    EnsureIdentitySucceeded(insertResult, $"add the \"{roleName}\" role");

                    return;
                }

                if (ProtectedAdministratorRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
                {
                    IList<string> userRoles = await this.identityBroker.SelectUserRolesAsync(user);

                    await EnsureNotLastHolderAsync(
                        userRoles,
                        roleName,
                        action: $"removed from the \"{roleName}\" role");
                }

                IdentityResult deleteResult =
                    await this.identityBroker.DeleteUserFromRoleAsync(user, roleName);

                EnsureIdentitySucceeded(deleteResult, $"remove the \"{roleName}\" role");
            });

        public ValueTask DeleteUserAsync(Guid userId) =>
            TryCatch(async () =>
            {
                AppUser user = await RetrieveExistingUserAsync(userId);

                await EnsureNotLastAdministratorAsync(user, "deleted");

                IdentityResult deleteResult = await this.identityBroker.DeleteUserAsync(user);

                EnsureIdentitySucceeded(deleteResult, "delete this user");
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

            foreach (string administratorRole in ProtectedAdministratorRoles)
            {
                await EnsureNotLastHolderAsync(roles, administratorRole, action);
            }
        }

        private async ValueTask EnsureNotLastHolderAsync(
            IList<string> roles,
            string roleName,
            string action)
        {
            // Identity resolves role names through NormalizedName, so RemoveFromRoleAsync
            // succeeds for a differently-cased name. Matching ordinally here would let
            // "admin" strip the Admin role while the guard concluded the role was not
            // protected at all.
            if (!roles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            int usableHolders = await CountUsableHoldersAsync(roleName);

            if (usableHolders <= 1)
            {
                throw new UsersViewValidationException(
                    $"This is the last administrator holding the \"{roleName}\" role, so it "
                        + $"cannot be {action}. Give another account the \"{roleName}\" role first.");
            }
        }

        // An account that cannot sign in cannot administer the site, so it cannot be the reason
        // it is safe to demote somebody else. Counting rows rather than usable accounts let the
        // last real administrator be removed as long as a disabled or locked-out one still held
        // the role — and disabling a user deliberately leaves their role rows intact.
        private async ValueTask<int> CountUsableHoldersAsync(string roleName)
        {
            IList<AppUser> holders =
                await this.identityBroker.SelectUsersInRoleAsync(roleName);

            var usableHolders = 0;

            foreach (AppUser holder in holders)
            {
                if (holder.IsDisabled)
                {
                    continue;
                }

                if (await this.identityBroker.SelectIsLockedOutAsync(holder))
                {
                    continue;
                }

                usableHolders++;
            }

            return usableHolders;
        }

        // Identity reports failure by returning an unsuccessful result, not by throwing. Dropping
        // it made granting a role that does not exist answer 200 and do nothing — which is exactly
        // how the #193 vocabulary mismatch stayed invisible for as long as it did.
        private static void EnsureIdentitySucceeded(IdentityResult identityResult, string action)
        {
            if (identityResult.Succeeded)
            {
                return;
            }

            string reasons = string.Join(
                " ",
                identityResult.Errors.Select(identityError => identityError.Description));

            throw new UsersViewValidationException(
                $"Could not {action}. {reasons}".Trim());
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
