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
using Glory2Him.WebApp.Components.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.WebApp.Tests.Unit.Components.Navigation
{
    public class NavMenuComponentTests : BunitContext
    {
        private void NavigateTo(string relativePath) =>
            Services.GetRequiredService<NavigationManager>().NavigateTo(relativePath);

        [Fact]
        public void ShouldHideAuthenticatedSectionsWhenAnonymous()
        {
            // given
            this.AddAuthorization().SetNotAuthorized();

            // when
            IRenderedComponent<NavMenu> renderedMenu = Render<NavMenu>();

            // then
            renderedMenu.Markup.Should().NotContain("Admin/Users");
            renderedMenu.Markup.Should().NotContain("Account/Manage");
        }

        [Fact]
        public void ShouldShowOnlyMyAccountWhenViewingTheAccountArea()
        {
            // given
            this.AddAuthorization().SetAuthorized("User");
            NavigateTo("Account/Manage");

            // when
            IRenderedComponent<NavMenu> renderedMenu = Render<NavMenu>();

            // then
            renderedMenu.Markup.Should().Contain("My Account");
            renderedMenu.Markup.Should().Contain("Account/Manage");
            renderedMenu.Markup.Should().NotContain("Dashboard");
            renderedMenu.Markup.Should().NotContain("Admin/Users");
            renderedMenu.Markup.Should().NotContain("Admin/Posts");
        }

        [Fact]
        public void ShouldShowOnlyDashboardAndAdminWhenViewingTheAdminArea()
        {
            // given
            BunitAuthorizationContext authorizationContext = this.AddAuthorization();
            authorizationContext.SetAuthorized("Admin");
            authorizationContext.SetRoles("Administrators");
            NavigateTo("Admin/Users");

            // when
            IRenderedComponent<NavMenu> renderedMenu = Render<NavMenu>();

            // then
            renderedMenu.Markup.Should().Contain("Dashboard");
            renderedMenu.Markup.Should().Contain("Admin/Users");
            renderedMenu.Markup.Should().Contain("Admin/Posts");
            renderedMenu.Markup.Should().NotContain("My Account");
            renderedMenu.Markup.Should().NotContain("Account/Manage");
        }

        [Fact]
        public void ShouldShowOnlyDashboardAndAdminWhenViewingTheDashboard()
        {
            // given
            BunitAuthorizationContext authorizationContext = this.AddAuthorization();
            authorizationContext.SetAuthorized("Admin");
            authorizationContext.SetRoles("Administrators");
            NavigateTo("Dashboard");

            // when
            IRenderedComponent<NavMenu> renderedMenu = Render<NavMenu>();

            // then
            renderedMenu.Markup.Should().Contain("Dashboard");
            renderedMenu.Markup.Should().NotContain("Account/Manage");
        }

        [Fact]
        public void ShouldHideAdminSectionFromNonAdministratorInTheAdminArea()
        {
            // given
            this.AddAuthorization().SetAuthorized("User");
            NavigateTo("Dashboard");

            // when
            IRenderedComponent<NavMenu> renderedMenu = Render<NavMenu>();

            // then (the Dashboard entry only needs authentication; the Admin section needs the role)
            renderedMenu.Markup.Should().Contain("Dashboard");
            renderedMenu.Markup.Should().NotContain("Admin/Users");
            renderedMenu.Markup.Should().NotContain("Admin/Posts");
        }

        [Fact]
        public void ShouldFollowTheAreaWhenTheLocationChanges()
        {
            // given
            BunitAuthorizationContext authorizationContext = this.AddAuthorization();
            authorizationContext.SetAuthorized("Admin");
            authorizationContext.SetRoles("Administrators");
            NavigateTo("Admin/Users");

            IRenderedComponent<NavMenu> renderedMenu = Render<NavMenu>();
            renderedMenu.Markup.Should().Contain("Admin/Users");

            // when
            NavigateTo("Account/Manage");

            // then
            renderedMenu.Markup.Should().Contain("Account/Manage");
            renderedMenu.Markup.Should().NotContain("Admin/Users");
        }

        [Fact]
        public void ShouldRenderBootstrapIconsNotCoreUiIcons()
        {
            // given
            this.AddAuthorization().SetAuthorized("User");
            NavigateTo("Account/Manage");

            // when
            IRenderedComponent<NavMenu> renderedMenu = Render<NavMenu>();

            // then (CoreUI's cil-* set is not loaded in this site — icons must be Bootstrap Icons)
            renderedMenu.Markup.Should().Contain("bi-");
            renderedMenu.Markup.Should().NotContain("cil-");
        }
    }
}
