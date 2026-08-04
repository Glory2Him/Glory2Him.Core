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
using FluentAssertions;
using Glory2Him.WebApp.Components.CoreUI;
using Glory2Him.WebApp.Models.Views.Posts;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public class SampleLayoutComponentsTests : BunitContext
    {
        private static PostView CreatePost() =>
            new PostView
            {
                Id = "1",
                Title = "Hope in the morning",
                Slug = "hope-in-the-morning",
                Excerpt = "A short reflection.",
                ImageUrl = "assets/images/blog/01.jpg",
                Category = "Faith",
                CategoryBadgeCss = "text-bg-primary",
                AuthorName = "Joan Wallace",
                PublishedDate = new DateTimeOffset(2022, 2, 18, 0, 0, 0, TimeSpan.Zero),
                ReadMinutes = 4,
            };

        [Fact]
        public void ShouldRenderTheOverlayCardWithTitleOverTheImage()
        {
            // given
            PostView post = CreatePost();

            // when
            IRenderedComponent<PostOverlayCardComponent> rendered =
                Render<PostOverlayCardComponent>(parameters =>
                    parameters.Add(card => card.Post, post));

            // then
            rendered.Find("div.card").ClassList.Should().Contain("card-overlay-bottom");
            rendered.Find("div.card-img-overlay").Should().NotBeNull();
            rendered.Markup.Should().Contain(post.Title);
            rendered.Markup.Should().Contain(post.Category);
        }

        [Fact]
        public void ShouldSizeTheOverlayTitleFromTheSuppliedUtilityClass()
        {
            // given
            PostView post = CreatePost();

            // when
            IRenderedComponent<PostOverlayCardComponent> rendered =
                Render<PostOverlayCardComponent>(parameters => parameters
                    .Add(card => card.Post, post)
                    .Add(card => card.TitleCssClass, "display-6"));

            // then (the heading tag stays fixed; only the utility class changes)
            rendered.Find("h3").ClassList.Should().Contain("display-6");
        }

        [Theory]
        [InlineData(PostType.Video, "bi-play-fill")]
        [InlineData(PostType.Audio, "bi-mic-fill")]
        [InlineData(PostType.Gallery, "bi-images")]
        [InlineData(PostType.Quote, "bi-quote")]
        [InlineData(PostType.Standard, "bi-file-text-fill")]
        public void ShouldPickTheIconMatchingThePostType(PostType type, string expectedIcon)
        {
            // when
            IRenderedComponent<PostTypeBadgeComponent> rendered =
                Render<PostTypeBadgeComponent>(parameters =>
                    parameters.Add(badge => badge.Type, type));

            // then
            rendered.Find("i").ClassList.Should().Contain(expectedIcon);
            rendered.Find("span").GetAttribute("aria-label").Should().Be($"{type} post");
        }

        [Fact]
        public void ShouldRenderAPageLinkPerPageWhenNumbered()
        {
            // when
            IRenderedComponent<PaginationComponent> rendered =
                Render<PaginationComponent>(parameters => parameters
                    .Add(pagination => pagination.CurrentPage, 2)
                    .Add(pagination => pagination.TotalPages, 4));

            // then (four numbers plus the prev and next controls)
            rendered.FindAll("li.page-item").Should().HaveCount(6);
            rendered.Find("li.page-item.active").TextContent.Trim().Should().Be("2");
        }

        [Fact]
        public void ShouldDropThePageNumbersForThePrevNextVariant()
        {
            // when
            IRenderedComponent<PaginationComponent> rendered =
                Render<PaginationComponent>(parameters => parameters
                    .Add(pagination => pagination.TotalPages, 4)
                    .Add(pagination => pagination.Variant, PaginationVariant.PrevNext));

            // then
            rendered.FindAll("li.page-item").Should().HaveCount(2);
            rendered.Markup.Should().Contain("Prev");
            rendered.Markup.Should().Contain("Next");
        }

        [Fact]
        public void ShouldRaiseTheChangedCallbackWhenAPageIsClicked()
        {
            // given
            int? selectedPage = null;

            IRenderedComponent<PaginationComponent> rendered =
                Render<PaginationComponent>(parameters => parameters
                    .Add(pagination => pagination.CurrentPage, 1)
                    .Add(pagination => pagination.TotalPages, 3)
                    .Add(pagination => pagination.CurrentPageChanged,
                        page => selectedPage = page));

            // when
            rendered.FindAll("button.page-link")
                .First(button => button.TextContent.Trim() == "3")
                .Click();

            // then
            selectedPage.Should().Be(3);
        }

        [Fact]
        public void ShouldNotMoveBeyondTheFirstOrLastPage()
        {
            // when
            IRenderedComponent<PaginationComponent> rendered =
                Render<PaginationComponent>(parameters => parameters
                    .Add(pagination => pagination.CurrentPage, 1)
                    .Add(pagination => pagination.TotalPages, 1));

            // then (a single page leaves both arrows disabled)
            rendered.FindAll("li.page-item.disabled").Should().HaveCount(2);
        }

        [Fact]
        public void ShouldScoreEachReviewCriterionAsAProgressBar()
        {
            // given
            var criteria = new List<ReviewCriterion>
            {
                new ReviewCriterion("Writing", 4.0),
                new ReviewCriterion("Pacing", 2.5),
            };

            // when
            IRenderedComponent<ReviewRatingComponent> rendered =
                Render<ReviewRatingComponent>(parameters => parameters
                    .Add(review => review.OverallScore, 3.5)
                    .Add(review => review.MaximumScore, 5)
                    .Add(review => review.Criteria, criteria));

            // then
            rendered.FindAll("div.progress").Should().HaveCount(2);

            IElement pacing = rendered.FindAll("div.progress-bar")[1];
            pacing.GetAttribute("style").Should().Contain("50%");
        }

        [Fact]
        public void ShouldRenderTheMegaMenuAsAFullWidthDropdown()
        {
            // given
            var posts = new List<PostView> { CreatePost(), CreatePost() };

            // when
            IRenderedComponent<MegaMenuComponent> rendered =
                Render<MegaMenuComponent>(parameters => parameters
                    .Add(menu => menu.Title, "Lifestyle")
                    .Add(menu => menu.Posts, posts)
                    .Add(menu => menu.Topics, new List<string> { "Faith", "Hope" }));

            // then
            rendered.Find("li").ClassList.Should().Contain("dropdown-fullwidth");
            rendered.Find("a.dropdown-toggle").GetAttribute("data-bs-toggle").Should().Be("dropdown");
            rendered.FindAll("div.card").Should().HaveCount(2);
            rendered.Markup.Should().Contain("Browse topics");
        }
    }
}
