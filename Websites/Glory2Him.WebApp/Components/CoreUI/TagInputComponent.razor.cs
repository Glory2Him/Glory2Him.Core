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
    public partial class TagInputComponent
    {
        [Parameter]
        public IReadOnlyList<string> Tags { get; set; } = new List<string>();

        [Parameter]
        public EventCallback<IReadOnlyList<string>> TagsChanged { get; set; }

        [Parameter]
        public string Placeholder { get; set; } = "Type a tag and press Enter";

        [Parameter]
        public string AriaLabel { get; set; } = "Add a tag";

        [Parameter]
        public string TagCssClass { get; set; } = "btn-success-soft";

        private string? draft;

        private void OnDraftChanged(ChangeEventArgs args) =>
            this.draft = args.Value?.ToString();

        private async Task OnKeyDownAsync(KeyboardEventArgs args)
        {
            if (args.Key is not ("Enter" or "NumpadEnter"))
            {
                return;
            }

            // A leading hash is how people write tags, but it is not part of the tag itself.
            string tag = (this.draft ?? string.Empty).Trim().TrimStart('#');

            this.draft = string.Empty;

            bool alreadyListed = Tags.Any(listed =>
                string.Equals(listed, tag, StringComparison.OrdinalIgnoreCase));

            if (tag.Length == 0 || alreadyListed)
            {
                return;
            }

            await TagsChanged.InvokeAsync(Tags.Append(tag).ToList());
        }

        private Task RemoveAsync(string tag) =>
            TagsChanged.InvokeAsync(Tags.Where(listed => listed != tag).ToList());
    }
}
