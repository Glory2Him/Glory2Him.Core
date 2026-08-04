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

using Microsoft.AspNetCore.Components;

namespace Glory2Him.WebApp.Components.CoreUI
{
    public partial class PostHeroCardComponent
    {
        [Parameter]
        [EditorRequired]
        public string Title { get; set; } = string.Empty;

        [Parameter]
        public string Href { get; set; } = "#";

        [Parameter]
        public string? Excerpt { get; set; }

        [Parameter]
        public bool ShowExcerpt { get; set; } = true;

        [Parameter]
        public string Category { get; set; } = string.Empty;

        [Parameter]
        public string CategoryBadgeCss { get; set; } = "text-bg-primary";

        [Parameter]
        public string ImageUrl { get; set; } = string.Empty;

        [Parameter]
        public string AuthorName { get; set; } = string.Empty;

        [Parameter]
        public DateTimeOffset PublishedDate { get; set; }

        [Parameter]
        public bool IsFeatured { get; set; }

        // card-grid-lg for a half-page lead, card-grid-sm for the tiles beside it.
        [Parameter]
        public string SizeCssClass { get; set; } = "card-grid-lg";

        [Parameter]
        public string TitleCssClass { get; set; } = "h1";

        [Parameter]
        public int? Reactions { get; set; }

        [Parameter]
        public int? Comments { get; set; }

        [Parameter]
        public int? TagCount { get; set; }

        [Parameter]
        public int? ReferenceCount { get; set; }

        // Puts the engagement counts on their own row beneath the byline. The narrow hero tiles
        // need this; the full-width lead does not.
        [Parameter]
        public bool SplitMeta { get; set; }

        // The lead card shows the author's face beside their name; the smaller tiles have no room
        // for it and use the name alone.
        [Parameter]
        public bool ShowAuthorAvatar { get; set; }

        [Parameter]
        public string? AuthorImageUrl { get; set; }
    }
}
