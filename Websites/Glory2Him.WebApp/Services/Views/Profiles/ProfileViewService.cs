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
using Glory2Him.WebApp.Brokers.Images;
using Glory2Him.WebApp.Brokers.Loggings;
using Glory2Him.WebApp.Brokers.Profiles;
using Glory2Him.WebApp.Models.Foundations.Users;
using Glory2Him.WebApp.Models.Views.Profiles;
using Glory2Him.WebApp.Models.Views.Profiles.Exceptions;

namespace Glory2Him.WebApp.Services.Views.Profiles
{
    public partial class ProfileViewService : IProfileViewService
    {
        // Uploads above this are rejected before any processing (generous 5 MB ceiling).
        public const long MaxUploadBytes = 5 * 1024 * 1024;

        // Avatars are stored at 256x256 — sharp on standard and most high-DPI displays while
        // keeping the encoded WebP tiny.
        private const int AvatarSize = 256;

        private readonly IProfileImageBroker profileImageBroker;
        private readonly IImageProcessingBroker imageProcessingBroker;
        private readonly ILoggingBroker loggingBroker;

        public ProfileViewService(
            IProfileImageBroker profileImageBroker,
            IImageProcessingBroker imageProcessingBroker,
            ILoggingBroker loggingBroker)
        {
            this.profileImageBroker = profileImageBroker;
            this.imageProcessingBroker = imageProcessingBroker;
            this.loggingBroker = loggingBroker;
        }

        public ValueTask<ProfileView> RetrieveProfileByIdAsync(Guid userId) =>
            TryCatch(async () =>
            {
                AppUser? user = await this.profileImageBroker.SelectUserByIdAsync(userId);

                return AsProfileView(user);
            });

        public ValueTask SetProfileImageAsync(
            Guid userId,
            Stream imageStream,
            long byteLength,
            string contentType) =>
            TryCatch(async () =>
            {
                ValidateUpload(byteLength, contentType);

                ProcessedImage avatar =
                    await this.imageProcessingBroker.CreateSquareAvatarAsync(
                        imageStream, AvatarSize);

                await this.profileImageBroker.UpdateProfileImageAsync(
                    userId, avatar.Bytes, avatar.ContentType);
            });

        public ValueTask RemoveProfileImageAsync(Guid userId) =>
            TryCatch(async () =>
            {
                await this.profileImageBroker.UpdateProfileImageAsync(
                    userId, imageBytes: null, contentType: null);
            });

        public ValueTask<ProcessedImage?> RetrieveProfileImageAsync(Guid userId) =>
            TryCatch(async () =>
            {
                AppUser? user = await this.profileImageBroker.SelectUserByIdAsync(userId);

                if (user?.ProfileImage is null || user.ProfileImage.Length == 0)
                {
                    return (ProcessedImage?)null;
                }

                return new ProcessedImage(
                    user.ProfileImage,
                    user.ProfileImageContentType ?? "image/webp");
            });

        private static void ValidateUpload(long byteLength, string contentType)
        {
            if (byteLength <= 0)
            {
                throw new ProfileViewValidationException("Please choose a file to upload.");
            }

            if (byteLength > MaxUploadBytes)
            {
                throw new ProfileViewValidationException(
                    "The image is too large. Please choose a file up to 5 MB.");
            }

            if (string.IsNullOrWhiteSpace(contentType)
                || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                throw new ProfileViewValidationException(
                    "That file is not an image. Please choose a PNG, JPEG, or WebP image.");
            }
        }

        private static ProfileView AsProfileView(AppUser? user)
        {
            if (user is null)
            {
                return new ProfileView();
            }

            return new ProfileView
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                HasProfileImage = user.ProfileImage is { Length: > 0 },
                ImageVersion = ComputeVersion(user.ProfileImage),
            };
        }

        private static string? ComputeVersion(byte[]? bytes)
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
