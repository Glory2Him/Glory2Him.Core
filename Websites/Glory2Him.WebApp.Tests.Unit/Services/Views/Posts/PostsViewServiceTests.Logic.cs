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
using FluentAssertions;
using Glory2Him.WebApp.Models.Views.Posts;
using Moq;

namespace Glory2Him.WebApp.Tests.Unit.Services.Views.Posts
{
    public partial class PostsViewServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllPosts()
        {
            // given . when
            List<PostView> actualPosts =
                await this.postsViewService.RetrieveAllPostsAsync();

            // then
            actualPosts.Should().NotBeNullOrEmpty();
            actualPosts.Should().OnlyContain(post => !string.IsNullOrWhiteSpace(post.Title));

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrievePostByMatchingSlug()
        {
            // given
            List<PostView> allPosts =
                await this.postsViewService.RetrieveAllPostsAsync();

            PostView expectedPost = allPosts[1];

            // when
            PostView actualPost =
                await this.postsViewService.RetrievePostBySlugAsync(expectedPost.Slug);

            // then
            actualPost.Slug.Should().Be(expectedPost.Slug);
            actualPost.Title.Should().Be(expectedPost.Title);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldFallBackToFirstPostWhenSlugIsUnknown()
        {
            // given
            List<PostView> allPosts =
                await this.postsViewService.RetrieveAllPostsAsync();

            PostView expectedFallbackPost = allPosts[0];

            // when
            PostView actualPost =
                await this.postsViewService.RetrievePostBySlugAsync("does-not-exist");

            // then
            actualPost.Slug.Should().Be(expectedFallbackPost.Slug);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
