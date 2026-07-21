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
using Glory2Him.WebApp.Services.Views.Posts;
using Microsoft.AspNetCore.Components;

namespace Glory2Him.WebApp.Components.Pages
{
    public class TagBase : ComponentBase
    {
        [Inject]
        public IPostsViewService PostsViewService { get; set; } = default!;

        [SupplyParameterFromQuery(Name = "name")]
        public string? TagName { get; set; }

        protected bool IsLoading { get; private set; } = true;

        protected bool HasError { get; private set; }

        protected string ErrorMessage { get; private set; } = string.Empty;

        protected List<PostView> Posts { get; private set; } = new List<PostView>();

        protected IReadOnlyList<string> Tags { get; private set; } = new List<string>();

        protected string ActiveTag => string.IsNullOrWhiteSpace(TagName) ? "All" : TagName;

        protected override async Task OnParametersSetAsync()
        {
            IsLoading = true;
            HasError = false;

            try
            {
                List<PostView> allPosts = await PostsViewService.RetrieveAllPostsAsync();

                Tags = allPosts
                    .Select(post => post.Category)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(tag => tag)
                    .ToList();

                Posts = string.IsNullOrWhiteSpace(TagName)
                    ? allPosts
                    : allPosts
                        .Where(post => string.Equals(
                            post.Category, TagName, StringComparison.OrdinalIgnoreCase))
                        .ToList();
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
