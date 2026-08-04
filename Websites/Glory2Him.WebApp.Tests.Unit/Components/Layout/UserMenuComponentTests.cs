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

using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Glory2Him.WebApp.Components.Layout;
using Glory2Him.WebApp.Services.Views.Profiles;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Glory2Him.WebApp.Tests.Unit.Components.Layout
{
    public class UserMenuComponentTests : BunitContext
    {
        public UserMenuComponentTests()
        {
            Services.AddSingleton<AntiforgeryStateProvider, FakeAntiforgeryStateProvider>();
            Services.AddSingleton(Mock.Of<IProfileViewService>());
        }

        [Fact]
        public void ShouldRenderSignInIconWhenAnonymous()
        {
            // given
            this.AddAuthorization().SetNotAuthorized();

            // when
            IRenderedComponent<UserMenuComponent> renderedMenu = Render<UserMenuComponent>();

            // then (no bell/avatar, just a sign-in link)
            renderedMenu.Markup.Should().Contain("Account/Login");
            renderedMenu.Markup.Should().Contain("bi-person-circle");
            renderedMenu.FindAll("#notificationMenu").Should().BeEmpty();
            renderedMenu.FindAll("#profileMenu").Should().BeEmpty();
        }

        [Fact]
        public void ShouldRenderBellAndProfileMenuWhenAuthenticated()
        {
            // given
            this.AddAuthorization().SetAuthorized("Admin");

            // when
            IRenderedComponent<UserMenuComponent> renderedMenu = Render<UserMenuComponent>();

            // then
            renderedMenu.Find("#notificationMenu").Should().NotBeNull();
            renderedMenu.Find("#profileMenu").Should().NotBeNull();
            // No uploaded image in this test → the AvatarComponent shows initials.
            renderedMenu.Find("span[role='img']").TextContent.Trim().Should().Be("AD");
            renderedMenu.Markup.Should().Contain("Admin");
            renderedMenu.Markup.Should().Contain("Account/Manage");
            renderedMenu.Find("form[action='Account/Logout']").Should().NotBeNull();
        }

        [Fact]
        public void ShouldShowProjectsLinkOnlyForAdministrators()
        {
            // given
            this.AddAuthorization().SetAuthorized("User");

            // when
            IRenderedComponent<UserMenuComponent> renderedMenu = Render<UserMenuComponent>();

            // then (non-admin: no admin Projects link)
            renderedMenu.Markup.Should().NotContain("Admin/Posts");
        }

        [Fact]
        public void ShouldShowProjectsLinkForAdministrator()
        {
            // given
            BunitAuthorizationContext authorizationContext = this.AddAuthorization();
            authorizationContext.SetAuthorized("Admin");
            authorizationContext.SetRoles("Administrators");

            // when
            IRenderedComponent<UserMenuComponent> renderedMenu = Render<UserMenuComponent>();

            // then
            renderedMenu.Markup.Should().Contain("Admin/Posts");
        }
    }
}
