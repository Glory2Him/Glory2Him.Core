// ────────────────────────────────────────────────────────────────────────────────
// Glory 2 Him — ApexCharts interop for the Blazor Chart component.
// Blogzine ships ApexCharts (not Chart.js), so the ported CoreUI Chart component renders
// through this module instead of EventHighway's chart.js interop.
// ────────────────────────────────────────────────────────────────────────────────

window.glory2himCharts = (function () {
    const charts = {};

    function destroy(elementId) {
        const chart = charts[elementId];

        if (chart) {
            chart.destroy();
            delete charts[elementId];
        }
    }

    function render(elementId, config) {
        const element = document.querySelector('#' + elementId);

        if (!element || typeof ApexCharts === 'undefined') {
            return;
        }

        destroy(elementId);

        const series = (config.datasets || []).map(function (dataset) {
            return { name: dataset.label, data: dataset.data };
        });

        const colors = (config.datasets || [])
            .map(function (dataset) {
                return (dataset.colors && dataset.colors.length > 0)
                    ? dataset.colors[0]
                    : '#2163e8';
            });

        const options = {
            chart: {
                type: config.type || 'line',
                height: config.height || 300,
                toolbar: { show: false },
                fontFamily: 'inherit'
            },
            series: series,
            colors: colors,
            labels: config.labels || [],
            xaxis: {
                categories: config.labels || [],
                axisBorder: { show: false }
            },
            stroke: {
                curve: 'smooth',
                width: 2,
                dashArray: (config.datasets || []).map(function (dataset) {
                    return dataset.dashed ? 5 : 0;
                })
            },
            dataLabels: { enabled: false },
            legend: {
                show: series.length > 1,
                position: 'top',
                horizontalAlign: 'right',
                markers: { width: 8, height: 8 }
            },
            grid: { borderColor: 'rgba(0,0,0,0.08)' }
        };

        // Donut/pie take a flat numeric series and use `labels` for the slice names.
        if (options.chart.type === 'donut' || options.chart.type === 'pie') {
            options.series = series.length > 0 ? series[0].data : [];
            options.colors = (config.datasets && config.datasets[0] && config.datasets[0].colors) || colors;
            delete options.xaxis;
            delete options.stroke;
            options.legend.show = true;
        }

        const chart = new ApexCharts(element, options);
        charts[elementId] = chart;
        chart.render();
    }

    return { render: render, destroy: destroy };
})();
