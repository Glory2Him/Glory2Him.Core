import { useEffect, useRef } from 'react';
import { ChartDataset } from '../../models/coreUI/chartDataset';

// CoreUI Chart, rendered straight onto the globally loaded ApexCharts bundle (the charting
// library Blogzine ships). The option-building mirrors wwwroot/assets/js/glory2himCharts.js
// so the output stays pixel-identical to the Blazor component.
export interface ChartProps {
    chartType?: string;
    labels?: ReadonlyArray<string>;
    datasets?: ReadonlyArray<ChartDataset>;
    height?: number;
}

function buildOptions(
    chartType: string,
    labels: ReadonlyArray<string>,
    datasets: ReadonlyArray<ChartDataset>,
    height: number): Record<string, unknown> {
    const series = datasets.map((dataset) => ({ name: dataset.label, data: [...dataset.data] }));

    const colors = datasets.map((dataset) =>
        dataset.colors != null && dataset.colors.length > 0 ? dataset.colors[0] : '#2163e8');

    const options: Record<string, unknown> = {
        chart: {
            type: chartType,
            height,
            toolbar: { show: false },
            fontFamily: 'inherit',
        },
        series,
        colors,
        labels: [...labels],
        xaxis: {
            categories: [...labels],
            axisBorder: { show: false },
        },
        stroke: {
            curve: 'smooth',
            width: 2,
            dashArray: datasets.map((dataset) => (dataset.dashed === true ? 5 : 0)),
        },
        dataLabels: { enabled: false },
        legend: {
            show: series.length > 1,
            position: 'top',
            horizontalAlign: 'right',
            markers: { width: 8, height: 8 },
        },
        grid: { borderColor: 'rgba(0,0,0,0.08)' },
    };

    // Donut/pie take a flat numeric series and use `labels` for the slice names.
    if (chartType === 'donut' || chartType === 'pie') {
        options.series = series.length > 0 ? series[0].data : [];
        options.colors =
            datasets[0]?.colors != null && datasets[0].colors.length > 0
                ? [...datasets[0].colors]
                : colors;

        delete options.xaxis;
        delete options.stroke;
        options.legend = { ...(options.legend as Record<string, unknown>), show: true };
    }

    return options;
}

export function Chart({ chartType = 'line', labels = [], datasets = [], height = 300 }: ChartProps) {
    const elementRef = useRef<HTMLDivElement>(null);

    // The signature only changes when the data does, so re-renders (e.g. a dashboard refresh)
    // do not rebuild the chart and cause flicker.
    const signature =
        chartType + '|' + labels.join(',') + '|' +
        datasets.map((dataset) => dataset.label + ':' + dataset.data.join(',')).join(';');

    useEffect(() => {
        const element = elementRef.current;
        const ApexCharts = window.ApexCharts;

        if (element == null || ApexCharts == null) {
            return;
        }

        const chart = new ApexCharts(element, buildOptions(chartType, labels, datasets, height));
        void chart.render();

        return () => chart.destroy();
        // The primitive signature stands in for the array/object props, which get new
        // identities on every parent render.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [signature, height]);

    return (
        <div className="chart-wrapper" style={{ height: `${height}px` }}>
            <div ref={elementRef}></div>
        </div>
    );
}
