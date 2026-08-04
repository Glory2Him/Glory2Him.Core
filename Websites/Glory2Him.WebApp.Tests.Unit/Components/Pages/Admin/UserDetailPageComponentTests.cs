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
using System.Collections.Generic;
using Bunit;
using Glory2Him.WebApp.Components.Pages.Admin;
using Glory2Him.WebApp.Models.Views.Users;
using Glory2Him.WebApp.Services.Views.Users;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Glory2Him.WebApp.Tests.Unit.Components.Pages.Admin
{
    public partial class UserDetailPageComponentTests : BunitContext
    {
        private readonly Mock<IUsersViewService> usersViewServiceMock;
        private readonly Guid userId;

        public UserDetailPageComponentTests()
        {
            this.usersViewServiceMock = new Mock<IUsersViewService>();
            this.userId = Guid.NewGuid();

            Services.AddSingleton(this.usersViewServiceMock.Object);
            JSInterop.Mode = JSRuntimeMode.Loose;

            this.usersViewServiceMock.Setup(service =>
                service.RetrieveAllRoleNamesAsync())
                    .ReturnsAsync(new List<string> { "Administrators", "Users" });
        }

        private UserView CreateUser(
            bool isDisabled = false,
            bool isLockedOut = false,
            bool emailConfirmed = true,
            bool twoFactorEnabled = false,
            List<string> roles = null) =>
            new UserView
            {
                Id = this.userId,
                UserName = "someone",
                Email = "someone@glory2him.local",
                PhoneNumber = "0123",
                Name = "Some",
                Surname = "One",
                EmailConfirmed = emailConfirmed,
                IsLockedOut = isLockedOut,
                TwoFactorEnabled = twoFactorEnabled,
                IsDisabled = isDisabled,
                AccessFailedCount = 2,
                Roles = roles ?? new List<string> { "Users" },
            };

        private void GivenUser(UserView user) =>
            this.usersViewServiceMock.Setup(service =>
                service.RetrieveUserByIdAsync(this.userId))
                    .ReturnsAsync(user);

        private IRenderedComponent<UserDetailPage> RenderPage() =>
            Render<UserDetailPage>(parameters =>
                parameters.Add(page => page.UserId, this.userId));
    }
}
