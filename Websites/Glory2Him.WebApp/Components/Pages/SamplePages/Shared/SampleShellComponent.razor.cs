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

namespace Glory2Him.WebApp.Components.Pages.SamplePages.Shared
{
    public partial class SampleShellComponent
    {
        [Parameter]
        [EditorRequired]
        public string Title { get; set; } = string.Empty;

        // The Blogzine file this layout was ported from, shown so the demo can be traced back to
        // its source when comparing against the original template.
        [Parameter]
        public string? SourceFile { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }
    }
}
