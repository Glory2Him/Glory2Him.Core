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

namespace Glory2Him.WebApp.Components.Pages.SamplePages
{
    // The mockups carry more than PostView does — hashtags, bible references, reaction and view
    // counts, an author role. These live here rather than on PostView because they are still a
    // design proposal; PostView stays the shape the app actually stores.
    public sealed record SamplePost(
        string Title,
        string Slug,
        string Excerpt,
        string Category,
        string CategoryBadgeCss,
        string AuthorName,
        string AuthorRole,
        string AuthorImageUrl,
        string ImageUrl,
        DateTimeOffset PublishedDate,
        int ReadMinutes,
        int Reactions,
        int Comments,
        int Views,
        IReadOnlyList<string> Tags,
        IReadOnlyList<string> BibleReferences,
        bool IsFeatured = false);

    public sealed record SampleComment(
        string AuthorName,
        string AuthorImageUrl,
        DateTimeOffset PostedAt,
        string Body,
        int Reactions,
        bool IsReply = false);

    public sealed record SampleReaction(
        string Label,
        string IconCssClass,
        string Color,
        int Count);
}
