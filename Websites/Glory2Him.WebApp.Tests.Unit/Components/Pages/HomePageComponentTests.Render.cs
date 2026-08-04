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
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.Pages;
using Glory2Him.WebApp.Components.Pages.SamplePages;

namespace Glory2Him.WebApp.Tests.Unit.Components.Pages
{
    public partial class HomePageComponentTests
    {
        [Fact]
        public void ShouldOpenWithTheVerseOfTheDay()
        {
            // when
            IRenderedComponent<Home> renderedPage = Render<Home>();

            // then
            renderedPage.Find("span.badge.bg-primary").TextContent.Trim()
                .Should().Be("Verse of the day:");

            renderedPage.Markup.Should().Contain(SampleContent.VerseOfTheDay);
        }

        [Fact]
        public void ShouldLeadWithTheFeaturedStoryBesideThreeSmallerCards()
        {
            // when
            IRenderedComponent<Home> renderedPage = Render<Home>();

            // then (the lead carries the star; the tiles beside it do not)
            List<IElement> heroes =
                renderedPage.FindAll("div.card-overlay-bottom").ToList();

            heroes.Should().HaveCount(4);
            heroes[0].ClassList.Should().Contain("card-grid-lg");
            heroes[0].QuerySelector("span.card-featured").Should().NotBeNull();
            heroes[0].TextContent.Should().Contain(SampleContent.Featured.Title);

            heroes.Skip(1).Should().OnlyContain(hero =>
                hero.ClassList.Contains("card-grid-sm"));
        }

        [Fact]
        public void ShouldListTheLatestPostsUnderTheirOwnHeading()
        {
            // when
            IRenderedComponent<Home> renderedPage = Render<Home>();

            // then
            renderedPage.Find("h2.m-0").TextContent.Trim().Should().Be("Latest posts");

            renderedPage.FindAll("div.card.h-100").Should()
                .HaveCount(SampleContent.Latest.Count);

            foreach (SamplePost post in SampleContent.Latest)
            {
                renderedPage.Markup.Should().Contain(post.Title);
            }
        }

        [Fact]
        public void ShouldCarryTheCategoriesTagsAndReferencesSidebar()
        {
            // when
            IRenderedComponent<Home> renderedPage = Render<Home>();

            // then
            renderedPage.FindAll("div.col-lg-3 h4").Select(heading => heading.TextContent.Trim())
                .Should().Equal("Categories", "Popular tags", "Popular references");
        }

        [Fact]
        public void ShouldSendCategoriesAndTagsToASearchAndReferencesToThePassage()
        {
            // when
            IRenderedComponent<Home> renderedPage = Render<Home>();

            // then (categories and tags are both "show me posts about this"; a reference is the
            // passage itself)
            List<string> sidebarLinks = renderedPage.FindAll("div.col-lg-3 a")
                .Select(link => link.GetAttribute("href")!)
                .ToList();

            int references = SampleContent.PopularReferences.Count;

            sidebarLinks.Take(sidebarLinks.Count - references)
                .Should().OnlyContain(href => href.StartsWith("/Search?q="));

            sidebarLinks.TakeLast(references)
                .Should().OnlyContain(href => href == "/BibleReferences");
        }

        [Fact]
        public void ShouldPointEveryPostAtThePublicPostRoute()
        {
            // when
            IRenderedComponent<Home> renderedPage = Render<Home>();

            // then (the sample this mirrors links to an Administrators-only page — the public
            // home page must not send a visitor somewhere that bounces them to a login)
            renderedPage.Markup.Should().NotContain("SamplePages");

            renderedPage.FindAll("a[href='/Post-Single']").Should()
                .HaveCountGreaterThan(SampleContent.Latest.Count);
        }
    }
}
