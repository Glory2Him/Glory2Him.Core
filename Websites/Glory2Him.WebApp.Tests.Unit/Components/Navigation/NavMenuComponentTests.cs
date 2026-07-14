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

using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Glory2Him.WebApp.Components.Navigation;

namespace Glory2Him.WebApp.Tests.Unit.Components.Navigation
{
    public class NavMenuComponentTests : BunitContext
    {
        [Fact]
        public void ShouldHideAuthenticatedSectionsWhenAnonymous()
        {
            // given
            this.AddAuthorization().SetNotAuthorized();

            // when
            IRenderedComponent<NavMenu> renderedMenu = Render<NavMenu>();

            // then
            renderedMenu.Markup.Should().NotContain("admin/users");
            renderedMenu.Markup.Should().NotContain("Account/Manage");
        }

        [Fact]
        public void ShouldShowMyAccountButNotAdminForNonAdministrator()
        {
            // given
            this.AddAuthorization().SetAuthorized("User");

            // when
            IRenderedComponent<NavMenu> renderedMenu = Render<NavMenu>();

            // then
            renderedMenu.Markup.Should().Contain("Account/Manage");
            renderedMenu.Markup.Should().Contain("My Account");
            renderedMenu.Markup.Should().NotContain("admin/users");
            renderedMenu.Markup.Should().NotContain("admin/posts");
        }

        [Fact]
        public void ShouldShowAdminSectionForAdministrator()
        {
            // given
            BunitAuthorizationContext authorizationContext = this.AddAuthorization();
            authorizationContext.SetAuthorized("Admin");
            authorizationContext.SetRoles("Administrators");

            // when
            IRenderedComponent<NavMenu> renderedMenu = Render<NavMenu>();

            // then
            renderedMenu.Markup.Should().Contain("admin/users");
            renderedMenu.Markup.Should().Contain("admin/posts");
            renderedMenu.Markup.Should().Contain("Account/Manage");
        }

        [Fact]
        public void ShouldRenderBootstrapIconsNotCoreUiIcons()
        {
            // given
            this.AddAuthorization().SetAuthorized("User");

            // when
            IRenderedComponent<NavMenu> renderedMenu = Render<NavMenu>();

            // then (CoreUI's cil-* set is not loaded in this site — icons must be Bootstrap Icons)
            renderedMenu.Markup.Should().Contain("bi-");
            renderedMenu.Markup.Should().NotContain("cil-");
        }
    }
}
