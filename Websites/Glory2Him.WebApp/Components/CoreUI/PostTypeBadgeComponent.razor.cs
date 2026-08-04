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
    public partial class PostTypeBadgeComponent
    {
        [Parameter]
        [EditorRequired]
        public PostType Type { get; set; } = PostType.Standard;

        [Parameter]
        public int SizePx { get; set; } = 36;

        private string IconCssClass =>
            Type switch
            {
                PostType.Video => "bi-play-fill",
                PostType.Audio => "bi-mic-fill",
                PostType.Gallery => "bi-images",
                PostType.Quote => "bi-quote",
                _ => "bi-file-text-fill",
            };

        private string BackgroundCssClass =>
            Type switch
            {
                PostType.Video => "text-bg-danger",
                PostType.Audio => "text-bg-success",
                PostType.Gallery => "text-bg-warning",
                PostType.Quote => "text-bg-info",
                _ => "text-bg-primary",
            };

        private string Label =>
            $"{Type} post";
    }
}
