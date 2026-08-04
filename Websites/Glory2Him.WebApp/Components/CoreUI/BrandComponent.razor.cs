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
    public partial class BrandComponent
    {
        [Parameter]
        public BrandVariant Variant { get; set; } = BrandVariant.Responsive;

        // Left null the component's own stylesheet sizes the mark, which is what the header wants
        // so the brand shrinks with the sticky navbar. Set it where a fixed size reads better.
        [Parameter]
        public int? BannerHeightPx { get; set; }

        [Parameter]
        public int? IconSizePx { get; set; }

        // Overrides the wordmark size directly. Useful for the Text variant, which has no icon to
        // derive a balanced size from.
        [Parameter]
        public int? NameFontSizePx { get; set; }

        // The "2" is the brand's colour accent on light backgrounds; a caller over its own artwork
        // (the header photo) wants the whole wordmark in one flat colour instead.
        [Parameter]
        public bool AccentTwo { get; set; } = true;

        private string TwoSpanClass =>
            AccentTwo ? "text-primary" : string.Empty;

        private string? BannerStyle =>
            BannerHeightPx is null ? null : $"height:{BannerHeightPx}px;";

        private string? IconStyle =>
            IconSizePx is null ? null : $"width:{IconSizePx}px;height:{IconSizePx}px;";

        private string? NameStyle
        {
            get
            {
                if (NameFontSizePx is not null)
                {
                    return $"font-size:{NameFontSizePx}px;";
                }

                // Without an explicit size, the wordmark is sized off the icon so an icon+text
                // lockup stays balanced at any scale.
                return IconSizePx is null
                    ? null
                    : $"font-size:{Math.Max(14, (int)(IconSizePx.Value * 0.62))}px;";
            }
        }
    }
}
