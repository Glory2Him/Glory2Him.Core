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
using System.Linq;
using Microsoft.AspNetCore.Components;

namespace Glory2Him.WebApp.Components.CoreUI
{
    public partial class AvatarComponent
    {
        // A calm, readable palette; the name selects one deterministically so a user always gets
        // the same colour.
        private static readonly string[] Palette =
        {
            "#2163e8", "#0cbc87", "#d6293e", "#f7c32e",
            "#4f42b5", "#0d6efd", "#20c997", "#fd7e14",
        };

        [Parameter]
        [EditorRequired]
        public string Name { get; set; } = string.Empty;

        [Parameter]
        public string? ImageUrl { get; set; }

        [Parameter]
        public int SizePx { get; set; } = 40;

        [Parameter]
        public string SizeCssClass { get; set; } = string.Empty;

        private int FontSizePx => Math.Max(10, (int)(SizePx * 0.42));

        private string Initials
        {
            get
            {
                string trimmed = (Name ?? string.Empty).Trim();

                if (trimmed.Length == 0)
                {
                    return "?";
                }

                string[] parts =
                    trimmed.Split(new[] { ' ', '-', '_', '.' },
                        StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 2)
                {
                    return string.Concat(
                        char.ToUpperInvariant(parts[0][0]),
                        char.ToUpperInvariant(parts[^1][0]));
                }

                string single = parts[0];

                return single.Length >= 2
                    ? single[..2].ToUpperInvariant()
                    : single[..1].ToUpperInvariant();
            }
        }

        private string BackgroundColor
        {
            get
            {
                string key = (Name ?? string.Empty).Trim().ToLowerInvariant();

                // Stable, framework-independent hash so the colour never shifts between runs.
                int hash = key.Aggregate(17, (current, character) => (current * 31) + character);

                return Palette[Math.Abs(hash) % Palette.Length];
            }
        }
    }
}
