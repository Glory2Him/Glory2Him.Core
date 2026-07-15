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
using System.IO;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.Account.Shared;
using Glory2Him.WebApp.Models.Views.Profiles;
using Glory2Him.WebApp.Models.Views.Profiles.Exceptions;
using Glory2Him.WebApp.Services.Views.Profiles;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Glory2Him.WebApp.Tests.Unit.Components.Account.Shared
{
    public class ProfileImageManagerComponentTests : BunitContext
    {
        private readonly Mock<IProfileViewService> profileViewServiceMock;
        private readonly Guid userId = Guid.NewGuid();

        public ProfileImageManagerComponentTests()
        {
            this.profileViewServiceMock = new Mock<IProfileViewService>();
            Services.AddSingleton(this.profileViewServiceMock.Object);
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private IRenderedComponent<ProfileImageManager> RenderManager(bool hasImage)
        {
            this.profileViewServiceMock.Setup(service =>
                service.RetrieveProfileByIdAsync(this.userId))
                    .ReturnsAsync(new ProfileView
                    {
                        Id = this.userId,
                        UserName = "Admin",
                        HasProfileImage = hasImage,
                        ImageVersion = hasImage ? "abcd" : null,
                    });

            return Render<ProfileImageManager>(parameters => parameters
                .Add(manager => manager.UserId, this.userId)
                .Add(manager => manager.Name, "Admin"));
        }

        [Fact]
        public void ShouldShowUploadButtonAndInitialsWhenNoImage()
        {
            // given . when
            IRenderedComponent<ProfileImageManager> renderedManager = RenderManager(hasImage: false);

            // then (initials avatar, upload present, no remove)
            renderedManager.Markup.Should().Contain("Upload image");
            renderedManager.FindAll("img").Should().BeEmpty();
            renderedManager.Markup.Should().NotContain("Remove");
        }

        [Fact]
        public void ShouldShowRemoveWhenImagePresent()
        {
            // given . when
            IRenderedComponent<ProfileImageManager> renderedManager = RenderManager(hasImage: true);

            // then
            renderedManager.Markup.Should().Contain("Remove");
            renderedManager.Find("img.avatar-img").GetAttribute("src")
                .Should().Contain($"profile-image/{this.userId}");
        }

        [Fact]
        public void ShouldCallServiceWhenFileUploaded()
        {
            // given
            IRenderedComponent<ProfileImageManager> renderedManager = RenderManager(hasImage: false);

            this.profileViewServiceMock.Setup(service =>
                service.SetProfileImageAsync(
                    this.userId, It.IsAny<Stream>(), It.IsAny<long>(), "image/png"))
                        .Returns(ValueTask.CompletedTask);

            var file = InputFileContent.CreateFromBinary(
                new byte[] { 1, 2, 3, 4 }, "avatar.png", contentType: "image/png");

            // when
            renderedManager.FindComponent<InputFile>().UploadFiles(file);

            // then
            this.profileViewServiceMock.Verify(service =>
                service.SetProfileImageAsync(
                    this.userId, It.IsAny<Stream>(), It.IsAny<long>(), "image/png"),
                        Times.Once);

            renderedManager.Markup.Should().Contain("Your profile image has been updated.");
        }

        [Fact]
        public void ShouldShowValidationMessageWhenServiceRejectsUpload()
        {
            // given
            IRenderedComponent<ProfileImageManager> renderedManager = RenderManager(hasImage: false);

            this.profileViewServiceMock.Setup(service =>
                service.SetProfileImageAsync(
                    this.userId, It.IsAny<Stream>(), It.IsAny<long>(), It.IsAny<string>()))
                        .ThrowsAsync(new ProfileViewValidationException("That file is not an image."));

            var file = InputFileContent.CreateFromBinary(
                new byte[] { 1, 2, 3, 4 }, "note.txt", contentType: "text/plain");

            // when
            renderedManager.FindComponent<InputFile>().UploadFiles(file);

            // then
            renderedManager.Find("div.alert-danger").TextContent
                .Should().Contain("That file is not an image.");
        }

        [Fact]
        public void ShouldCallServiceWhenRemoveClicked()
        {
            // given
            IRenderedComponent<ProfileImageManager> renderedManager = RenderManager(hasImage: true);

            this.profileViewServiceMock.Setup(service =>
                service.RemoveProfileImageAsync(this.userId))
                    .Returns(ValueTask.CompletedTask);

            // when
            renderedManager.Find("button.btn-outline-danger").Click();

            // then
            this.profileViewServiceMock.Verify(service =>
                service.RemoveProfileImageAsync(this.userId),
                    Times.Once);
        }
    }
}
