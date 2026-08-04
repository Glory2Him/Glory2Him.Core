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
    public sealed record BibleVerse(int Number, string Text);

    public sealed record BibleSection(string? Heading, IReadOnlyList<BibleVerse> Verses);

    // Code is what a chapter is looked up by ("NIV"); Name is what the reader picks from the list.
    public sealed record ScriptureTranslation(string Code, string Name);

    public partial class BibleChapterComponent
    {
        [Parameter]
        [EditorRequired]
        public string Reference { get; set; } = string.Empty;

        [Parameter]
        public IReadOnlyList<BibleSection> Sections { get; set; } =
            new List<BibleSection>();

        [Parameter]
        public bool ShowShareLinks { get; set; } = true;
    }
}
