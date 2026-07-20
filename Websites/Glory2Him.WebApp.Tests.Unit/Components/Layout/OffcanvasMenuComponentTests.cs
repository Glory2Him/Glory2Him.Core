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

namespace Glory2Him.WebApp.Tests.Unit.Components.Layout
{
    public class OffcanvasMenuComponentTests : BunitContext
    {
        [Fact]
        public void ShouldRenderOffcanvasWithExpectedId()
        {
            // given . when
            IRenderedComponent<OffcanvasMenuComponent> renderedMenu =
                Render<OffcanvasMenuComponent>();

            // then
            renderedMenu.Find("div.offcanvas").GetAttribute("id").Should().Be("offcanvasMenu");
        }

        [Fact]
        public void ShouldRenderPrimaryNavigationLinks()
        {
            // given . when
            IRenderedComponent<OffcanvasMenuComponent> renderedMenu =
                Render<OffcanvasMenuComponent>();

            // then
            renderedMenu.Markup.Should().Contain("about-us");
            renderedMenu.Markup.Should().Contain("contact-us");
            renderedMenu.Markup.Should().Contain("Go and share the Gospel");
        }
    }
}
