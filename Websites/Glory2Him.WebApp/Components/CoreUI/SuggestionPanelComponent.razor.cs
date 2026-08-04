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
using Microsoft.AspNetCore.Components.Web;

namespace Glory2Him.WebApp.Components.CoreUI
{
    public partial class SuggestionPanelComponent
    {
        [Parameter]
        [EditorRequired]
        public string Heading { get; set; } = string.Empty;

        [Parameter]
        public string SuggestHeading { get; set; } = string.Empty;

        [Parameter]
        public string Prompt { get; set; } = string.Empty;

        [Parameter]
        public string Placeholder { get; set; } = string.Empty;

        [Parameter]
        public IReadOnlyList<string> Items { get; set; } = new List<string>();

        [Parameter]
        public string ItemCssClass { get; set; } = "btn-success-soft";

        // Left null for tags, which are prefixed with a hash instead of carrying an icon.
        [Parameter]
        public string? ItemIconCssClass { get; set; }

        [Parameter]
        public bool PrefixHash { get; set; }

        // Where an approved pill links to; {0} is the item, URL-escaped.
        [Parameter]
        public string HrefFormat { get; set; } = "Tag?name={0}";

        [Parameter]
        public EventCallback<string> OnSuggested { get; set; }

        private readonly List<string> pendingItems = new List<string>();

        private string? draft;

        private string DisplayName(string item) =>
            PrefixHash ? $"#{item}" : item;

        private string BuildHref(string item) =>
            string.Format(HrefFormat, Uri.EscapeDataString(item));

        private void OnDraftChanged(ChangeEventArgs args) =>
            this.draft = args.Value?.ToString();

        private async Task OnKeyDownAsync(KeyboardEventArgs args)
        {
            if (args.Key is not ("Enter" or "NumpadEnter"))
            {
                return;
            }

            await AddSuggestionAsync();
        }

        private async Task AddSuggestionAsync()
        {
            string suggestion = (this.draft ?? string.Empty).Trim().TrimStart('#');

            if (suggestion.Length == 0)
            {
                return;
            }

            // Neither an approved pill nor an already-pending one should be offered twice.
            bool alreadyListed =
                Items.Any(item => string.Equals(item, suggestion, StringComparison.OrdinalIgnoreCase))
                    || this.pendingItems.Any(item =>
                        string.Equals(item, suggestion, StringComparison.OrdinalIgnoreCase));

            this.draft = string.Empty;

            if (alreadyListed)
            {
                return;
            }

            this.pendingItems.Add(suggestion);

            await OnSuggested.InvokeAsync(suggestion);
        }

        // Withdrawing a suggestion is the mirror of making one, and just as local to this
        // component — nothing was stored, so nothing needs unstoring.
        private void RemoveSuggestion(string item) =>
            this.pendingItems.Remove(item);
    }
}
