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
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.Pages.Admin;
using Glory2Him.WebApp.Models.Views.Posts;
using Glory2Him.WebApp.Services.Views.Posts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Unit.Components.Pages.Admin
{
    public class PostsPageComponentTests : BunitContext
    {
        private readonly Mock<IPostsViewService> postsViewServiceMock;

        public PostsPageComponentTests()
        {
            this.postsViewServiceMock = new Mock<IPostsViewService>();
            Services.AddSingleton(this.postsViewServiceMock.Object);
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static string GetRandomString() =>
            new MnemonicString().GetValue();

        private static List<PostView> CreatePosts(int count) =>
            Enumerable.Range(0, count).Select(index => new PostView
            {
                Id = index.ToString(),
                Title = GetRandomString(),
                Slug = GetRandomString(),
                Category = "Faith",
                CategoryBadgeCss = "text-bg-primary",
                AuthorName = GetRandomString(),
                PublishedDate = new DateTimeOffset(2022, 2, 18, 0, 0, 0, TimeSpan.Zero),
            }).ToList();

        [Fact]
        public void ShouldRenderPostsInDataTable()
        {
            // given
            List<PostView> posts = CreatePosts(count: 2);

            this.postsViewServiceMock.Setup(service =>
                service.RetrieveAllPostsAsync())
                    .ReturnsAsync(posts);

            // when
            IRenderedComponent<PostsPage> renderedPage = Render<PostsPage>();

            // then
            renderedPage.FindAll("thead th")[0].TextContent.Trim().Should().Be("Title");
            renderedPage.Markup.Should().Contain(posts[0].Title);
        }

        [Fact]
        public void ShouldNavigateToTheNewPostPageWhenNewPostClicked()
        {
            // given
            this.postsViewServiceMock.Setup(service =>
                service.RetrieveAllPostsAsync())
                    .ReturnsAsync(new List<PostView>());

            IRenderedComponent<PostsPage> renderedPage = Render<PostsPage>();
            var navigationManager = Services.GetRequiredService<NavigationManager>();

            // when
            renderedPage.FindAll("button")
                .First(button => button.TextContent.Contains("New post"))
                .Click();

            // then
            navigationManager.Uri.Should().EndWith("Admin/Posts/New");
        }

        [Fact]
        public void ShouldNavigateToThePostDetailPageWhenManageClicked()
        {
            // given
            List<PostView> posts = CreatePosts(count: 1);

            this.postsViewServiceMock.Setup(service =>
                service.RetrieveAllPostsAsync())
                    .ReturnsAsync(posts);

            IRenderedComponent<PostsPage> renderedPage = Render<PostsPage>();
            var navigationManager = Services.GetRequiredService<NavigationManager>();

            // when
            renderedPage.FindAll("button")
                .First(button => button.TextContent.Trim() == "Manage")
                .Click();

            // then
            navigationManager.Uri.Should().EndWith($"Admin/Posts/{posts[0].Id}");
        }

        [Fact]
        public void ShouldNotEditPostsFromTheListItself()
        {
            // given
            List<PostView> posts = CreatePosts(count: 1);

            this.postsViewServiceMock.Setup(service =>
                service.RetrieveAllPostsAsync())
                    .ReturnsAsync(posts);

            // when
            IRenderedComponent<PostsPage> renderedPage = Render<PostsPage>();

            // then (editing a post happens on its own page, never in a modal over the list)
            renderedPage.FindAll("div.modal").Should().BeEmpty();
        }
    }
}
