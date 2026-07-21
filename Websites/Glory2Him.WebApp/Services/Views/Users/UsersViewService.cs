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

namespace Glory2Him.WebApp.Services.Views.Users
{
    public partial class UsersViewService : IUsersViewService
    {
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
                AppUser user = await this.identityBroker.SelectUserByIdAsync(userId);

                return await AsUserViewAsync(user);
            });

        public ValueTask SetUserDisabledAsync(Guid userId, bool isDisabled) =>
            TryCatch(async () =>
            {
                AppUser user = await this.identityBroker.SelectUserByIdAsync(userId);
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
                AppUser user = await this.identityBroker.SelectUserByIdAsync(userId);

                if (isInRole)
                {
                    await this.identityBroker.InsertUserToRoleAsync(user, roleName);
                }
                else
                {
                    await this.identityBroker.DeleteUserFromRoleAsync(user, roleName);
                }
            });

        public ValueTask DeleteUserAsync(Guid userId) =>
            TryCatch(async () =>
            {
                AppUser user = await this.identityBroker.SelectUserByIdAsync(userId);

                await this.identityBroker.DeleteUserAsync(user);
            });

        private async ValueTask<UserView> AsUserViewAsync(AppUser user)
        {
            IList<string> roles =
                await this.identityBroker.SelectUserRolesAsync(user);

            return new UserView
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
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
