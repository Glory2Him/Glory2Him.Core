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

using System.Collections.Generic;

namespace Glory2Him.WebApp.Components.CoreUI
{
    public sealed class ChartDataset
    {
        public string Label { get; set; } = string.Empty;

        public IReadOnlyList<double> Data { get; set; } = new List<double>();

        // Series colour(s). For bar/donut charts more than one colour may be supplied,
        // one per data point.
        public IReadOnlyList<string> Colors { get; set; } = new List<string>();

        // Renders the line as a dashed stroke.
        public bool Dashed { get; set; }

        // Fills the area under a line (line charts only).
        public bool Fill { get; set; } = true;
    }
}
