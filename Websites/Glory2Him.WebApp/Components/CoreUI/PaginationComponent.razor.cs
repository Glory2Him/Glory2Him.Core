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
    public partial class PaginationComponent
    {
        [Parameter]
        public int CurrentPage { get; set; } = 1;

        [Parameter]
        public int TotalPages { get; set; } = 1;

        [Parameter]
        public EventCallback<int> CurrentPageChanged { get; set; }

        [Parameter]
        public PaginationVariant Variant { get; set; } = PaginationVariant.Numbered;

        [Parameter]
        public bool Alignment { get; set; } = true;

        [Parameter]
        public string AriaLabel { get; set; } = "Page navigation";

        // The rounded variant is the same control with pill-shaped links; "PrevNext" drops the
        // numbers entirely and spells the direction out.
        private string VariantCssClass =>
            Variant is PaginationVariant.Rounded ? "pagination-rounded" : string.Empty;

        private string AlignmentCssClass =>
            Alignment ? "justify-content-center" : string.Empty;

        private bool ShowNumbers =>
            Variant is not PaginationVariant.PrevNext;

        private bool ShowLabels =>
            Variant is PaginationVariant.PrevNext;

        private IEnumerable<int> PageNumbers =>
            Enumerable.Range(1, Math.Max(TotalPages, 1));

        private async Task GoTo(int page)
        {
            if (page < 1 || page > TotalPages || page == CurrentPage)
            {
                return;
            }

            CurrentPage = page;

            await CurrentPageChanged.InvokeAsync(page);
        }
    }
}
