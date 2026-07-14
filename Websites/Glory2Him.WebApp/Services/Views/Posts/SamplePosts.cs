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

using System;
using System.Collections.Generic;
using Glory2Him.WebApp.Models.Views.Posts;

namespace Glory2Him.WebApp.Services.Views.Posts
{
    // Static sample content that mirrors the Blogzine demo. This stands in for real,
    // modelled content until domain pages replace these placeholder posts. The list is mutable so
    // the admin CRUD demo can add/edit/remove posts for the lifetime of the app.
    internal static class SamplePosts
    {
        // Simple monotonic id source for newly created demo posts.
        private static int nextId = 100;

        public static string NextId() =>
            System.Threading.Interlocked.Increment(ref nextId).ToString();

        public static readonly List<PostView> All = new()
        {
            new PostView
            {
                Id = "1",
                Title = "7 common mistakes everyone makes while traveling",
                Slug = "common-mistakes-while-traveling",
                Excerpt = "Traveling is one of life's great joys, but small missteps can turn a "
                    + "dream trip into a stressful one. Here are the mistakes to avoid.",
                ImageUrl = "assets/images/blog/16by9/big/01.jpg",
                Category = "Travel",
                CategoryBadgeCss = "text-bg-primary",
                AuthorName = "Joan Wallace",
                AuthorImageUrl = "assets/images/avatar/01.jpg",
                PublishedDate = new DateTimeOffset(2022, 2, 18, 0, 0, 0, TimeSpan.Zero),
                ReadMinutes = 5,
                IsFeatured = true,
            },
            new PostView
            {
                Id = "2",
                Title = "12 worst types of business accounts you follow on Twitter",
                Slug = "worst-business-accounts-on-twitter",
                Excerpt = "Not every account deserves your attention. We break down the profiles "
                    + "that add noise instead of value to your feed.",
                ImageUrl = "assets/images/blog/16by9/big/02.jpg",
                Category = "Business",
                CategoryBadgeCss = "text-bg-warning",
                AuthorName = "Lori Stevens",
                AuthorImageUrl = "assets/images/avatar/02.jpg",
                PublishedDate = new DateTimeOffset(2022, 6, 3, 0, 0, 0, TimeSpan.Zero),
                ReadMinutes = 4,
                IsFeatured = true,
            },
            new PostView
            {
                Id = "3",
                Title = "Skills that you can learn from business",
                Slug = "skills-you-can-learn-from-business",
                Excerpt = "The discipline of running a business teaches lessons that carry over "
                    + "into every part of life. Here is what stands out.",
                ImageUrl = "assets/images/blog/16by9/big/03.jpg",
                Category = "Tech",
                CategoryBadgeCss = "text-bg-success",
                AuthorName = "Judy Nguyen",
                AuthorImageUrl = "assets/images/avatar/03.jpg",
                PublishedDate = new DateTimeOffset(2022, 9, 7, 0, 0, 0, TimeSpan.Zero),
                ReadMinutes = 6,
                IsFeatured = false,
            },
            new PostView
            {
                Id = "4",
                Title = "The unconventional guide to a healthier morning routine",
                Slug = "unconventional-guide-healthier-morning-routine",
                Excerpt = "Mornings set the tone for the day. A few small, sustainable habits can "
                    + "make a bigger difference than any dramatic overhaul.",
                ImageUrl = "assets/images/blog/16by9/big/04.jpg",
                Category = "Lifestyle",
                CategoryBadgeCss = "text-bg-info",
                AuthorName = "Louis Ferguson",
                AuthorImageUrl = "assets/images/avatar/04.jpg",
                PublishedDate = new DateTimeOffset(2022, 10, 12, 0, 0, 0, TimeSpan.Zero),
                ReadMinutes = 3,
                IsFeatured = false,
            },
            new PostView
            {
                Id = "5",
                Title = "Why gadgets are the future of everyday productivity",
                Slug = "gadgets-future-of-productivity",
                Excerpt = "From smartwatches to home assistants, the right gadget can quietly "
                    + "reclaim hours of your week. We look at what actually helps.",
                ImageUrl = "assets/images/blog/16by9/big/05.jpg",
                Category = "Gadgets",
                CategoryBadgeCss = "text-bg-danger",
                AuthorName = "Dennis Barrett",
                AuthorImageUrl = "assets/images/avatar/05.jpg",
                PublishedDate = new DateTimeOffset(2022, 11, 2, 0, 0, 0, TimeSpan.Zero),
                ReadMinutes = 5,
                IsFeatured = false,
            },
            new PostView
            {
                Id = "6",
                Title = "How to build a reading habit that actually sticks",
                Slug = "build-a-reading-habit-that-sticks",
                Excerpt = "Everyone wants to read more, few manage it. The trick is designing your "
                    + "environment so reading becomes the easy choice.",
                ImageUrl = "assets/images/blog/16by9/big/06.jpg",
                Category = "Lifestyle",
                CategoryBadgeCss = "text-bg-info",
                AuthorName = "Carolyn Ortiz",
                AuthorImageUrl = "assets/images/avatar/06.jpg",
                PublishedDate = new DateTimeOffset(2022, 12, 15, 0, 0, 0, TimeSpan.Zero),
                ReadMinutes = 4,
                IsFeatured = false,
            },
        };
    }
}
