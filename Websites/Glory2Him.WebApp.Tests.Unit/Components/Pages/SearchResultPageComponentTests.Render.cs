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

using System.Collections.Generic;
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.Pages;
using Glory2Him.WebApp.Models.Views.Posts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Glory2Him.WebApp.Tests.Unit.Components.Pages
{
    public partial class SearchResultPageComponentTests
    {
        private void NavigateToSearch(string query)
        {
            var navigationManager = Services.GetRequiredService<NavigationManager>();

            navigationManager.NavigateTo(
                navigationManager.GetUriWithQueryParameter("q", query));
        }

        [Fact]
        public void ShouldFilterPostsByQueryAcrossTitleAndCategory()
        {
            // given
            PostView matchingPost = CreatePost(title: "Hope in the morning", category: "Faith");
            PostView otherPost = CreatePost(title: "Gadgets review", category: "Tech");

            this.postsViewServiceMock.Setup(service =>
                service.RetrieveAllPostsAsync())
                    .ReturnsAsync(new List<PostView> { matchingPost, otherPost });

            NavigateToSearch("hope");

            // when
            IRenderedComponent<SearchResult> renderedPage = Render<SearchResult>();

            // then
            renderedPage.Markup.Should().Contain(matchingPost.Title);
            renderedPage.Markup.Should().NotContain(otherPost.Title);
        }

        [Fact]
        public void ShouldRenderEmptyStateWhenNoResultsMatch()
        {
            // given
            PostView post = CreatePost(title: "Hope in the morning", category: "Faith");

            this.postsViewServiceMock.Setup(service =>
                service.RetrieveAllPostsAsync())
                    .ReturnsAsync(new List<PostView> { post });

            NavigateToSearch("zzzznomatch");

            // when
            IRenderedComponent<SearchResult> renderedPage = Render<SearchResult>();

            // then
            renderedPage.Find("div.alert-info").Should().NotBeNull();
        }

        [Fact]
        public void ShouldReturnAllPostsWhenQueryIsEmpty()
        {
            // given
            PostView firstPost = CreatePost(title: "Hope in the morning", category: "Faith");
            PostView secondPost = CreatePost(title: "Gadgets review", category: "Tech");

            this.postsViewServiceMock.Setup(service =>
                service.RetrieveAllPostsAsync())
                    .ReturnsAsync(new List<PostView> { firstPost, secondPost });

            // when
            IRenderedComponent<SearchResult> renderedPage = Render<SearchResult>();

            // then
            renderedPage.Markup.Should().Contain(firstPost.Title);
            renderedPage.Markup.Should().Contain(secondPost.Title);
        }
    }
}
