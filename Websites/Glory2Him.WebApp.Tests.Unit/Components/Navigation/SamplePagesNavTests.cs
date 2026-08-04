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
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Glory2Him.WebApp.Components.Navigation;
using Glory2Him.WebApp.Models.Views.Navigations;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.WebApp.Tests.Unit.Components.Navigation
{
    public class SamplePagesNavTests : BunitContext
    {
        private void NavigateTo(string relativePath) =>
            Services.GetRequiredService<NavigationManager>().NavigateTo(relativePath);

        private static IEnumerable<NavItem> Leaves(NavItem item)
        {
            if (!item.HasChildren)
            {
                yield return item;

                yield break;
            }

            foreach (NavItem leaf in item.Children!.SelectMany(Leaves))
            {
                yield return leaf;
            }
        }

        [Fact]
        public void ShouldPlaceSamplePagesInTheAdminAreaAfterTheAdminSection()
        {
            // when
            IReadOnlyList<NavItem> adminArea = NavMenuProvider.GetNavMenu("Dashboard");

            // then (the section has to sit below Admin, not in an area of its own)
            List<string> titles = adminArea.Select(item => item.Title).ToList();

            titles.Should().ContainInOrder("Admin", "Sample Pages");
        }

        [Fact]
        public void ShouldCarryEverySampleGroupFromTheSpec()
        {
            // when
            NavItem samplePages = NavMenuProvider.GetSamplePagesSection();

            // then
            samplePages.Children!.Select(child => child.Title)
                .Should().Equal(
                    "Home", "Pages", "Post", "Bible References", "Lifestyle", "Dashboard");
        }

        [Fact]
        public void ShouldPlaceBibleReferencesDirectlyBelowPost()
        {
            // given
            NavItem samplePages = NavMenuProvider.GetSamplePagesSection();

            // when
            List<string> titles = samplePages.Children!.Select(child => child.Title).ToList();

            // then
            titles.IndexOf("Bible References").Should().Be(titles.IndexOf("Post") + 1);
        }

        [Fact]
        public void ShouldNestBothBibleReferenceViewsBeneathTheGroup()
        {
            // given
            NavItem samplePages = NavMenuProvider.GetSamplePagesSection();

            // when
            NavItem bibleReferences =
                samplePages.Children!.Single(child => child.Title == "Bible References");

            // then
            bibleReferences.HasChildren.Should().BeTrue();

            bibleReferences.Children!.Select(child => child.Title).Should().Equal(
                "Bible Reference - Partial",
                "Bible Reference - Full Chapter");

            bibleReferences.Children!.Select(child => child.Href).Should().Equal(
                "SamplePages/BibleReferences/BibleReference-Single-verse",
                "SamplePages/BibleReferences/BibleReference-Full-Chapter");
        }

        [Fact]
        public void ShouldNestPostGridBeneathPost()
        {
            // given
            NavItem samplePages = NavMenuProvider.GetSamplePagesSection();

            // when
            NavItem post = samplePages.Children!.Single(child => child.Title == "Post");
            NavItem postGrid = post.Children!.Single(child => child.Title == "Post Grid");

            // then (Post Grid is the one group that nests a third level)
            postGrid.HasChildren.Should().BeTrue();

            postGrid.Children!.Select(child => child.Title).Should().Equal(
                "Post Grid",
                "Post Grid 4 Col",
                "Post Grid Masonry",
                "Post Grid Masonry Filter",
                "Post Mixed Large Then Grid");
        }

        [Fact]
        public void ShouldRouteEverySampleUnderTheSamplePagesArea()
        {
            // when
            List<NavItem> leaves = Leaves(NavMenuProvider.GetSamplePagesSection()).ToList();

            // then
            leaves.Should().HaveCountGreaterThan(25);

            leaves.Should().OnlyContain(leaf =>
                leaf.Href.StartsWith("SamplePages/", StringComparison.Ordinal));
        }

        [Fact]
        public void ShouldMatchEachSampleExactly()
        {
            // when
            List<NavItem> leaves = Leaves(NavMenuProvider.GetSamplePagesSection()).ToList();

            // then (prefix matching would light "…/Post-Grid/Post-Grid" up while viewing
            // "…/Post-Grid/Post-Grid-4-Col", since the longer route starts with the shorter one)
            leaves.Should().OnlyContain(leaf => leaf.ExactMatch);
        }

        [Fact]
        public void ShouldRestrictEverySampleToAdministrators()
        {
            // when
            List<NavItem> leaves = Leaves(NavMenuProvider.GetSamplePagesSection()).ToList();

            // then
            leaves.Should().OnlyContain(leaf =>
                leaf.Roles != null && leaf.Roles.Contains("Administrators"));
        }

        [Fact]
        public void ShouldRenderSamplePagesForAnAdministrator()
        {
            // given
            BunitAuthorizationContext authorizationContext = this.AddAuthorization();
            authorizationContext.SetAuthorized("admin");
            authorizationContext.SetRoles("Administrators");
            NavigateTo("Dashboard");

            // when
            IRenderedComponent<NavMenu> renderedMenu = Render<NavMenu>();

            // then
            renderedMenu.Markup.Should().Contain("Sample Pages");
            renderedMenu.Markup.Should().Contain("SamplePages/Home/Default");
        }

        [Fact]
        public void ShouldHideSamplePagesFromANonAdministrator()
        {
            // given
            this.AddAuthorization().SetAuthorized("User");
            NavigateTo("Dashboard");

            // when
            IRenderedComponent<NavMenu> renderedMenu = Render<NavMenu>();

            // then
            renderedMenu.Markup.Should().NotContain("Sample Pages");
            renderedMenu.Markup.Should().NotContain("SamplePages/");
        }

        [Fact]
        public void ShouldRenderCollapsibleGroupsForTheNestedSections()
        {
            // given
            BunitAuthorizationContext authorizationContext = this.AddAuthorization();
            authorizationContext.SetAuthorized("admin");
            authorizationContext.SetRoles("Administrators");
            NavigateTo("Dashboard");

            // when
            IRenderedComponent<NavMenu> renderedMenu = Render<NavMenu>();

            // then (Bootstrap drives the collapse so it also works on statically rendered pages)
            IElement toggle = renderedMenu.FindAll("a.nav-group-toggle")
                .First(element => element.TextContent.Contains("Home"));

            toggle.GetAttribute("data-bs-toggle").Should().Be("collapse");
            toggle.GetAttribute("href").Should().StartWith("#nav-group-");

            renderedMenu.FindAll("ul.nav-group-items.collapse").Should().NotBeEmpty();
        }

        [Fact]
        public void ShouldNavigateToSamplesInTheSameTab()
        {
            // given
            BunitAuthorizationContext authorizationContext = this.AddAuthorization();
            authorizationContext.SetAuthorized("admin");
            authorizationContext.SetRoles("Administrators");
            NavigateTo("Dashboard");

            // when
            IRenderedComponent<NavMenu> renderedMenu = Render<NavMenu>();

            // then (you come back via the demo's own "Back to Sample Pages" button, not by
            // closing a tab, so nothing in the sidebar should open a new window)
            IElement sampleLink = renderedMenu.Find("a[href='SamplePages/Home/Default']");

            sampleLink.HasAttribute("target").Should().BeFalse();
            renderedMenu.Markup.Should().NotContain("_blank");
        }

        [Fact]
        public void ShouldExpandTheBranchContainingThePageBeingViewed()
        {
            // given
            BunitAuthorizationContext authorizationContext = this.AddAuthorization();
            authorizationContext.SetAuthorized("admin");
            authorizationContext.SetRoles("Administrators");
            NavigateTo("Admin/Users");

            // when
            IRenderedComponent<NavMenu> renderedMenu = Render<NavMenu>();

            // then (a collapsed sample tree still renders, but nothing in it is the active page)
            renderedMenu.FindAll("a.nav-group-toggle.collapsed").Should().NotBeEmpty();
        }
    }
}
