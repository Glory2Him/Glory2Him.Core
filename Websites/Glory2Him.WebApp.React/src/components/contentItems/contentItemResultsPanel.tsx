import { useEffect, useRef } from 'react';
import { ContentItemPanel } from './contentItemPanel';
import { Spinner } from '../coreUI/spinner';

import {
    ContentItemEvents,
    ContentItemSectionToggles,
    ContentItemText
} from '../../models/components/contentItems/contentItemTemplate';

import {
    ContentItemReactionOption,
    ContentItemSearchItem
} from '../../models/components/contentItems/contentItemSearchItem';

import './contentItems.css';

// THE ORANGE BLOCK: every result that matched, one ContentItemPanel each, scrolled rather
// than paged. A pure presentation component — the collection arrives ACCUMULATED (react-query
// keeps the pages; this panel appends nothing of its own), and the panel's whole contribution
// to paging is noticing that its foot came into view and saying so.
// The six SECTION SWITCHES thread through too (ContentItemSectionToggles) — like
// showModerationSection and showApprovalStatusRibbon they are per-SURFACE decisions a page makes
// once for every card, and ContentItemPanel owns what each means. The form-face props
// (showEditSection, onModified, validationIssues…) are deliberately NOT here: they
// carry ONE item's write lifecycle, which no single list-level value can express — a
// list row's edit is a navigation (onEditClick).
export interface ContentItemResultsPanelProps
    extends ContentItemEvents, ContentItemText, ContentItemSectionToggles {
    // The accumulated results as they stand — each element SELF-CONTAINED, carrying its item
    // and its winning setting, so a card consults nothing beyond its own element.
    contentItemCollection?: ReadonlyArray<ContentItemSearchItem>;

    // The reaction choices behind every card's Like control.
    reactionOptions?: ReadonlyArray<ContentItemReactionOption>;

    // Threaded to every card — see ContentItemPanel, which owns what it means.
    showModerationSection?: boolean;

    // ON BY DEFAULT here, off by default on ContentItemPanel: a listed card's title is the
    // way into the detail surface, while a panel standing alone already is that surface.
    allowTitleClick?: boolean;

    showApprovalStatusRibbon?: boolean;
    showApprovalStatus?: boolean;
    showContentExpanded?: boolean;
    truncateAt?: number;
    allowInPlaceExpansion?: boolean;

    // The FIRST page. While it is on, the list is replaced by a spinner rather than being
    // emptied, so a re-search does not flash "nothing found" on its way to results.
    isLoading?: boolean;

    // A further page, on its way. Renders beneath the results and holds the sentinel back, so
    // one scroll is one fetch.
    isLoadingMore?: boolean;

    // Whether anything is left — the consumer knows from its own paging: the OData reads answer
    // with a plain array and no total, so a page asks for one row beyond the page and drops it.
    hasMore?: boolean;

    // Raised when the foot of the list comes into view, and by the fallback button where
    // IntersectionObserver is unavailable. Never raised while isLoadingMore is on.
    onLoadMore?: () => void;

    loadingText?: string;
    loadingMoreText?: string;
    loadMoreButtonText?: string;
    emptyText?: string;
}

export function ContentItemResultsPanel({
    contentItemCollection = [],
    reactionOptions = [],
    showModerationSection = false,
    allowTitleClick = true,
    showApprovalStatusRibbon = false,
    showApprovalStatus = false,
    isLoading = false,
    isLoadingMore = false,
    hasMore = false,
    onLoadMore,
    loadingText = 'Loading…',
    loadingMoreText = 'Loading more…',
    loadMoreButtonText = 'Load more',
    emptyText = 'Nothing matched that search.',
    ...itemEventsAndText
}: ContentItemResultsPanelProps) {
    const sentinelRef = useRef<HTMLDivElement | null>(null);

    // Held in a ref so the observer below depends only on the paging state. Without it, a
    // consumer passing an inline arrow — the natural thing — would tear the observer down and
    // rebuild it on every render.
    const onLoadMoreRef = useRef(onLoadMore);

    useEffect(() => {
        onLoadMoreRef.current = onLoadMore;
    });

    // Progressive enhancement, read at render rather than at module load so a test (and a
    // browser without it) takes the same path the fallback button is rendered for.
    const supportsAutoLoad = typeof IntersectionObserver === 'function';

    // DEPENDS ON isLoadingMore ON PURPOSE. The observer is torn down while a page is in flight
    // and rebuilt when it lands, and observing fires an immediate callback — so a sentinel still
    // on screen after the new rows arrive asks for the next page. Reading the flag inside the
    // callback instead would stall the list: the sentinel never moves, so nothing would fire
    // again to un-stick it.
    useEffect(() => {
        const sentinel = sentinelRef.current;

        if (sentinel == null || hasMore === false || isLoadingMore || supportsAutoLoad === false) {
            return;
        }

        const observer = new IntersectionObserver(
            (entries) => {
                if (entries.some((entry) => entry.isIntersecting)) {
                    onLoadMoreRef.current?.();
                }
            },
            // Asks a screen early, so the next page is usually there before the reader arrives.
            { rootMargin: '200px 0px' });

        observer.observe(sentinel);

        return () => observer.disconnect();
    }, [hasMore, isLoadingMore, supportsAutoLoad, contentItemCollection.length]);

    if (isLoading) {
        return (
            <div className="text-center py-5">
                <Spinner />
                <p className="mt-2 mb-0">{loadingText}</p>
            </div>
        );
    }

    if (contentItemCollection.length === 0) {
        return <div className="alert alert-info mb-0" role="status">{emptyText}</div>;
    }

    return (
        <>
            {contentItemCollection.map((contentItem) => (
                <ContentItemPanel
                    key={contentItem.id}
                    contentItem={contentItem}
                    reactionOptions={reactionOptions}
                    showModerationSection={showModerationSection}
                    allowTitleClick={allowTitleClick}
                    showApprovalStatusRibbon={showApprovalStatusRibbon}
                    showApprovalStatus={showApprovalStatus}
                    {...itemEventsAndText} />
            ))}

            {/* A pixel tall rather than nothing at all: an IntersectionObserver over a zero-area
                target is unreliable — engines disagree on whether an empty intersection
                rectangle counts — and the failure is a list that quietly stops loading. */}
            {hasMore && (
                <div
                    ref={sentinelRef}
                    className="g2h-content-item-sentinel"
                    aria-hidden="true"></div>
            )}

            {isLoadingMore && (
                <div className="text-center py-3" role="status">
                    <Spinner />
                    <p className="mt-2 mb-0">{loadingMoreText}</p>
                </div>
            )}

            {/* The way out of a dead end. Without IntersectionObserver nothing would ever ask
                for the next page, and the list would simply stop with no explanation. */}
            {hasMore && isLoadingMore === false && supportsAutoLoad === false && (
                <div className="text-center">
                    <button
                        type="button"
                        className="btn btn-outline-primary mb-0"
                        onClick={() => onLoadMore?.()}>
                        {loadMoreButtonText}
                    </button>
                </div>
            )}
        </>
    );
}
