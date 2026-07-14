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
using Glory2Him.WebApp.Models.Views.Posts;
using Glory2Him.WebApp.Services.Views.Posts;
using Microsoft.AspNetCore.Components;

namespace Glory2Him.WebApp.Components.Pages
{
    public class HomeBase : ComponentBase
    {
        [Inject]
        public IPostsViewService PostsViewService { get; set; } = default!;

        protected bool IsLoading { get; private set; } = true;

        protected bool HasError { get; private set; }

        protected string ErrorMessage { get; private set; } = string.Empty;

        protected List<PostView> Posts { get; private set; } = new List<PostView>();

        protected IEnumerable<PostView> FeaturedPosts => Posts.Where(post => post.IsFeatured);

        protected IEnumerable<PostView> LatestPosts => Posts;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Posts = await PostsViewService.RetrieveAllPostsAsync();
            }
            catch
            {
                HasError = true;
                ErrorMessage = "We could not load posts right now. Please try again later.";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
