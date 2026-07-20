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

using Glory2Him.WebApp.Models.Views.Posts;

namespace Glory2Him.WebApp.Services.Views.Posts
{
    public interface IPostsViewService
    {
        ValueTask<List<PostView>> RetrieveAllPostsAsync();
        ValueTask<PostView> RetrievePostBySlugAsync(string slug);
        ValueTask<PostView> RetrievePostByIdAsync(string id);
        ValueTask<PostView> AddPostAsync(PostView post);
        ValueTask<PostView> ModifyPostAsync(PostView post);
        ValueTask RemovePostAsync(string id);
    }
}
