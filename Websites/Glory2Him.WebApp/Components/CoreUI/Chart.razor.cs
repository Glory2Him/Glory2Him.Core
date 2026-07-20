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
using Microsoft.JSInterop;

namespace Glory2Him.WebApp.Components.CoreUI
{
    public partial class Chart : IAsyncDisposable
    {
        [Inject]
        public IJSRuntime JSRuntime { get; set; } = default!;

        [Parameter]
        public string ChartType { get; set; } = "line";

        [Parameter]
        public IReadOnlyList<string> Labels { get; set; } = new List<string>();

        [Parameter]
        public IReadOnlyList<ChartDataset> Datasets { get; set; } = new List<ChartDataset>();

        [Parameter]
        public int Height { get; set; } = 300;

        public string ElementId { get; } = "chart-" + Guid.NewGuid().ToString("N");

        private string? lastRenderedSignature;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Only (re)draw when the data actually changes, so re-renders (e.g. a dashboard
            // refresh) do not rebuild the chart and cause flicker.
            string signature = ComputeSignature();

            if (signature == lastRenderedSignature)
            {
                return;
            }

            lastRenderedSignature = signature;

            await this.JSRuntime.InvokeVoidAsync(
                "glory2himCharts.render", ElementId, BuildConfig());
        }

        private string ComputeSignature() =>
            ChartType + "|" + string.Join(",", Labels) + "|" + string.Join(";",
                Datasets.Select(dataset =>
                    dataset.Label + ":" + string.Join(",", dataset.Data)));

        private object BuildConfig() =>
            new
            {
                type = ChartType,
                height = Height,
                labels = Labels,
                datasets = Datasets.Select(dataset => new
                {
                    label = dataset.Label,
                    data = dataset.Data,
                    colors = dataset.Colors,
                    dashed = dataset.Dashed,
                    fill = dataset.Fill,
                })
            };

        public async ValueTask DisposeAsync()
        {
            try
            {
                await this.JSRuntime.InvokeVoidAsync("glory2himCharts.destroy", ElementId);
            }
            catch
            {
                // ignored — circuit may already be gone during disposal
            }
        }
    }
}
