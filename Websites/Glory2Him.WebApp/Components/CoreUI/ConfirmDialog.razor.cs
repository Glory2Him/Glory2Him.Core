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
    public partial class ConfirmDialog
    {
        [Parameter]
        public bool Visible { get; set; }

        [Parameter]
        public string Title { get; set; } = "Are you sure?";

        [Parameter]
        public string? Message { get; set; }

        [Parameter]
        public string ConfirmText { get; set; } = "OK";

        [Parameter]
        public string CancelText { get; set; } = "Cancel";

        [Parameter]
        public string ConfirmColor { get; set; } = "danger";

        [Parameter]
        public EventCallback OnConfirm { get; set; }

        [Parameter]
        public EventCallback OnCancel { get; set; }
    }
}
