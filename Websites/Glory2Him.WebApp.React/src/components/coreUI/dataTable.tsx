import { ReactNode, useMemo, useState } from 'react';
import { DataTableColumn } from '../../models/coreUI/dataTableColumn';

// Generic sortable/searchable/paged table with the same behavior as the Blazor DataTable:
// case-insensitive substring search across every column's value, click-to-sort headers,
// and simple previous/next paging.
export interface DataTableProps<TItem> {
    items?: ReadonlyArray<TItem>;
    columns?: ReadonlyArray<DataTableColumn<TItem>>;
    searchable?: boolean;
    pageSize?: number;
    rowActions?: (item: TItem) => ReactNode;
    rowClass?: (item: TItem) => string | undefined;
    extraFilters?: ReactNode;
}

function compareValues(left: unknown, right: unknown): number {
    if (left == null && right == null) {
        return 0;
    }

    if (left == null) {
        return -1;
    }

    if (right == null) {
        return 1;
    }

    if (typeof left === 'number' && typeof right === 'number') {
        return left - right;
    }

    if (left instanceof Date && right instanceof Date) {
        return left.getTime() - right.getTime();
    }

    return String(left).localeCompare(String(right));
}

export function DataTable<TItem>({
    items = [],
    columns = [],
    searchable = true,
    pageSize = 10,
    rowActions,
    rowClass,
    extraFilters,
}: DataTableProps<TItem>) {
    const [searchTerm, setSearchTerm] = useState('');
    const [sortColumnIndex, setSortColumnIndex] = useState<number | null>(null);
    const [sortAscending, setSortAscending] = useState(true);
    const [currentPage, setCurrentPage] = useState(1);

    const filteredItems = useMemo(() => {
        if (searchTerm.trim().length === 0) {
            return [...items];
        }

        const term = searchTerm.toLowerCase();

        return items.filter((item) =>
            columns.some((column) =>
                String(column.value(item) ?? '').toLowerCase().includes(term)));
    }, [items, columns, searchTerm]);

    const sortedItems = useMemo(() => {
        if (sortColumnIndex == null || columns[sortColumnIndex] == null) {
            return filteredItems;
        }

        const sortColumn = columns[sortColumnIndex];
        const direction = sortAscending ? 1 : -1;

        return [...filteredItems].sort(
            (left, right) => direction * compareValues(sortColumn.value(left), sortColumn.value(right)));
    }, [filteredItems, columns, sortColumnIndex, sortAscending]);

    const pageCount = Math.max(1, Math.ceil(filteredItems.length / pageSize));
    const clampedPage = Math.min(currentPage, pageCount);
    const pagedItems = sortedItems.slice((clampedPage - 1) * pageSize, clampedPage * pageSize);

    const toggleSort = (columnIndex: number) => {
        if (sortColumnIndex === columnIndex) {
            setSortAscending(!sortAscending);
        } else {
            setSortColumnIndex(columnIndex);
            setSortAscending(true);
        }

        setCurrentPage(1);
    };

    return (
        <div className="datatable">
            {(searchable || extraFilters != null) && (
                <div className="d-flex gap-2 mb-3">
                    {searchable && (
                        <input
                            type="search"
                            className="form-control datatable-search"
                            placeholder="Search..."
                            value={searchTerm}
                            onChange={(event) => {
                                setSearchTerm(event.target.value);
                                setCurrentPage(1);
                            }} />
                    )}
                    {extraFilters}
                </div>
            )}

            <table className="table table-hover align-middle">
                <thead>
                    <tr>
                        {columns.map((column, columnIndex) =>
                            column.sortable !== false ? (
                                <th
                                    key={column.title}
                                    role="button"
                                    className="user-select-none"
                                    onClick={() => toggleSort(columnIndex)}>
                                    {column.title}
                                    {sortColumnIndex === columnIndex && (
                                        <i className={`ms-1 bi ${sortAscending ? 'bi-arrow-up' : 'bi-arrow-down'}`}></i>
                                    )}
                                </th>
                            ) : (
                                <th key={column.title}>{column.title}</th>
                            ))}
                        {rowActions != null && <th className="text-end"></th>}
                    </tr>
                </thead>
                <tbody>
                    {pagedItems.map((item, rowIndex) => (
                        <tr key={rowIndex} className={rowClass?.(item)}>
                            {columns.map((column) => (
                                <td key={column.title}>
                                    {column.cellTemplate != null
                                        ? column.cellTemplate(item)
                                        : String(column.value(item) ?? '')}
                                </td>
                            ))}
                            {rowActions != null && (
                                <td className="text-end">{rowActions(item)}</td>
                            )}
                        </tr>
                    ))}
                </tbody>
            </table>

            {pageCount > 1 && (
                <nav className="d-flex justify-content-between align-items-center datatable-pager">
                    <button
                        type="button"
                        className="btn btn-sm btn-outline-secondary datatable-prev"
                        disabled={clampedPage <= 1}
                        onClick={() => setCurrentPage(Math.max(1, clampedPage - 1))}>
                        Previous
                    </button>

                    <span className="small">Page {clampedPage} of {pageCount}</span>

                    <button
                        type="button"
                        className="btn btn-sm btn-outline-secondary datatable-next"
                        disabled={clampedPage >= pageCount}
                        onClick={() => setCurrentPage(Math.min(pageCount, clampedPage + 1))}>
                        Next
                    </button>
                </nav>
            )}
        </div>
    );
}
