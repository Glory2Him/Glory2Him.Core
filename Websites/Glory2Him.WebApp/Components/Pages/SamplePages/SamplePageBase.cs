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

namespace Glory2Him.WebApp.Components.Pages.SamplePages
{
    // Shared loader for the layout demos. Adds the slicing helpers the layouts need on top of the
    // usual posts fetch, so each sample page stays markup and nothing else.
    public class SamplePageBase : CategoriesBase
    {
        protected PostView? Lead =>
            Posts.FirstOrDefault();

        protected IEnumerable<PostView> AfterLead =>
            Posts.Skip(1);

        // The demo store holds only a handful of posts, but a masonry or four-across grid needs
        // more tiles than that to read as a real layout — repeat the set to fill the shape rather
        // than shipping a half-empty grid.
        protected IReadOnlyList<PostView> Fill(int count)
        {
            if (Posts.Count == 0 || count <= 0)
            {
                return new List<PostView>();
            }

            return Enumerable.Range(0, count)
                .Select(index => Posts[index % Posts.Count])
                .ToList();
        }

        protected IReadOnlyList<PostView> Take(int count) =>
            Posts.Take(count).ToList();
    }
}
