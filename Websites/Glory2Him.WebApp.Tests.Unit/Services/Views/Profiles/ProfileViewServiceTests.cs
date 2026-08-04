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

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.WebApp.Brokers.Images;
using Glory2Him.WebApp.Brokers.Loggings;
using Glory2Him.WebApp.Brokers.Profiles;
using Glory2Him.WebApp.Models.Foundations.Users;
using Glory2Him.WebApp.Models.Views.Profiles;
using Glory2Him.WebApp.Models.Views.Profiles.Exceptions;
using Glory2Him.WebApp.Services.Views.Profiles;
using Moq;
using SkiaSharp;

namespace Glory2Him.WebApp.Tests.Unit.Services.Views.Profiles
{
    public class ProfileViewServiceTests
    {
        private readonly Mock<IProfileImageBroker> profileImageBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IProfileViewService profileViewService;

        public ProfileViewServiceTests()
        {
            this.profileImageBrokerMock = new Mock<IProfileImageBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            // A real image broker so the full processing path is exercised.
            this.profileViewService = new ProfileViewService(
                profileImageBroker: this.profileImageBrokerMock.Object,
                imageProcessingBroker: new ImageProcessingBroker(),
                loggingBroker: this.loggingBrokerMock.Object);
        }

        private static Stream CreatePngStream(int width, int height)
        {
            var info = new SKImageInfo(width, height);
            using var surface = SKSurface.Create(info);
            surface.Canvas.Clear(SKColors.CornflowerBlue);

            using SKImage image = surface.Snapshot();
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

            return new MemoryStream(data.ToArray());
        }

        [Fact]
        public async Task ShouldResizePersistAndServeProfileImage()
        {
            // given
            Guid userId = Guid.NewGuid();
            byte[] persistedBytes = null;
            string persistedContentType = null;

            this.profileImageBrokerMock.Setup(broker =>
                broker.UpdateProfileImageAsync(userId, It.IsAny<byte[]>(), It.IsAny<string>()))
                    .Callback<Guid, byte[], string>((_, bytes, contentType) =>
                    {
                        persistedBytes = bytes;
                        persistedContentType = contentType;
                    })
                    .Returns(ValueTask.CompletedTask);

            using Stream png = CreatePngStream(400, 300);

            // when
            await this.profileViewService.SetProfileImageAsync(
                userId, png, png.Length, "image/png");

            // then (persisted as a WebP)
            persistedBytes.Should().NotBeNullOrEmpty();
            persistedContentType.Should().Be("image/webp");

            this.profileImageBrokerMock.Verify(broker =>
                broker.UpdateProfileImageAsync(userId, It.IsAny<byte[]>(), "image/webp"),
                    Times.Once);

            // and served back
            this.profileImageBrokerMock.Setup(broker =>
                broker.SelectUserByIdAsync(userId))
                    .ReturnsAsync(new AppUser
                    {
                        Id = userId,
                        ProfileImage = persistedBytes,
                        ProfileImageContentType = persistedContentType,
                    });

            ProcessedImage served =
                await this.profileViewService.RetrieveProfileImageAsync(userId);

            served.Should().NotBeNull();
            served!.ContentType.Should().Be("image/webp");
            served.Bytes.Should().BeEquivalentTo(persistedBytes);
        }

        [Fact]
        public async Task ShouldRejectUploadOverMaxSize()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };
            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            long tooLarge = ProfileViewService.MaxUploadBytes + 1;

            // when
            Func<Task> setTask = async () =>
                await this.profileViewService.SetProfileImageAsync(
                    user.Id, stream, tooLarge, "image/png");

            // then (rejected before any processing/persistence)
            await setTask.Should().ThrowAsync<ProfileViewValidationException>();

            this.profileImageBrokerMock.Verify(broker =>
                broker.UpdateProfileImageAsync(
                    It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>()),
                        Times.Never);
        }

        [Fact]
        public async Task ShouldRejectNonImageContentType()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };
            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

            // when
            Func<Task> setTask = async () =>
                await this.profileViewService.SetProfileImageAsync(
                    user.Id, stream, stream.Length, "application/pdf");

            // then
            await setTask.Should().ThrowAsync<ProfileViewValidationException>();
        }

        [Fact]
        public async Task ShouldRemoveProfileImage()
        {
            // given
            Guid userId = Guid.NewGuid();

            this.profileImageBrokerMock.Setup(broker =>
                broker.UpdateProfileImageAsync(userId, null, null))
                    .Returns(ValueTask.CompletedTask);

            // when
            await this.profileViewService.RemoveProfileImageAsync(userId);

            // then (cleared by passing null bytes/content type to the broker)
            this.profileImageBrokerMock.Verify(broker =>
                broker.UpdateProfileImageAsync(userId, null, null),
                    Times.Once);
        }

        [Fact]
        public async Task ShouldReturnNullWhenServingUserWithoutImage()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };

            this.profileImageBrokerMock.Setup(broker =>
                broker.SelectUserByIdAsync(user.Id))
                    .ReturnsAsync(user);

            // when
            ProcessedImage served =
                await this.profileViewService.RetrieveProfileImageAsync(user.Id);

            // then
            served.Should().BeNull();
        }

        [Fact]
        public async Task ShouldExposeImageUrlAndVersionWhenImagePresent()
        {
            // given
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = "someone",
                ProfileImage = new byte[] { 1, 2, 3, 4 },
                ProfileImageContentType = "image/webp",
            };

            this.profileImageBrokerMock.Setup(broker =>
                broker.SelectUserByIdAsync(user.Id))
                    .ReturnsAsync(user);

            // when
            ProfileView profile =
                await this.profileViewService.RetrieveProfileByIdAsync(user.Id);

            // then
            profile.HasProfileImage.Should().BeTrue();
            profile.ImageVersion.Should().NotBeNullOrEmpty();
            profile.ImageUrl.Should().Be($"Profile-Image/{user.Id}?v={profile.ImageVersion}");
        }
    }
}
