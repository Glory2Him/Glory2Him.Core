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
using Glory2Him.WebApp.Components.CoreUI;
using Glory2Him.WebApp.Models.Views.Posts;
using Glory2Him.WebApp.Models.Views.Users;
using Glory2Him.WebApp.Services.Views.Posts;
using Glory2Him.WebApp.Services.Views.Users;
using Microsoft.AspNetCore.Components;

namespace Glory2Him.WebApp.Components.Pages
{
    public class DashboardBase : ComponentBase
    {
        [Inject]
        public IPostsViewService PostsViewService { get; set; } = default!;

        [Inject]
        public IUsersViewService UsersViewService { get; set; } = default!;

        protected bool IsLoading { get; private set; } = true;

        protected bool HasError { get; private set; }

        protected string ErrorMessage { get; private set; } = string.Empty;

        protected List<PostView> Posts { get; private set; } = new List<PostView>();

        protected int PostCount => Posts.Count;

        protected int UserCount { get; private set; }

        protected int FeaturedCount => Posts.Count(post => post.IsFeatured);

        protected int CategoryCount =>
            Posts.Select(post => post.Category).Distinct().Count();

        // Posts-per-category, shaped for the CoreUI Chart (ApexCharts donut).
        protected IReadOnlyList<string> CategoryLabels { get; private set; } =
            new List<string>();

        protected IReadOnlyList<ChartDataset> CategoryDatasets { get; private set; } =
            new List<ChartDataset>();

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Posts = await PostsViewService.RetrieveAllPostsAsync();
                List<UserView> users = await UsersViewService.RetrieveAllUsersAsync();
                UserCount = users.Count;
                BuildCategoryChart();
            }
            catch
            {
                HasError = true;
                ErrorMessage = "We could not load the dashboard right now. Please try again later.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void BuildCategoryChart()
        {
            var grouped = Posts
                .GroupBy(post => post.Category)
                .Select(group => new { Category = group.Key, Count = group.Count() })
                .OrderByDescending(entry => entry.Count)
                .ToList();

            var palette = new[]
            {
                "#2163e8", "#0cbc87", "#d6293e", "#f7c32e", "#4f42b5", "#0d6efd"
            };

            CategoryLabels = grouped.Select(entry => entry.Category).ToList();

            CategoryDatasets = new List<ChartDataset>
            {
                new ChartDataset
                {
                    Label = "Posts",
                    Data = grouped.Select(entry => (double)entry.Count).ToList(),
                    Colors = grouped.Select((entry, index) =>
                        palette[index % palette.Length]).ToList(),
                }
            };
        }
    }
}
