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

using System.Linq;
using AngleSharp.Dom;
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
    public class HeaderComponentTests : BunitContext
    {
        public HeaderComponentTests()
        {
            Services.AddSingleton<AntiforgeryStateProvider, FakeAntiforgeryStateProvider>();
            Services.AddSingleton(Mock.Of<IProfileViewService>());
        }

        [Fact]
        public void ShouldRenderBrandAndLoginLinkWhenAnonymous()
        {
            // given
            this.AddAuthorization().SetNotAuthorized();

            // when
            IRenderedComponent<HeaderComponent> renderedHeader = Render<HeaderComponent>();

            // then
            renderedHeader.Find("a.navbar-brand").GetAttribute("href").Should().Be("/");
            renderedHeader.Markup.Should().Contain("Account/Login");
            renderedHeader.Markup.Should().Contain("Login / Join");
        }

        [Fact]
        public void ShouldShowTheSeparatorBeforeLoginJoinWhenAnonymous()
        {
            // given
            this.AddAuthorization().SetNotAuthorized();

            // when
            IRenderedComponent<HeaderComponent> renderedHeader = Render<HeaderComponent>();

            // then (the separator marks off About/Contact from the sign-in link, same as it marks
            // off the signed-in user's own links once they're logged in)
            renderedHeader.Markup.Should().Contain("|");
        }

        [Fact]
        public void ShouldRenderUserNameAndLogoutWhenAuthenticated()
        {
            // given
            this.AddAuthorization().SetAuthorized("cjdutoit");

            // when
            IRenderedComponent<HeaderComponent> renderedHeader = Render<HeaderComponent>();

            // then (the raw username reads as a typo next to About / Contact, so it is capitalized)
            renderedHeader.Markup.Should().Contain("Cjdutoit");
            renderedHeader.Markup.Should().Contain("Logout");
            renderedHeader.Find("form[action='Account/Logout']").Should().NotBeNull();
        }

        [Fact]
        public void ShouldLinkTheDisplayNameToTheOwnProfileWithAPersonIcon()
        {
            // given
            this.AddAuthorization().SetAuthorized("cjdutoit");

            // when
            IRenderedComponent<HeaderComponent> renderedHeader = Render<HeaderComponent>();

            // then
            IElement profileLink = renderedHeader.Find(".navbar-top a[href='Account/Manage']");
            profileLink.TextContent.Trim().Should().Be("Cjdutoit");
            profileLink.QuerySelector("i.bi-person").Should().NotBeNull();
        }

        [Fact]
        public void ShouldNotShowTheAdminLinkButStillShowTheSeparatorForARegularUser()
        {
            // given
            this.AddAuthorization().SetAuthorized("cjdutoit");

            // when
            IRenderedComponent<HeaderComponent> renderedHeader = Render<HeaderComponent>();

            // then (the separator is always shown for a signed-in user — it separates
            // About/Contact from their own links, admin or not)
            renderedHeader.FindAll(".navbar-top a[href='Admin/Users']").Should().BeEmpty();
            renderedHeader.Markup.Should().Contain("|");
        }

        [Fact]
        public void ShouldShowASeparateAdminLinkBeforeTheAlwaysPresentSeparatorForAnAdministrator()
        {
            // given
            BunitAuthorizationContext authorizationContext = this.AddAuthorization();
            authorizationContext.SetAuthorized("admin");
            authorizationContext.SetRoles("Administrators");

            // when
            IRenderedComponent<HeaderComponent> renderedHeader = Render<HeaderComponent>();

            // then (a separate "Admin" link sits to the left of the separator, which sits to the
            // left of the profile link — which keeps pointing at the administrator's own profile;
            // clicking your name should never surprise you by landing somewhere other than your
            // account)
            renderedHeader.Find(".navbar-top a[href='Admin/Users']").TextContent.Trim()
                .Should().Be("Admin");

            renderedHeader.Find(".navbar-top a[href='Account/Manage']").TextContent.Trim()
                .Should().Be("Admin");

            renderedHeader.Markup.Should().Contain("|");
        }

        [Fact]
        public void ShouldRenderASignOutIconBesideLogout()
        {
            // given
            this.AddAuthorization().SetAuthorized("cjdutoit");

            // when
            IRenderedComponent<HeaderComponent> renderedHeader = Render<HeaderComponent>();

            // then
            IElement logoutButton = renderedHeader.Find("button[type='submit']");
            logoutButton.TextContent.Trim().Should().Be("Logout");
            logoutButton.QuerySelector("i.bi-box-arrow-right").Should().NotBeNull();
        }

        [Fact]
        public void ShouldVerticallyCenterTheTopBarItems()
        {
            // given
            this.AddAuthorization().SetAuthorized("cjdutoit");

            // when
            IRenderedComponent<HeaderComponent> renderedHeader = Render<HeaderComponent>();

            // then (the Logout button previously sat higher than the plain <a> links beside it —
            // centring the row keeps every item, whatever its own natural height, on one baseline)
            renderedHeader.Find(".navbar-top ul.nav").ClassList.Should().Contain("align-items-center");
        }

        [Fact]
        public void ShouldRenderTheGlory2HimBrandNotTheTemplateLogo()
        {
            // given
            this.AddAuthorization().SetNotAuthorized();

            // when
            IRenderedComponent<HeaderComponent> renderedHeader = Render<HeaderComponent>();

            // then
            // The header carries the wordmark alone at every width — the sunset photo behind it
            // does the visual work, so the brand mark doesn't need its own icon here too. The "2"
            // skips its usual blue accent (AccentTwo="false") since the whole wordmark needs to
            // read as one flat white shape over the photo.
            renderedHeader.Find("span.g2h-brand-name").TextContent.Trim()
                .Should().Be("Glory 2 Him");
            renderedHeader.FindAll("img.g2h-brand-icon").Should().BeEmpty();
            renderedHeader.FindAll("span.text-primary").Should().BeEmpty();
            renderedHeader.Markup.Should().NotContain("glory2him-banner.png");
            renderedHeader.Markup.Should().NotContain("logo.svg");
        }

        [Fact]
        public void ShouldOfferSearchWhereTheNavMenuUsedToBe()
        {
            // given
            this.AddAuthorization().SetNotAuthorized();

            // when
            IRenderedComponent<HeaderComponent> renderedHeader = Render<HeaderComponent>();

            // then (the links, their dropdowns, Subscribe and the toggler that opened them are all
            // gone — one green Search button on the right stands in their place)
            IElement searchButton = renderedHeader.Find("a.btn-success[href='/Search']");
            searchButton.TextContent.Trim().Should().Be("Search");
            searchButton.QuerySelector("i.bi-search").Should().NotBeNull();

            renderedHeader.FindAll("#navbarCollapse").Should().BeEmpty();
            renderedHeader.FindAll("button.navbar-toggler").Should().BeEmpty();
            renderedHeader.Markup.Should().NotContain("Subscribe!");
        }

        [Fact]
        public void ShouldCarryTheThreeSectionsBetweenTheBrandAndTheSearchButton()
        {
            // given
            this.AddAuthorization().SetNotAuthorized();

            // when
            IRenderedComponent<HeaderComponent> renderedHeader = Render<HeaderComponent>();

            // then (separators included, as they read on the page)
            renderedHeader.FindAll("ul.g2h-section-nav .nav-link")
                .Select(link => link.TextContent.Trim())
                .Should().Equal("Posts", "|", "Series", "|", "The Gospel");

            renderedHeader.Find("ul.g2h-section-nav").ClassList.Should().Contain("mx-auto");
        }

        [Fact]
        public void ShouldLinkPostsHomeAndLeaveTheOthersAsPlainTextUntilTheyHaveRoutes()
        {
            // given
            this.AddAuthorization().SetNotAuthorized();

            // when
            IRenderedComponent<HeaderComponent> renderedHeader = Render<HeaderComponent>();

            // then (an anchor that goes nowhere is worse than plain text — Series and The Gospel
            // stay spans until there is somewhere to send a reader)
            IElement sectionNav = renderedHeader.Find("ul.g2h-section-nav");

            sectionNav.QuerySelectorAll("a.nav-link").Should().ContainSingle();
            sectionNav.QuerySelector("a.nav-link")!.GetAttribute("href").Should().Be("/");
            sectionNav.QuerySelector("a.nav-link")!.TextContent.Trim().Should().Be("Posts");

            sectionNav.QuerySelectorAll("span.g2h-section-pending")
                .Select(pending => pending.TextContent.Trim())
                .Should().Equal("Series", "The Gospel");
        }

        [Fact]
        public void ShouldOfferOnlyOneWayIntoSearchFromTheHeader()
        {
            // given
            this.AddAuthorization().SetNotAuthorized();

            // when
            IRenderedComponent<HeaderComponent> renderedHeader = Render<HeaderComponent>();

            // then (the magnifier used to drop its own search form here, posting to a second
            // results page — one header offering two searches that land in different places)
            renderedHeader.FindAll("a[href='/Search']").Should().ContainSingle();
            renderedHeader.FindAll("form[action='Search-Result']").Should().BeEmpty();
            renderedHeader.FindAll("div.nav-search").Should().BeEmpty();
        }

        [Fact]
        public void ShouldCarryThePhotoBackgroundWithNavbarDarkForContrast()
        {
            // given
            this.AddAuthorization().SetNotAuthorized();

            // when
            IRenderedComponent<HeaderComponent> renderedHeader = Render<HeaderComponent>();

            // then (navbar-dark turns every nav-link/icon/wordmark white so they read over the
            // photo, which spans the full header — see HeaderComponent.razor.css)
            renderedHeader.Find("header").ClassList.Should()
                .Contain(new[] { "navbar-dark", "g2h-header-photo" });

            renderedHeader.Find("div.g2h-header-pill").Should().NotBeNull();
        }
    }
}
