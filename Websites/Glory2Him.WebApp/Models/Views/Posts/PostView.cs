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

namespace Glory2Him.WebApp.Models.Views.Posts
{
    public class PostView
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string CategoryBadgeCss { get; set; } = "text-bg-primary";
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorImageUrl { get; set; } = string.Empty;
        public DateTimeOffset PublishedDate { get; set; }
        public int ReadMinutes { get; set; }
        public bool IsFeatured { get; set; }
    }
}
