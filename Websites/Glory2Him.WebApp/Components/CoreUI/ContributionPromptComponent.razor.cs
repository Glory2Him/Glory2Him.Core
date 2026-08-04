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
    public partial class ContributionPromptComponent
    {
        // "Have a contribution / something to share?" said the same thing twice and the slash read
        // as a form label — the question is simply whether the reader has something.
        [Parameter]
        public string Heading { get; set; } = "Have something to share?";

        // Naming what a contribution can be does more to prompt one than asking in the abstract.
        [Parameter]
        public string Body { get; set; } =
            "A story, a testimony, or a verse that carried you through — if it might encourage "
                + "someone else, we would love to read it.";

        [Parameter]
        public string LinkText { get; set; } = "Submit a contribution";

        [Parameter]
        public string Href { get; set; } = "Contribute";

        [Parameter]
        public string IconCssClass { get; set; } = "bi-pencil-square";

        // A little under the 44px author avatar, so it reads as an icon rather than a portrait.
        [Parameter]
        public int IconSizePx { get; set; } = 36;

        [Parameter]
        public string CssClass { get; set; } = "mb-4";
    }
}
