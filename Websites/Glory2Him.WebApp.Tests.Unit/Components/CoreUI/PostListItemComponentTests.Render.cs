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
using Glory2Him.WebApp.Models.Views.Posts;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public partial class PostListItemComponentTests
    {
        [Fact]
        public void ShouldRenderPostTitleExcerptAndReadTime()
        {
            // given
            PostView randomPost = CreateRandomPost();

            // when
            IRenderedComponent<PostListItemComponent> renderedItem =
                Render<PostListItemComponent>(parameters =>
                    parameters.Add(item => item.Post, randomPost));

            // then
            renderedItem.Markup.Should().Contain(randomPost.Title);
            renderedItem.Markup.Should().Contain(randomPost.Excerpt);
            renderedItem.Markup.Should().Contain($"{randomPost.ReadMinutes} min read");
        }

        [Fact]
        public void ShouldLinkToPostSingleUsingSlug()
        {
            // given
            PostView randomPost = CreateRandomPost();
            string expectedHref = $"Post-Single/{randomPost.Slug}";

            // when
            IRenderedComponent<PostListItemComponent> renderedItem =
                Render<PostListItemComponent>(parameters =>
                    parameters.Add(item => item.Post, randomPost));

            // then
            renderedItem.Find("h3.card-title a").GetAttribute("href")
                .Should().Be(expectedHref);
        }
    }
}
