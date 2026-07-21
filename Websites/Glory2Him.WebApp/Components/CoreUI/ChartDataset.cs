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
