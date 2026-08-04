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
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.Pages.Admin;
using Glory2Him.WebApp.Models.Views.Posts;
using Glory2Him.WebApp.Services.Views.Posts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Glory2Him.WebApp.Tests.Unit.Components.Pages.Admin
{
    public class PostDetailPageComponentTests : BunitContext
    {
        private const string PostId = "7";

        private readonly Mock<IPostsViewService> postsViewServiceMock;

        public PostDetailPageComponentTests()
        {
            this.postsViewServiceMock = new Mock<IPostsViewService>();
            Services.AddSingleton(this.postsViewServiceMock.Object);
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static PostView CreatePost() =>
            new PostView
            {
                Id = PostId,
                Title = "An existing post",
                Slug = "an-existing-post",
                Excerpt = "Some excerpt",
                Category = "Faith",
                CategoryBadgeCss = "text-bg-primary",
                AuthorName = "Glory 2 Him",
                PublishedDate = new DateTimeOffset(2022, 2, 18, 0, 0, 0, TimeSpan.Zero),
            };

        private IRenderedComponent<PostDetailPage> RenderNewPostPage() =>
            Render<PostDetailPage>();

        private IRenderedComponent<PostDetailPage> RenderEditPage() =>
            Render<PostDetailPage>(parameters =>
                parameters.Add(page => page.PostId, PostId));

        [Fact]
        public void ShouldRenderTheNewPostEditorWithoutLoadingAnything()
        {
            // when
            IRenderedComponent<PostDetailPage> renderedPage = RenderNewPostPage();

            // then
            renderedPage.Find("h1").TextContent.Should().Be("New post");

            this.postsViewServiceMock.Verify(service =>
                service.RetrievePostByIdAsync(It.IsAny<string>()),
                    Times.Never);
        }

        [Fact]
        public void ShouldLoadThePostWhenEditing()
        {
            // given
            PostView post = CreatePost();

            this.postsViewServiceMock.Setup(service =>
                service.RetrievePostByIdAsync(PostId))
                    .ReturnsAsync(post);

            // when
            IRenderedComponent<PostDetailPage> renderedPage = RenderEditPage();

            // then
            renderedPage.Find("h1").TextContent.Should().Be("Edit post");
            renderedPage.Markup.Should().Contain(post.Title);
        }

        [Fact]
        public void ShouldAddThePostAndReturnToTheListWhenSavingANewPost()
        {
            // given
            this.postsViewServiceMock.Setup(service =>
                service.AddPostAsync(It.IsAny<PostView>()))
                    .ReturnsAsync(new PostView());

            IRenderedComponent<PostDetailPage> renderedPage = RenderNewPostPage();
            var navigationManager = Services.GetRequiredService<NavigationManager>();

            // when
            renderedPage.FindAll("button")
                .First(button => button.TextContent.Trim() == "Save post")
                .Click();

            // then
            this.postsViewServiceMock.Verify(service =>
                service.AddPostAsync(It.IsAny<PostView>()),
                    Times.Once);

            navigationManager.Uri.Should().EndWith("Admin/Posts");
        }

        [Fact]
        public void ShouldModifyThePostWhenSavingAnExistingPost()
        {
            // given
            this.postsViewServiceMock.Setup(service =>
                service.RetrievePostByIdAsync(PostId))
                    .ReturnsAsync(CreatePost());

            this.postsViewServiceMock.Setup(service =>
                service.ModifyPostAsync(It.IsAny<PostView>()))
                    .ReturnsAsync(new PostView());

            IRenderedComponent<PostDetailPage> renderedPage = RenderEditPage();

            // when
            renderedPage.FindAll("button")
                .First(button => button.TextContent.Trim() == "Save post")
                .Click();

            // then
            this.postsViewServiceMock.Verify(service =>
                service.ModifyPostAsync(It.Is<PostView>(post => post.Id == PostId)),
                    Times.Once);

            this.postsViewServiceMock.Verify(service =>
                service.AddPostAsync(It.IsAny<PostView>()),
                    Times.Never);
        }

        [Fact]
        public void ShouldDeleteThePostAndReturnToTheListWhenConfirmed()
        {
            // given
            this.postsViewServiceMock.Setup(service =>
                service.RetrievePostByIdAsync(PostId))
                    .ReturnsAsync(CreatePost());

            this.postsViewServiceMock.Setup(service =>
                service.RemovePostAsync(PostId))
                    .Returns(ValueTask.CompletedTask);

            IRenderedComponent<PostDetailPage> renderedPage = RenderEditPage();
            var navigationManager = Services.GetRequiredService<NavigationManager>();

            renderedPage.FindAll("button")
                .First(button => button.TextContent.Trim() == "Delete post")
                .Click();

            // when (confirm)
            renderedPage.Find("div.modal-footer button.btn-danger").Click();

            // then
            this.postsViewServiceMock.Verify(service =>
                service.RemovePostAsync(PostId),
                    Times.Once);

            navigationManager.Uri.Should().EndWith("Admin/Posts");
        }

        [Fact]
        public void ShouldNotOfferDeleteWhenCreatingANewPost()
        {
            // when
            IRenderedComponent<PostDetailPage> renderedPage = RenderNewPostPage();

            // then
            renderedPage.FindAll("button")
                .Should().NotContain(button => button.TextContent.Trim() == "Delete post");
        }

        [Fact]
        public void ShouldRenderErrorWhenThePostCannotBeLoaded()
        {
            // given
            this.postsViewServiceMock.Setup(service =>
                service.RetrievePostByIdAsync(PostId))
                    .ThrowsAsync(new Exception("boom"));

            // when
            IRenderedComponent<PostDetailPage> renderedPage = RenderEditPage();

            // then
            renderedPage.Find("div.alert-danger").TextContent
                .Should().Contain("could not load this post");
        }
    }
}
