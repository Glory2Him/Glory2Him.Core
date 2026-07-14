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

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Glory2Him.WebApp.Components.CoreUI
{
    public partial class FormDate
    {
        [Parameter]
        public string? Label { get; set; }

        [Parameter]
        public DateTimeOffset? Value { get; set; }

        [Parameter]
        public EventCallback<DateTimeOffset?> ValueChanged { get; set; }

        private string? FormattedValue =>
            Value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        private async Task OnChangeAsync(ChangeEventArgs args)
        {
            string? rawValue = args.Value?.ToString();

            DateTimeOffset? parsedValue =
                DateTimeOffset.TryParse(
                    rawValue,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out DateTimeOffset value)
                    ? value
                    : null;

            await ValueChanged.InvokeAsync(parsedValue);
        }
    }
}
