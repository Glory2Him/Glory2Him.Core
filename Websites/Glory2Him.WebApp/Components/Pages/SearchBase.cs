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

using Glory2Him.WebApp.Components.Pages.SamplePages;
using Glory2Him.WebApp.Models.Views.Posts;
using Microsoft.AspNetCore.Components;

namespace Glory2Him.WebApp.Components.Pages
{
    // The page opens as nothing but the search box; the post list appears once a search has been
    // run. This is a demo: whatever is typed, the same four posts come back — the ones the home
    // page lists. The text is not matched against anything, so the flow can be shown without a
    // search index behind it. The advanced options do narrow that set, so nothing on screen is a
    // control that does nothing.
    public class SearchBase : ComponentBase
    {
        public const int ResultsPerPage = 5;

        // Lets a link elsewhere land on /Search?q=anything with the results already showing.
        [SupplyParameterFromQuery(Name = "q")]
        public string? InitialQuery { get; set; }

        protected string Query { get; set; } = string.Empty;

        protected string SelectedCategory { get; set; } = string.Empty;

        protected string SelectedAuthor { get; set; } = string.Empty;

        protected IReadOnlyList<string> Tags { get; set; } = new List<string>();

        // Any by default: a reader adding a second tag is usually widening the net, not narrowing
        // it to posts that carry both.
        protected bool MatchAllTags { get; set; }

        protected bool HasSearched { get; private set; }

        protected IReadOnlyList<PostView> Results { get; private set; } = new List<PostView>();

        protected int CurrentPage { get; private set; } = 1;

        protected static IReadOnlyList<PostView> DemoPosts { get; } =
            SampleContent.Latest.Select(ToPostView).ToList();

        protected int TotalPages =>
            Math.Max(1, (int)Math.Ceiling(Results.Count / (double)ResultsPerPage));

        protected IReadOnlyList<PostView> PageOfResults =>
            Results.Skip((CurrentPage - 1) * ResultsPerPage).Take(ResultsPerPage).ToList();

        protected IReadOnlyList<PostView> Trending =>
            DemoPosts.Take(4).ToList();

        protected static IReadOnlyList<string> Categories =>
            DemoPosts.Select(post => post.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(category => category)
                .ToList();

        protected override void OnInitialized()
        {
            if (!string.IsNullOrWhiteSpace(InitialQuery))
            {
                Query = InitialQuery;

                Search();
            }
        }

        protected void OnQueryChanged(string query) =>
            Query = query;

        protected void OnCategoryChanged(ChangeEventArgs args) =>
            SelectedCategory = args.Value?.ToString() ?? string.Empty;

        protected void OnAuthorChanged(ChangeEventArgs args) =>
            SelectedAuthor = args.Value?.ToString() ?? string.Empty;

        protected void OnTagsChanged(IReadOnlyList<string> tags) =>
            Tags = tags;

        protected void OnTagMatchChanged(bool matchAll) =>
            MatchAllTags = matchAll;

        protected void OnPageChanged(int page) =>
            CurrentPage = page;

        protected void Search()
        {
            HasSearched = true;
            CurrentPage = 1;
            Results = DemoPosts.Where(Matches).ToList();
        }

        // Query deliberately absent: the demo always returns the set, whatever was typed.
        private bool Matches(PostView post)
        {
            bool matchesCategory =
                string.IsNullOrWhiteSpace(SelectedCategory)
                    || string.Equals(
                        post.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase);

            // Contains, not equals: the author box is free text, so a surname or a first name has
            // to be enough to find someone.
            bool matchesAuthor =
                string.IsNullOrWhiteSpace(SelectedAuthor)
                    || post.AuthorName.Contains(
                        SelectedAuthor.Trim(), StringComparison.OrdinalIgnoreCase);

            return matchesCategory && matchesAuthor && MatchesTags(post);
        }

        private bool MatchesTags(PostView post)
        {
            if (Tags.Count == 0)
            {
                return true;
            }

            bool Carries(string tag) =>
                post.Tags.Any(posted =>
                    string.Equals(posted, tag, StringComparison.OrdinalIgnoreCase));

            return MatchAllTags
                ? Tags.All(Carries)
                : Tags.Any(Carries);
        }

        private static PostView ToPostView(SamplePost post) =>
            new PostView
            {
                Id = post.Slug,
                Title = post.Title,
                Slug = post.Slug,
                Excerpt = post.Excerpt,
                ImageUrl = post.ImageUrl,
                Category = post.Category,
                CategoryBadgeCss = post.CategoryBadgeCss,
                AuthorName = post.AuthorName,
                AuthorImageUrl = post.AuthorImageUrl,
                PublishedDate = post.PublishedDate,
                ReadMinutes = post.ReadMinutes,
                IsFeatured = post.IsFeatured,
                Tags = post.Tags,
            };
    }
}
