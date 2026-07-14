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
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.Pages;
using Glory2Him.WebApp.Models.Views.Posts;
using Glory2Him.WebApp.Models.Views.Posts.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Components.Pages
{
    public partial class HomePageComponentTests
    {
        [Fact]
        public void ShouldShowSpinnerWhileLoading()
        {
            // given
            var pendingSource = new TaskCompletionSource<List<PostView>>();

            this.postsViewServiceMock.Setup(service =>
                service.RetrieveAllPostsAsync())
                    .Returns(new ValueTask<List<PostView>>(pendingSource.Task));

            // when
            IRenderedComponent<Home> renderedPage = Render<Home>();

            // then
            renderedPage.FindAll("div.spinner-border").Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void ShouldRenderPostsWhenLoaded()
        {
            // given
            List<PostView> posts = CreateRandomPosts(count: 3, featured: true);

            this.postsViewServiceMock.Setup(service =>
                service.RetrieveAllPostsAsync())
                    .ReturnsAsync(posts);

            // when
            IRenderedComponent<Home> renderedPage = Render<Home>();

            // then
            foreach (PostView post in posts)
            {
                renderedPage.Markup.Should().Contain(post.Title);
            }

            this.postsViewServiceMock.Verify(service =>
                service.RetrieveAllPostsAsync(),
                    Times.Once);
        }

        [Fact]
        public void ShouldRenderErrorAlertWhenServiceThrows()
        {
            // given
            var serviceException =
                new PostsViewServiceException(
                    message: "Service error",
                    innerException: new Xeption());

            this.postsViewServiceMock.Setup(service =>
                service.RetrieveAllPostsAsync())
                    .ThrowsAsync(serviceException);

            // when
            IRenderedComponent<Home> renderedPage = Render<Home>();

            // then
            renderedPage.Find("div.alert-danger").Should().NotBeNull();
        }

        [Fact]
        public void ShouldRenderEmptyStateWhenNoPosts()
        {
            // given
            this.postsViewServiceMock.Setup(service =>
                service.RetrieveAllPostsAsync())
                    .ReturnsAsync(new List<PostView>());

            // when
            IRenderedComponent<Home> renderedPage = Render<Home>();

            // then
            renderedPage.Find("div.alert-info").TextContent
                .Should().Contain("No posts");
        }
    }
}
