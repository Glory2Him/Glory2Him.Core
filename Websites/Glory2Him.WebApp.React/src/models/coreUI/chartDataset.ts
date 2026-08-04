export interface ChartDataset {
    label: string;
    data: ReadonlyArray<number>;

    // Series colour(s). For bar/donut charts more than one colour may be supplied,
    // one per data point.
    colors?: ReadonlyArray<string>;

    // Renders the line as a dashed stroke.
    dashed?: boolean;

    // Fills the area under a line (line charts only).
    fill?: boolean;
}
