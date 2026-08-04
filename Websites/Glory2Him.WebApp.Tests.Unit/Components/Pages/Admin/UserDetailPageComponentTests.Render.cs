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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.Pages.Admin;
using Glory2Him.WebApp.Models.Views.Users;
using Glory2Him.WebApp.Models.Views.Users.Exceptions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Glory2Him.WebApp.Tests.Unit.Components.Pages.Admin
{
    public partial class UserDetailPageComponentTests
    {
        [Fact]
        public void ShouldRenderTheUserWithStatusBadges()
        {
            // given
            GivenUser(CreateUser(
                isDisabled: true,
                isLockedOut: true,
                emailConfirmed: false,
                twoFactorEnabled: true));

            // when
            IRenderedComponent<UserDetailPage> renderedPage = RenderPage();

            // then
            renderedPage.Markup.Should().Contain("Email unconfirmed");
            renderedPage.Markup.Should().Contain("Locked out");
            renderedPage.Markup.Should().Contain("2FA on");
            renderedPage.Markup.Should().Contain("Disabled");
            renderedPage.Markup.Should().Contain("Failed logins: 2");
        }

        [Fact]
        public void ShouldRenderTheProfileFieldsForEditing()
        {
            // given
            GivenUser(CreateUser());

            // when
            IRenderedComponent<UserDetailPage> renderedPage = RenderPage();

            // then
            renderedPage.FindAll("input")
                .Select(input => input.GetAttribute("value"))
                .Should().Contain(new[] { "someone", "someone@glory2him.local", "0123", "Some", "One" });
        }

        [Fact]
        public void ShouldRenderErrorWhenTheUserCannotBeLoaded()
        {
            // given
            this.usersViewServiceMock.Setup(service =>
                service.RetrieveUserByIdAsync(this.userId))
                    .ThrowsAsync(new UsersViewValidationException("That user no longer exists."));

            // when
            IRenderedComponent<UserDetailPage> renderedPage = RenderPage();

            // then
            renderedPage.Find("div.alert-danger").TextContent
                .Should().Contain("no longer exists");
        }

        [Fact]
        public void ShouldSaveTheProfileWhenSaveClicked()
        {
            // given
            GivenUser(CreateUser());

            this.usersViewServiceMock.Setup(service =>
                service.ModifyUserAsync(It.IsAny<UserView>()))
                    .Returns(ValueTask.CompletedTask);

            IRenderedComponent<UserDetailPage> renderedPage = RenderPage();

            // when
            renderedPage.FindAll("button")
                .First(button => button.TextContent.Trim() == "Save profile")
                .Click();

            // then
            this.usersViewServiceMock.Verify(service =>
                service.ModifyUserAsync(It.Is<UserView>(user => user.Id == this.userId)),
                    Times.Once);

            renderedPage.Find("div.alert-success").TextContent.Should().Contain("Profile updated");
        }

        [Fact]
        public void ShouldAddARoleWhenAddClicked()
        {
            // given
            GivenUser(CreateUser(roles: new List<string> { "Users" }));

            this.usersViewServiceMock.Setup(service =>
                service.SetUserRoleAsync(this.userId, "Administrators", true))
                    .Returns(ValueTask.CompletedTask);

            IRenderedComponent<UserDetailPage> renderedPage = RenderPage();

            // when (Administrators is the only role the user does not already hold)
            renderedPage.FindAll("button")
                .First(button => button.TextContent.Trim() == "Add")
                .Click();

            // then
            this.usersViewServiceMock.Verify(service =>
                service.SetUserRoleAsync(this.userId, "Administrators", true),
                    Times.Once);
        }

        [Fact]
        public void ShouldRemoveARoleWhenRemoveClicked()
        {
            // given
            GivenUser(CreateUser(roles: new List<string> { "Users" }));

            this.usersViewServiceMock.Setup(service =>
                service.SetUserRoleAsync(this.userId, "Users", false))
                    .Returns(ValueTask.CompletedTask);

            IRenderedComponent<UserDetailPage> renderedPage = RenderPage();

            // when
            renderedPage.FindAll("button")
                .First(button => button.TextContent.Trim() == "Remove")
                .Click();

            // then
            this.usersViewServiceMock.Verify(service =>
                service.SetUserRoleAsync(this.userId, "Users", false),
                    Times.Once);
        }

        [Fact]
        public void ShouldSurfaceTheRealReasonWhenAnActionIsRefused()
        {
            // given
            GivenUser(CreateUser(roles: new List<string> { "Administrators" }));

            this.usersViewServiceMock.Setup(service =>
                service.SetUserRoleAsync(this.userId, "Administrators", false))
                    .ThrowsAsync(new UsersViewValidationException(
                        "This is the last administrator, so it cannot be removed."));

            IRenderedComponent<UserDetailPage> renderedPage = RenderPage();

            // when
            renderedPage.FindAll("button")
                .First(button => button.TextContent.Trim() == "Remove")
                .Click();

            // then
            renderedPage.Find("div.alert-danger").TextContent
                .Should().Contain("last administrator");
        }

        [Fact]
        public void ShouldLockAnUnlockedUser()
        {
            // given
            GivenUser(CreateUser(isLockedOut: false));

            this.usersViewServiceMock.Setup(service =>
                service.SetUserLockedOutAsync(this.userId, true))
                    .Returns(ValueTask.CompletedTask);

            IRenderedComponent<UserDetailPage> renderedPage = RenderPage();

            // when
            renderedPage.FindAll("button")
                .First(button => button.TextContent.Trim() == "Lock")
                .Click();

            // then
            this.usersViewServiceMock.Verify(service =>
                service.SetUserLockedOutAsync(this.userId, true),
                    Times.Once);
        }

        [Fact]
        public void ShouldUnlockALockedOutUser()
        {
            // given
            GivenUser(CreateUser(isLockedOut: true));

            this.usersViewServiceMock.Setup(service =>
                service.SetUserLockedOutAsync(this.userId, false))
                    .Returns(ValueTask.CompletedTask);

            IRenderedComponent<UserDetailPage> renderedPage = RenderPage();

            // when
            renderedPage.FindAll("button")
                .First(button => button.TextContent.Trim() == "Unlock")
                .Click();

            // then
            this.usersViewServiceMock.Verify(service =>
                service.SetUserLockedOutAsync(this.userId, false),
                    Times.Once);
        }

        [Fact]
        public void ShouldConfirmEmailOnlyWhenItIsUnconfirmed()
        {
            // given
            GivenUser(CreateUser(emailConfirmed: true));

            // when
            IRenderedComponent<UserDetailPage> renderedPage = RenderPage();

            // then
            renderedPage.FindAll("button")
                .Should().NotContain(button => button.TextContent.Trim() == "Confirm email");
        }

        [Fact]
        public void ShouldEnableTwoFactorWhenItIsOff()
        {
            // given
            GivenUser(CreateUser(twoFactorEnabled: false));

            this.usersViewServiceMock.Setup(service =>
                service.SetTwoFactorEnabledAsync(this.userId, true))
                    .Returns(ValueTask.CompletedTask);

            IRenderedComponent<UserDetailPage> renderedPage = RenderPage();

            // when
            renderedPage.FindAll("button")
                .First(button => button.TextContent.Trim() == "Enable 2FA")
                .Click();

            // then
            this.usersViewServiceMock.Verify(service =>
                service.SetTwoFactorEnabledAsync(this.userId, true),
                    Times.Once);
        }

        [Fact]
        public void ShouldResetTheFailedLoginCount()
        {
            // given
            GivenUser(CreateUser());

            this.usersViewServiceMock.Setup(service =>
                service.ResetAccessFailedCountAsync(this.userId))
                    .Returns(ValueTask.CompletedTask);

            IRenderedComponent<UserDetailPage> renderedPage = RenderPage();

            // when
            renderedPage.FindAll("button")
                .First(button => button.TextContent.Trim() == "Reset failed count")
                .Click();

            // then
            this.usersViewServiceMock.Verify(service =>
                service.ResetAccessFailedCountAsync(this.userId),
                    Times.Once);
        }

        [Fact]
        public void ShouldDisableAnActiveUser()
        {
            // given
            GivenUser(CreateUser(isDisabled: false));

            this.usersViewServiceMock.Setup(service =>
                service.SetUserDisabledAsync(this.userId, true))
                    .Returns(ValueTask.CompletedTask);

            IRenderedComponent<UserDetailPage> renderedPage = RenderPage();

            // when
            renderedPage.FindAll("button")
                .First(button => button.TextContent.Trim() == "Disable user")
                .Click();

            // then
            this.usersViewServiceMock.Verify(service =>
                service.SetUserDisabledAsync(this.userId, true),
                    Times.Once);
        }

        [Fact]
        public void ShouldShowAShareableResetLinkWhenGenerated()
        {
            // given
            GivenUser(CreateUser());

            this.usersViewServiceMock.Setup(service =>
                service.GeneratePasswordResetTokenAsync(this.userId))
                    .ReturnsAsync("reset-token");

            IRenderedComponent<UserDetailPage> renderedPage = RenderPage();

            // when
            renderedPage.FindAll("button")
                .First(button => button.TextContent.Trim() == "Password reset link")
                .Click();

            // then
            renderedPage.Find("div.alert-info").TextContent.Should().Contain("Password reset link");
            renderedPage.Find("textarea").TextContent.Should().Contain("Account/ResetPassword");
        }

        [Fact]
        public void ShouldDeleteTheUserAndReturnToTheListWhenConfirmed()
        {
            // given
            GivenUser(CreateUser());

            this.usersViewServiceMock.Setup(service =>
                service.DeleteUserAsync(this.userId))
                    .Returns(ValueTask.CompletedTask);

            IRenderedComponent<UserDetailPage> renderedPage = RenderPage();
            var navigationManager = Services.GetRequiredService<NavigationManager>();

            renderedPage.FindAll("button")
                .First(button => button.TextContent.Trim() == "Delete user")
                .Click();

            // when (confirm)
            renderedPage.Find("div.modal-footer button.btn-danger").Click();

            // then
            this.usersViewServiceMock.Verify(service =>
                service.DeleteUserAsync(this.userId),
                    Times.Once);

            navigationManager.Uri.Should().EndWith("Admin/Users");
        }
    }
}
