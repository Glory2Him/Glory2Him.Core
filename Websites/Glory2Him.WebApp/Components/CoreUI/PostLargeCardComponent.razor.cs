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
using Microsoft.AspNetCore.Components;

namespace Glory2Him.WebApp.Components.CoreUI
{
    public partial class PostLargeCardComponent
    {
        [Parameter]
        [EditorRequired]
        public PostView Post { get; set; } = new PostView();

        [Parameter]
        public string TitleCssClass { get; set; } = "h4";

        // Stacked below md so the image never shrinks to a sliver on a phone.
        [Parameter]
        public bool ImageFirst { get; set; } = true;

        private string ImageColumnCssClass =>
            ImageFirst ? "col-md-5" : "col-md-5 order-md-2";

        private string BodyColumnCssClass =>
            ImageFirst ? "col-md-7" : "col-md-7 order-md-1";
    }
}
