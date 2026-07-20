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
using Glory2Him.WebApp.Components.CoreUI;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public partial class PageHeaderComponentTests
    {
        [Fact]
        public void ShouldRenderTitleAndHomeBreadcrumb()
        {
            // given
            string randomTitle = GetRandomString();

            // when
            IRenderedComponent<PageHeaderComponent> renderedHeader =
                Render<PageHeaderComponent>(parameters =>
                    parameters.Add(header => header.Title, randomTitle));

            // then
            renderedHeader.Find("h1").TextContent.Should().Be(randomTitle);
            renderedHeader.Markup.Should().Contain("Home");
        }

        [Fact]
        public void ShouldNotRenderParentCrumbWhenParentTitleIsMissing()
        {
            // given
            string randomTitle = GetRandomString();

            // when
            IRenderedComponent<PageHeaderComponent> renderedHeader =
                Render<PageHeaderComponent>(parameters =>
                    parameters.Add(header => header.Title, randomTitle));

            // then (Home + active page only)
            renderedHeader.FindAll("li.breadcrumb-item").Count.Should().Be(2);
        }

        [Fact]
        public void ShouldRenderParentCrumbWhenParentTitleIsProvided()
        {
            // given
            string randomTitle = GetRandomString();
            string randomParent = GetRandomString();

            // when
            IRenderedComponent<PageHeaderComponent> renderedHeader =
                Render<PageHeaderComponent>(parameters => parameters
                    .Add(header => header.Title, randomTitle)
                    .Add(header => header.ParentTitle, randomParent)
                    .Add(header => header.ParentHref, "parent"));

            // then (Home + parent + active page)
            renderedHeader.FindAll("li.breadcrumb-item").Count.Should().Be(3);
            renderedHeader.Markup.Should().Contain(randomParent);
        }
    }
}
