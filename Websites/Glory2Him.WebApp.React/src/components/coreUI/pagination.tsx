import { PaginationVariant } from '../../models/coreUI/paginationVariant';

// Pagination in the styles the Blogzine template ships (pagination-styles.html): the default
// numbered strip, a rounded-pill variant, and a plain previous/next pair. The parent owns the
// current page and decides what a page change means.
export interface PaginationProps {
    currentPage?: number;
    totalPages?: number;
    onPageChange?: (page: number) => void;
    variant?: PaginationVariant;
    alignment?: boolean;
    ariaLabel?: string;
}

export function Pagination({
    currentPage = 1,
    totalPages = 1,
    onPageChange,
    variant = 'Numbered',
    alignment = true,
    ariaLabel = 'Page navigation',
}: PaginationProps) {
    // The rounded variant is the same control with pill-shaped links; "PrevNext" drops the
    // numbers entirely and spells the direction out.
    const variantCssClass = variant === 'Rounded' ? 'pagination-rounded' : '';
    const alignmentCssClass = alignment ? 'justify-content-center' : '';
    const showNumbers = variant !== 'PrevNext';
    const showLabels = variant === 'PrevNext';

    const pageNumbers = Array.from(
        { length: Math.max(totalPages, 1) },
        (_, index) => index + 1);

    const goTo = (page: number) => {
        if (page < 1 || page > totalPages || page === currentPage) {
            return;
        }

        onPageChange?.(page);
    };

    return (
        <nav aria-label={ariaLabel}>
            <ul className={`pagination ${variantCssClass} mb-0 ${alignmentCssClass}`}>
                <li className={`page-item ${currentPage <= 1 ? 'disabled' : ''}`}>
                    <button
                        type="button"
                        className="page-link"
                        onClick={() => goTo(currentPage - 1)}
                        disabled={currentPage <= 1}>
                        {showLabels ? 'Prev' : <i className="bi bi-chevron-left"></i>}
                    </button>
                </li>

                {showNumbers && pageNumbers.map((pageNumber) => (
                    <li key={pageNumber} className={`page-item ${pageNumber === currentPage ? 'active' : ''}`}>
                        <button type="button" className="page-link" onClick={() => goTo(pageNumber)}>
                            {pageNumber}
                        </button>
                    </li>
                ))}

                <li className={`page-item ${currentPage >= totalPages ? 'disabled' : ''}`}>
                    <button
                        type="button"
                        className="page-link"
                        onClick={() => goTo(currentPage + 1)}
                        disabled={currentPage >= totalPages}>
                        {showLabels ? 'Next' : <i className="bi bi-chevron-right"></i>}
                    </button>
                </li>
            </ul>
        </nav>
    );
}
