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
    public partial class ReviewRatingComponent
    {
        [Parameter]
        public double OverallScore { get; set; }

        [Parameter]
        public int MaximumScore { get; set; } = 5;

        [Parameter]
        public string? Summary { get; set; }

        [Parameter]
        public IReadOnlyList<ReviewCriterion> Criteria { get; set; } =
            new List<ReviewCriterion>();

        private int ToPercentage(double score) =>
            MaximumScore <= 0
                ? 0
                : (int)Math.Round(Math.Clamp(score / MaximumScore, 0, 1) * 100);
    }
}
