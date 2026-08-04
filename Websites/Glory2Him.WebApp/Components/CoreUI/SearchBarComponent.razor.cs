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
    public partial class SearchBarComponent
    {
        [Parameter]
        public string Query { get; set; } = string.Empty;

        [Parameter]
        public EventCallback<string> QueryChanged { get; set; }

        [Parameter]
        public EventCallback OnSearch { get; set; }

        [Parameter]
        public string Placeholder { get; set; } = "Search";

        // Left null by pages that want the plain box; the chevron only appears when there is
        // something behind it.
        [Parameter]
        public RenderFragment? Advanced { get; set; }

        // A fixed id is safe here: aria-controls only has to be unique on the page, and a page
        // carries one search bar.
        private const string AdvancedPanelId = "advancedSearchOptions";

        private bool isAdvancedOpen;

        private async Task OnQueryInputAsync(ChangeEventArgs args)
        {
            Query = args.Value?.ToString() ?? string.Empty;

            await QueryChanged.InvokeAsync(Query);
        }

        private void ToggleAdvanced() =>
            this.isAdvancedOpen = !this.isAdvancedOpen;

        private Task SubmitAsync() =>
            OnSearch.InvokeAsync();
    }
}
