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
        public void ShouldRenderUserNameAndLogoutWhenAuthenticated()
        {
            // given
            string userName = "cjdutoit";
            this.AddAuthorization().SetAuthorized(userName);

            // when
            IRenderedComponent<HeaderComponent> renderedHeader = Render<HeaderComponent>();

            // then
            renderedHeader.Markup.Should().Contain(userName);
            renderedHeader.Markup.Should().Contain("Logout");
            renderedHeader.Find("form[action='Account/Logout']").Should().NotBeNull();
        }
    }
}
