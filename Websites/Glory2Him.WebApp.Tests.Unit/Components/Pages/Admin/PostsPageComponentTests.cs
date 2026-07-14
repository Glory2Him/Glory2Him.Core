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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.Pages.Admin;
using Glory2Him.WebApp.Models.Views.Posts;
using Glory2Him.WebApp.Services.Views.Posts;
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
        public void ShouldOpenCreateModalWhenNewPostClicked()
        {
            // given
            this.postsViewServiceMock.Setup(service =>
                service.RetrieveAllPostsAsync())
                    .ReturnsAsync(new List<PostView>());

            IRenderedComponent<PostsPage> renderedPage = Render<PostsPage>();

            // when
            renderedPage.FindAll("button")
                .First(button => button.TextContent.Contains("New post"))
                .Click();

            // then
            renderedPage.FindAll("div.modal").Should().NotBeEmpty();
            renderedPage.Markup.Should().Contain("New post");
        }

        [Fact]
        public void ShouldCallAddPostWhenSavingNewPost()
        {
            // given
            this.postsViewServiceMock.Setup(service =>
                service.RetrieveAllPostsAsync())
                    .ReturnsAsync(new List<PostView>());

            this.postsViewServiceMock.Setup(service =>
                service.AddPostAsync(It.IsAny<PostView>()))
                    .ReturnsAsync(new PostView());

            IRenderedComponent<PostsPage> renderedPage = Render<PostsPage>();

            renderedPage.FindAll("button")
                .First(button => button.TextContent.Contains("New post"))
                .Click();

            // when (Save post — button in the modal footer)
            renderedPage.FindAll("div.modal-footer button")
                .First(button => button.TextContent.Contains("Save post"))
                .Click();

            // then
            this.postsViewServiceMock.Verify(service =>
                service.AddPostAsync(It.IsAny<PostView>()),
                    Times.Once);
        }

        [Fact]
        public void ShouldCallRemovePostWhenDeletionConfirmed()
        {
            // given
            List<PostView> posts = CreatePosts(count: 1);

            this.postsViewServiceMock.Setup(service =>
                service.RetrieveAllPostsAsync())
                    .ReturnsAsync(posts);

            this.postsViewServiceMock.Setup(service =>
                service.RemovePostAsync(posts[0].Id))
                    .Returns(ValueTask.CompletedTask);

            IRenderedComponent<PostsPage> renderedPage = Render<PostsPage>();

            renderedPage.FindAll("button")
                .First(button => button.TextContent.Trim() == "Delete")
                .Click();

            // when (confirm in the dialog)
            renderedPage.Find("div.modal-footer button.btn-danger").Click();

            // then
            this.postsViewServiceMock.Verify(service =>
                service.RemovePostAsync(posts[0].Id),
                    Times.Once);
        }
    }
}
