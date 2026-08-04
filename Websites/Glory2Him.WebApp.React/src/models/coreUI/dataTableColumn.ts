import { ReactNode } from 'react';

export interface DataTableColumn<TItem> {
    title: string;
    value: (item: TItem) => string | number | boolean | Date | null | undefined;
    sortable?: boolean;
    cellTemplate?: (item: TItem) => ReactNode;
}
