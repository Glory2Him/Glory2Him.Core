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
    public partial class EngagementMetaComponent
    {
        [Parameter]
        public string AuthorName { get; set; } = string.Empty;

        [Parameter]
        public string? AuthorImageUrl { get; set; }

        [Parameter]
        public bool ShowAuthor { get; set; } = true;

        [Parameter]
        public DateTimeOffset? PublishedDate { get; set; }

        // Every count is optional: null leaves the entry out rather than rendering a zero.
        [Parameter]
        public int? ReadMinutes { get; set; }

        [Parameter]
        public int? Reactions { get; set; }

        [Parameter]
        public int? Comments { get; set; }

        [Parameter]
        public int? TagCount { get; set; }

        [Parameter]
        public int? ReferenceCount { get; set; }

        [Parameter]
        public int? Views { get; set; }

        [Parameter]
        public string CssClass { get; set; } = "mb-0";
    }
}
