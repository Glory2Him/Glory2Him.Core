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
using Glory2Him.WebApp.Components.Pages;
using Glory2Him.WebApp.Models.Views.Posts;
using Glory2Him.WebApp.Models.Views.Posts.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Components.Pages
{
    public partial class PostSinglePageComponentTests
    {
        [Fact]
        public void ShouldRetrievePostBySlugAndRenderIt()
        {
            // given
            PostView post = CreateRandomPost();

            this.postsViewServiceMock.Setup(service =>
                service.RetrievePostBySlugAsync(post.Slug))
                    .ReturnsAsync(post);

            // when
            IRenderedComponent<PostSingle> renderedPage =
                Render<PostSingle>(parameters =>
                    parameters.Add(page => page.Slug, post.Slug));

            // then
            renderedPage.Find("h1").TextContent.Should().Contain(post.Title);
            renderedPage.Markup.Should().Contain(post.Category);

            this.postsViewServiceMock.Verify(service =>
                service.RetrievePostBySlugAsync(post.Slug),
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
                service.RetrievePostBySlugAsync(It.IsAny<string>()))
                    .ThrowsAsync(serviceException);

            // when
            IRenderedComponent<PostSingle> renderedPage =
                Render<PostSingle>(parameters =>
                    parameters.Add(page => page.Slug, "any-slug"));

            // then
            renderedPage.Find("div.alert-danger").Should().NotBeNull();
        }
    }
}
