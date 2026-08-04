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
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.CoreUI;
using Glory2Him.WebApp.Models.Views.Navigations;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public class BreadcrumbComponentTests : BunitContext
    {
        [Fact]
        public void ShouldAlwaysRenderHomeCrumb()
        {
            // given . when
            IRenderedComponent<BreadcrumbComponent> renderedCrumbs =
                Render<BreadcrumbComponent>();

            // then
            renderedCrumbs.FindAll("li.breadcrumb-item").Should().HaveCount(1);
            renderedCrumbs.Markup.Should().Contain("Home");
        }

        [Fact]
        public void ShouldRenderTrailWithActiveLastCrumb()
        {
            // given
            var items = new List<BreadcrumbItem>
            {
                new BreadcrumbItem("Admin"),
                new BreadcrumbItem("Users", "Admin/Users", IsActive: true),
            };

            // when
            IRenderedComponent<BreadcrumbComponent> renderedCrumbs =
                Render<BreadcrumbComponent>(parameters =>
                    parameters.Add(crumbs => crumbs.Items, items));

            // then (Home + Admin + Users)
            renderedCrumbs.FindAll("li.breadcrumb-item").Should().HaveCount(3);
            renderedCrumbs.Find("li.breadcrumb-item.active").TextContent
                .Should().Contain("Users");
        }

        [Fact]
        public void ShouldRenderNonActiveCrumbAsLink()
        {
            // given
            var items = new List<BreadcrumbItem>
            {
                new BreadcrumbItem("Journal", "Categories"),
            };

            // when
            IRenderedComponent<BreadcrumbComponent> renderedCrumbs =
                Render<BreadcrumbComponent>(parameters =>
                    parameters.Add(crumbs => crumbs.Items, items));

            // then
            renderedCrumbs.FindAll("li.breadcrumb-item a")[1]
                .GetAttribute("href").Should().Be("Categories");
        }
    }
}
