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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.WebApp.Models.Views.Posts;

namespace Glory2Him.WebApp.Tests.Unit.Services.Views.Posts
{
    public partial class PostsViewServiceTests
    {
        [Fact]
        public async Task ShouldAddPostAndAssignIdAndSlug()
        {
            // given
            var newPost = new PostView
            {
                Title = "A Fresh Word Of Hope",
                Excerpt = "Encouragement",
                Category = "Hope",
            };

            // when
            PostView addedPost = await this.postsViewService.AddPostAsync(newPost);

            List<PostView> allPosts = await this.postsViewService.RetrieveAllPostsAsync();

            // then
            addedPost.Id.Should().NotBeNullOrWhiteSpace();
            addedPost.Slug.Should().Be("a-fresh-word-of-hope");
            allPosts.Should().Contain(post => post.Id == addedPost.Id);

            // cleanup (the store is static/shared across tests)
            await this.postsViewService.RemovePostAsync(addedPost.Id);
        }

        [Fact]
        public async Task ShouldModifyExistingPost()
        {
            // given
            PostView addedPost = await this.postsViewService.AddPostAsync(
                new PostView { Title = "Original Title", Category = "Faith" });

            addedPost.Title = "Updated Title";

            // when
            PostView modifiedPost = await this.postsViewService.ModifyPostAsync(addedPost);

            PostView reloaded =
                await this.postsViewService.RetrievePostByIdAsync(addedPost.Id);

            // then
            modifiedPost.Title.Should().Be("Updated Title");
            reloaded.Title.Should().Be("Updated Title");
            reloaded.Slug.Should().Be("updated-title");

            // cleanup
            await this.postsViewService.RemovePostAsync(addedPost.Id);
        }

        [Fact]
        public async Task ShouldRemovePost()
        {
            // given
            PostView addedPost = await this.postsViewService.AddPostAsync(
                new PostView { Title = "To Be Removed", Category = "Faith" });

            // when
            await this.postsViewService.RemovePostAsync(addedPost.Id);

            List<PostView> allPosts = await this.postsViewService.RetrieveAllPostsAsync();

            // then
            allPosts.Should().NotContain(post => post.Id == addedPost.Id);
        }
    }
}
