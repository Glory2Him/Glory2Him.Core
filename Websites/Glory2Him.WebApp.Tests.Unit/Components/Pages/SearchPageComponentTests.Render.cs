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
using FluentAssertions;
using Glory2Him.WebApp.Components.Pages;
using Glory2Him.WebApp.Components.Pages.SamplePages;

namespace Glory2Him.WebApp.Tests.Unit.Components.Pages
{
    public partial class SearchPageComponentTests
    {
        [Fact]
        public void ShouldOpenAsNothingButTheSearchBox()
        {
            // when
            IRenderedComponent<Search> page = Render<Search>();

            // then
            page.Find("input[type='search']").Should().NotBeNull();
            page.Find("h1").TextContent.Trim().Should().Be("Search");
            page.FindAll("div.card").Should().BeEmpty();
        }

        [Fact]
        public void ShouldReturnTheHomePagePostsWhateverIsSearchedFor()
        {
            // given (a demo: the text is not matched against anything)
            IRenderedComponent<Search> page = Render<Search>();

            // when
            SearchFor(page, "something no post mentions");

            // then
            ResultTitles(page).Should().Equal(
                SampleContent.Latest.Select(post => post.Title));

            page.Find("p.text-muted").TextContent.Should().Contain("4 result");
        }

        [Fact]
        public void ShouldLayTheResultsOutAsThePostListRowsWithItsSidebar()
        {
            // given
            IRenderedComponent<Search> page = Render<Search>();

            // when
            SearchFor(page, "grace");

            // then (image left, copy right — the Post-List row, not the home page's card)
            IElement firstRow = page.FindAll("div.card.mb-4")[0];
            firstRow.QuerySelector("div.col-md-4 img").Should().NotBeNull();
            firstRow.QuerySelector("div.col-md-8 h3.card-title").Should().NotBeNull();
            firstRow.QuerySelector("ul.nav-divider").Should().NotBeNull();

            page.FindAll("div.col-lg-4 .card-header").Cast<IElement>()
                .Select(header => header.TextContent.Trim())
                .Should().Equal("About", "Trending", "Topics");
        }

        [Fact]
        public void ShouldDropTheBannerTheSampleListCarries()
        {
            // when
            IRenderedComponent<Search> page = Render<Search>();
            SearchFor(page, "grace");

            // then (the search box stands where Post-List has its hero image)
            page.FindAll("section.bg-light").Should().BeEmpty();
            page.FindAll("nav[aria-label='breadcrumb']").Should().BeEmpty();
        }

        [Fact]
        public void ShouldLinkEveryResultToThePostSinglePage()
        {
            // given
            IRenderedComponent<Search> page = Render<Search>();

            // when
            SearchFor(page, "grace");

            // then
            page.FindAll("h3.card-title a").Cast<IElement>()
                .Select(link => link.GetAttribute("href"))
                .Should().OnlyContain(href => href!.StartsWith("Post-Single/"));
        }

        [Fact]
        public void ShouldNarrowTheResultsByTheAdvancedCategory()
        {
            // given
            IRenderedComponent<Search> page = Render<Search>();
            SearchFor(page, "grace");

            // when
            page.Find("button[type='button']").Click();
            page.Find("select#searchCategory").Change("Devotional");
            page.Find("form").Submit();

            // then (the advanced options do filter — a control that did nothing would be worse
            // than no control)
            ResultTitles(page).Should().Equal("Walking daily in grace");
        }

        [Fact]
        public void ShouldTakeTheAuthorAsFreeTextNotAList()
        {
            // given (there is no useful upper bound on how many authors a site has)
            IRenderedComponent<Search> page = Render<Search>();
            SearchFor(page, "grace");
            page.Find("button[type='button']").Click();

            // then
            page.FindAll("select#searchAuthor").Should().BeEmpty();
            page.Find("input#searchAuthor").GetAttribute("type").Should().Be("text");
        }

        [Fact]
        public void ShouldNarrowTheResultsOnPartOfTheAuthorName()
        {
            // given
            IRenderedComponent<Search> page = Render<Search>();
            SearchFor(page, "grace");

            // when (free text, so a fragment has to be enough)
            page.Find("button[type='button']").Click();
            page.Find("input#searchAuthor").Input("man");
            page.Find("form").Submit();

            // then
            ResultTitles(page).Should().Equal("The armor of God, piece by piece");
        }

        [Fact]
        public void ShouldMatchAnyOfTheTagsByDefault()
        {
            // given
            IRenderedComponent<Search> page = Render<Search>();
            SearchFor(page, "grace");
            page.Find("button[type='button']").Click();

            // when (two tags carried by different posts)
            AddTag(page, "prayer");
            AddTag(page, "science");
            page.Find("form").Submit();

            // then
            page.Find("#tagMatchAny").HasAttribute("checked").Should().BeTrue();

            ResultTitles(page).Should().Equal(
                "NASA Proves The Bible Is True", "The armor of God, piece by piece");
        }

        [Fact]
        public void ShouldMatchAllOfTheTagsWhenAskedTo()
        {
            // given
            IRenderedComponent<Search> page = Render<Search>();
            SearchFor(page, "grace");
            page.Find("button[type='button']").Click();
            AddTag(page, "grace");
            AddTag(page, "discipleship");

            // when
            page.Find("#tagMatchAll").Change(true);
            page.Find("form").Submit();

            // then (only one post carries both; "grace" alone would have matched two)
            ResultTitles(page).Should().Equal("Walking daily in grace");
        }

        [Fact]
        public void ShouldStopFilteringOnATagOnceItsCrossIsClicked()
        {
            // given
            IRenderedComponent<Search> page = Render<Search>();
            SearchFor(page, "grace");
            page.Find("button[type='button']").Click();
            AddTag(page, "science");
            page.Find("form").Submit();

            ResultTitles(page).Should().Equal("NASA Proves The Bible Is True");

            // when
            page.Find("button.g2h-tag-remove").Click();
            page.Find("form").Submit();

            // then
            ResultTitles(page).Should().HaveCount(SampleContent.Latest.Count);
        }

        [Fact]
        public void ShouldSayWhenTheAdvancedOptionsLeaveNothing()
        {
            // given (a category and an author that no single post carries together)
            IRenderedComponent<Search> page = Render<Search>();
            SearchFor(page, "grace");

            // when
            page.Find("button[type='button']").Click();
            page.Find("select#searchCategory").Change("Devotional");
            page.Find("input#searchAuthor").Input("Amanda");
            page.Find("form").Submit();

            // then
            page.Find("div.alert-info").TextContent.Should().Contain("Nothing matched");
        }

        [Fact]
        public void ShouldNotShowPaginationForASinglePageOfResults()
        {
            // when (four demo posts, five to a page)
            IRenderedComponent<Search> page = Render<Search>();
            SearchFor(page, "grace");

            // then
            SampleContent.Latest.Count.Should().BeLessThan(SearchBase.ResultsPerPage);
            page.FindAll("ul.pagination").Should().BeEmpty();
        }
    }
}
