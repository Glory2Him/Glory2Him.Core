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
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.CoreUI;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public class ChartComponentTests : BunitContext
    {
        public ChartComponentTests() =>
            JSInterop.Mode = JSRuntimeMode.Loose;

        [Fact]
        public void ShouldRenderChartContainerWithUniqueId()
        {
            // given . when
            IRenderedComponent<Chart> renderedChart =
                Render<Chart>(parameters => parameters
                    .Add(chart => chart.ChartType, "donut")
                    .Add(chart => chart.Height, 250));

            // then
            renderedChart.Find("div.chart-wrapper").GetAttribute("style")
                .Should().Contain("250px");

            renderedChart.Instance.ElementId.Should().StartWith("chart-");
        }

        [Fact]
        public void ShouldInvokeApexRenderInteropOnRender()
        {
            // given
            var datasets = new List<ChartDataset>
            {
                new ChartDataset { Label = "Posts", Data = new List<double> { 1, 2, 3 } },
            };

            // when
            Render<Chart>(parameters => parameters
                .Add(chart => chart.ChartType, "donut")
                .Add(chart => chart.Labels, new List<string> { "A", "B", "C" })
                .Add(chart => chart.Datasets, datasets));

            // then (the ApexCharts interop module is invoked to draw the chart)
            JSInterop.VerifyInvoke("glory2himCharts.render");
        }
    }
}
