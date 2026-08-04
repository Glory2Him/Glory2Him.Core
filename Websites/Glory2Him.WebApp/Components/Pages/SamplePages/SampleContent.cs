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
    // The content shown in the Home and Post Detail mockups, transcribed so the demos can be
    // compared against the originals side by side.
    public static class SampleContent
    {
        public const string VerseOfTheDay =
            "\"For by grace you have been saved through faith...\" — Ephesians 2:8 NIV";

        // The lead story, shown as the featured card on Home and in full on Post Detail.
        public static SamplePost Featured =>
            new SamplePost(
                Title: "NASA Proves The Bible Is True",
                Slug: "nasa-proves-the-bible-is-true",
                Excerpt: "The story of the missing day in space — Joshua's long day and "
                    + "Hezekiah's shadow that went backward.",
                Category: "Testimony",
                CategoryBadgeCss: "text-bg-warning",
                AuthorName: "Louis",
                AuthorRole: "An editor at Glory 2 Him",
                AuthorImageUrl: "assets/images/avatar/01.jpg",
                ImageUrl: "assets/images/blog/16by9/big/01.jpg",
                PublishedDate: new DateTimeOffset(2026, 7, 15, 0, 0, 0, TimeSpan.Zero),
                ReadMinutes: 5,
                Reactions: 266,
                Comments: 18,
                Views: 2344,
                Tags: new[] { "creation", "science", "faith", "miracles" },
                BibleReferences: new[] { "Joshua 10:8, 12–13", "2 Kings 20:9–11" },
                IsFeatured: true);

        // The three tiles filling the right half of the hero grid, in the mockup's order.
        public static IReadOnlyList<SamplePost> HeroTiles =>
            new[]
            {
                new SamplePost(
                    Title: "Justification means there isn't a charge against you — D.L. Moody",
                    Slug: "justification-no-charge-against-you",
                    Excerpt: string.Empty,
                    Category: "Quotes",
                    CategoryBadgeCss: "text-bg-success",
                    AuthorName: "Bryan",
                    AuthorRole: "Contributor",
                    AuthorImageUrl: "assets/images/avatar/02.jpg",
                    ImageUrl: "assets/images/blog/4by3/01.jpg",
                    PublishedDate: new DateTimeOffset(2026, 7, 18, 0, 0, 0, TimeSpan.Zero),
                    ReadMinutes: 2,
                    Reactions: 142,
                    Comments: 9,
                    Views: 980,
                    Tags: new[] { "justified", "redemption", "grace" },
                    BibleReferences: new[] { "Romans 3:23–24" }),

                new SamplePost(
                    Title: "Walking daily in grace",
                    Slug: "walking-daily-in-grace",
                    Excerpt: string.Empty,
                    Category: "Devotional",
                    CategoryBadgeCss: "text-bg-danger",
                    AuthorName: "Joan",
                    AuthorRole: "Contributor",
                    AuthorImageUrl: "assets/images/avatar/03.jpg",
                    ImageUrl: "assets/images/blog/4by3/03.jpg",
                    PublishedDate: new DateTimeOffset(2026, 7, 3, 0, 0, 0, TimeSpan.Zero),
                    ReadMinutes: 3,
                    Reactions: 87,
                    Comments: 5,
                    Views: 1120,
                    Tags: new[] { "grace", "discipleship" },
                    BibleReferences: new[] { "Ephesians 2:8–9" }),

                new SamplePost(
                    Title: "The armor of God, piece by piece",
                    Slug: "the-armor-of-god-piece-by-piece",
                    Excerpt: string.Empty,
                    Category: "Bible Study",
                    CategoryBadgeCss: "text-bg-info",
                    AuthorName: "Amanda",
                    AuthorRole: "Contributor",
                    AuthorImageUrl: "assets/images/avatar/04.jpg",
                    ImageUrl: "assets/images/blog/4by3/04.jpg",
                    PublishedDate: new DateTimeOffset(2026, 6, 28, 0, 0, 0, TimeSpan.Zero),
                    ReadMinutes: 6,
                    Reactions: 54,
                    Comments: 12,
                    Views: 1340,
                    Tags: new[] { "prayer", "spiritual-warfare" },
                    BibleReferences: new[] { "Ephesians 6:10–18" }),
            };

        // The "Latest posts" grid — four cards, each carrying its own excerpt, tags and references.
        public static IReadOnlyList<SamplePost> Latest =>
            new[]
            {
                new SamplePost(
                    Title: "Justification means there isn’t a charge against you",
                    Slug: "justification-no-charge-against-you",
                    Excerpt: "Your sins are completely wiped out; God says He puts them out of "
                        + "His memory. — Dwight L. Moody",
                    Category: "Quotes",
                    CategoryBadgeCss: "text-bg-success",
                    AuthorName: "Bryan",
                    AuthorRole: "Contributor",
                    AuthorImageUrl: "assets/images/avatar/02.jpg",
                    ImageUrl: "assets/images/blog/4by3/01.jpg",
                    PublishedDate: new DateTimeOffset(2026, 7, 18, 0, 0, 0, TimeSpan.Zero),
                    ReadMinutes: 2,
                    Reactions: 142,
                    Comments: 9,
                    Views: 980,
                    Tags: new[] { "justified", "redemption", "grace" },
                    BibleReferences: new[] { "Romans 3:23–24" }),

                new SamplePost(
                    Title: "NASA Proves The Bible Is True",
                    Slug: "nasa-proves-the-bible-is-true",
                    Excerpt: "The story of the missing day in space — and the forty minutes found "
                        + "in Hezekiah’s backward shadow.",
                    Category: "Testimony",
                    CategoryBadgeCss: "text-bg-warning",
                    AuthorName: "Louis",
                    AuthorRole: "An editor at Glory 2 Him",
                    AuthorImageUrl: "assets/images/avatar/01.jpg",
                    ImageUrl: "assets/images/blog/4by3/02.jpg",
                    PublishedDate: new DateTimeOffset(2026, 7, 15, 0, 0, 0, TimeSpan.Zero),
                    ReadMinutes: 5,
                    Reactions: 266,
                    Comments: 18,
                    Views: 2344,
                    Tags: new[] { "creation", "science", "faith" },
                    BibleReferences: new[] { "Joshua 10:12–13", "2 Kings 20:9–11" }),

                new SamplePost(
                    Title: "Walking daily in grace",
                    Slug: "walking-daily-in-grace",
                    Excerpt: "Grace is not a one-time event but the daily air the believer "
                        + "breathes.",
                    Category: "Devotional",
                    CategoryBadgeCss: "text-bg-danger",
                    AuthorName: "Joan",
                    AuthorRole: "Contributor",
                    AuthorImageUrl: "assets/images/avatar/03.jpg",
                    ImageUrl: "assets/images/blog/4by3/03.jpg",
                    PublishedDate: new DateTimeOffset(2026, 7, 3, 0, 0, 0, TimeSpan.Zero),
                    ReadMinutes: 3,
                    Reactions: 87,
                    Comments: 5,
                    Views: 1120,
                    Tags: new[] { "grace", "discipleship" },
                    BibleReferences: new[] { "Ephesians 2:8–9" }),

                new SamplePost(
                    Title: "The armor of God, piece by piece",
                    Slug: "the-armor-of-god-piece-by-piece",
                    Excerpt: "A six-part walk through Paul’s picture of the believer’s equipment "
                        + "for the fight.",
                    Category: "Bible Study",
                    CategoryBadgeCss: "text-bg-info",
                    AuthorName: "Amanda",
                    AuthorRole: "Contributor",
                    AuthorImageUrl: "assets/images/avatar/04.jpg",
                    ImageUrl: "assets/images/blog/4by3/04.jpg",
                    PublishedDate: new DateTimeOffset(2026, 6, 28, 0, 0, 0, TimeSpan.Zero),
                    ReadMinutes: 6,
                    Reactions: 54,
                    Comments: 12,
                    Views: 1340,
                    Tags: new[] { "prayer", "spiritual-warfare" },
                    BibleReferences: new[] { "Ephesians 6:10–18" }),
            };

        // Post Detail states its own figures for the lead story, and they differ from the numbers
        // on the same story's Home card (257 vs 266 reactions, 4 vs 18 comments). Each page is
        // kept faithful to its own mockup rather than one being quietly "corrected".
        public const int DetailReactions = 257;
        public const int DetailComments = 4;
        public const int DetailViews = 2344;
        public const string DetailAuthorName = "Louis Ferguson";

        public static IReadOnlyList<SampleReaction> Reactions =>
            new[]
            {
                new SampleReaction("Amen", "fas fa-thumbs-up", "#4e5ff9", 112),
                new SampleReaction("Love", "fas fa-heart", "#d6293e", 98),
                new SampleReaction("Joy", "fas fa-smile", "#f7c32e", 41),
                new SampleReaction("Moved", "fas fa-sad-tear", "#17a2b8", 6),
            };

        public static IReadOnlyList<SampleComment> Comments =>
            new[]
            {
                new SampleComment(
                    AuthorName: "Allen Smith",
                    AuthorImageUrl: "assets/images/avatar/01.jpg",
                    PostedAt: new DateTimeOffset(2026, 7, 16, 6, 1, 0, TimeSpan.Zero),
                    Body: "This blessed me so much. The little words in Scripture really do "
                        + "matter — \"about a whole day\"!",
                    Reactions: 14),

                new SampleComment(
                    AuthorName: "Louis Ferguson",
                    AuthorImageUrl: "assets/images/avatar/02.jpg",
                    PostedAt: new DateTimeOffset(2026, 7, 16, 9, 24, 0, TimeSpan.Zero),
                    Body: "Thank you Allen — that phrase is exactly what sent me looking into "
                        + "this in the first place.",
                    Reactions: 6,
                    IsReply: true),

                new SampleComment(
                    AuthorName: "Marie Cooper",
                    AuthorImageUrl: "assets/images/avatar/03.jpg",
                    PostedAt: new DateTimeOffset(2026, 7, 17, 14, 12, 0, TimeSpan.Zero),
                    Body: "I had never connected Hezekiah's sundial to Joshua's long day. "
                        + "Reading both together is remarkable.",
                    Reactions: 21),

                new SampleComment(
                    AuthorName: "Peter Nguyen",
                    AuthorImageUrl: "assets/images/avatar/04.jpg",
                    PostedAt: new DateTimeOffset(2026, 7, 18, 8, 40, 0, TimeSpan.Zero),
                    Body: "Worth reading slowly. Sharing this with our small group this week.",
                    Reactions: 9),
            };

        public static IReadOnlyList<(string Label, string ButtonCssClass)> Categories =>
            new[]
            {
                ("Testimony", "btn-warning"),
                ("Quotes", "btn-success"),
                ("Stories", "btn-info"),
                ("Devotional", "btn-danger"),
            };

        public static IReadOnlyList<(string Label, string ButtonCssClass)> PopularTags =>
            new[]
            {
                ("grace", "btn-primary-soft"),
                ("faith", "btn-warning-soft"),
                ("redemption", "btn-success-soft"),
                ("prayer", "btn-danger-soft"),
                ("justified", "btn-info-soft"),
                ("creation", "btn-primary-soft"),
            };

        public static IReadOnlyList<string> PopularReferences =>
            new[]
            {
                "Romans 3:23–24",
                "Joshua 10:12–13",
                "2 Kings 20:9–11",
                "Ephesians 2:8–9",
            };
    }
}
