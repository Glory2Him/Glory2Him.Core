// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6
// ────────────────────────────────────────────────────────────────────────────────

using Microsoft.AspNetCore.Components;

namespace Glory2Him.WebApp.Components.CoreUI
{
    public partial class StatTile
    {
        [Parameter]
        public StatTileVariant Variant { get; set; } = StatTileVariant.Na;

        [Parameter]
        public string? Value { get; set; }

        [Parameter]
        public string? Label { get; set; }

        [Parameter]
        public string? Icon { get; set; }

        // The RAG variants map onto Blogzine's Bootstrap contextual colours so the tile matches the
        // template's dashboard counters rather than EventHighway's gradient RAG styling.
        public string VariantCssClass =>
            Variant switch
            {
                StatTileVariant.Green => "rag-green",
                StatTileVariant.Amber => "rag-amber",
                StatTileVariant.Red => "rag-red",
                _ => "rag-na"
            };

        public string IconCssClass =>
            Variant switch
            {
                StatTileVariant.Green => "bg-success bg-opacity-10 text-success",
                StatTileVariant.Amber => "bg-warning bg-opacity-10 text-warning",
                StatTileVariant.Red => "bg-danger bg-opacity-10 text-danger",
                _ => "bg-primary bg-opacity-10 text-primary"
            };
    }
}
