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

using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.CoreUI;
using Glory2Him.WebApp.Models.Views.Posts;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public partial class PostCardComponentTests
    {
        [Fact]
        public void ShouldRenderPostTitleAndCategory()
        {
            // given
            PostView randomPost = CreateRandomPost();

            // when
            IRenderedComponent<PostCardComponent> renderedCard =
                Render<PostCardComponent>(parameters =>
                    parameters.Add(card => card.Post, randomPost));

            // then
            renderedCard.Markup.Should().Contain(randomPost.Title);
            renderedCard.Markup.Should().Contain(randomPost.Category);
            renderedCard.Markup.Should().Contain(randomPost.AuthorName);
        }

        [Fact]
        public void ShouldLinkToPostSingleUsingSlug()
        {
            // given
            PostView randomPost = CreateRandomPost();
            string expectedHref = $"post-single/{randomPost.Slug}";

            // when
            IRenderedComponent<PostCardComponent> renderedCard =
                Render<PostCardComponent>(parameters =>
                    parameters.Add(card => card.Post, randomPost));

            // then
            renderedCard.Find("h4.card-title a").GetAttribute("href")
                .Should().Be(expectedHref);
        }

        [Fact]
        public void ShouldApplyCategoryBadgeCssClass()
        {
            // given
            PostView randomPost = CreateRandomPost();
            randomPost.CategoryBadgeCss = "text-bg-warning";

            // when
            IRenderedComponent<PostCardComponent> renderedCard =
                Render<PostCardComponent>(parameters =>
                    parameters.Add(card => card.Post, randomPost));

            // then
            renderedCard.Find("a.badge").ClassList.Should().Contain("text-bg-warning");
        }

        [Fact]
        public void ShouldNotRenderExcerptByDefault()
        {
            // given
            PostView randomPost = CreateRandomPost();

            // when
            IRenderedComponent<PostCardComponent> renderedCard =
                Render<PostCardComponent>(parameters =>
                    parameters.Add(card => card.Post, randomPost));

            // then
            renderedCard.FindAll("p.card-text").Should().BeEmpty();
        }

        [Fact]
        public void ShouldRenderExcerptWhenRequested()
        {
            // given
            PostView randomPost = CreateRandomPost();

            // when
            IRenderedComponent<PostCardComponent> renderedCard =
                Render<PostCardComponent>(parameters => parameters
                    .Add(card => card.Post, randomPost)
                    .Add(card => card.ShowExcerpt, true));

            // then
            renderedCard.Find("p.card-text").TextContent.Should().Contain(randomPost.Excerpt);
        }
    }
}
