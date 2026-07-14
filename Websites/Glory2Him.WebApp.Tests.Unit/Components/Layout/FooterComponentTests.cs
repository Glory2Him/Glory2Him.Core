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
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.Layout;

namespace Glory2Him.WebApp.Tests.Unit.Components.Layout
{
    public class FooterComponentTests : BunitContext
    {
        [Fact]
        public void ShouldRenderCurrentYearInCopyright()
        {
            // given
            string expectedYear = DateTime.UtcNow.Year.ToString();

            // when
            IRenderedComponent<FooterComponent> renderedFooter = Render<FooterComponent>();

            // then
            renderedFooter.Find("footer").TextContent.Should().Contain(expectedYear);
            renderedFooter.Markup.Should().Contain("Glory 2 Him");
        }

        [Fact]
        public void ShouldRenderExploreAndAccountLinks()
        {
            // given . when
            IRenderedComponent<FooterComponent> renderedFooter = Render<FooterComponent>();

            // then
            renderedFooter.Markup.Should().Contain("about-us");
            renderedFooter.Markup.Should().Contain("Account/Login");
            renderedFooter.Markup.Should().Contain("John 14:6");
        }
    }
}
