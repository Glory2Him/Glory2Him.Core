import { useEffect, useState } from 'react';
import { Spinner } from '../coreUI/spinner';
import { useInfiniteScrollSentinel } from '../../hooks/useInfiniteScrollSentinel';
import { ReviewCommentEditPanel } from './reviewCommentEditPanel';
import { ReviewCommentViewPanel } from './reviewCommentViewPanel';

import {
    ReviewCommentEvents,
    ReviewCommentItem
} from '../../models/components/approvals/reviewCommentItem';

// EVERY ROW OF THE THREAD, one template each, scrolled rather than paged. A pure presentation
// component — the collection arrives ACCUMULATED and already ORDERED by ReviewCommentPanel (this
// one appends nothing and sorts nothing), and its whole contribution to paging is noticing that
// its foot came into view and saying so.
//
// The sentinel and the observer come from useInfiniteScrollSentinel, shared with
// ContentItemResultsPanel: infinite scroll is one behaviour, and it now has one implementation
// rather than two copies that could drift.
//
// WHICH ROW IS OPEN is local state HERE, keyed on the row id. Which face a row is showing is
// nothing the consumer persists — the same call ContentItemPanel makes about its editor — and
// keeping it at this level is what lets Cancel restore the row verbatim: the view template is
// re-rendered from the untouched item, so there is nothing to put back.
export interface ReviewCommentResultsPanelProps extends ReviewCommentEvents {
    reviewCommentCollection?: ReadonlyArray<ReviewCommentItem>;

    // The per-row gates, decided ONCE by ReviewCommentPanel and asked per item here. Passed as
    // predicates rather than as booleans because they are row-dependent — ownership is a
    // property of the row, not of the surface — and passed at all rather than re-derived so
    // there is exactly one place the ownership and tier rules live.
    mayAmend?: (item: ReviewCommentItem) => boolean;
    mayResolve?: (item: ReviewCommentItem) => boolean;

    // The FIRST page. While it is on the list is replaced by a spinner rather than emptied, so a
    // re-read does not flash "nothing said yet" on its way to the thread.
    isLoading?: boolean;

    // A further page, on its way. Renders beneath the rows and holds the sentinel back, so one
    // scroll is one fetch.
    isLoadingMore?: boolean;

    hasMore?: boolean;
    onLoadMore?: () => void;

    isSubmitting?: boolean;

    loadingText?: string;
    loadingMoreText?: string;
    loadMoreButtonText?: string;

    // Required: the parent owns the wording. See the destructuring below.
    emptyText: string;
}

export function ReviewCommentResultsPanel({
    reviewCommentCollection = [],
    mayAmend = () => false,
    mayResolve = () => false,
    isLoading = false,
    isLoadingMore = false,
    hasMore = false,
    onLoadMore,
    isSubmitting = false,
    loadingText = 'Loading…',
    loadingMoreText = 'Loading more…',
    loadMoreButtonText = 'Load more',
    // NO DEFAULT, deliberately: ReviewCommentPanel always forwards its own, so a copy here would
    // be a second live-looking wording that can never render and would silently go stale the
    // moment the real one changed.
    emptyText,
    onModified,
    onRemoved,
    onRemoveRequested,
    onResolvedChanged
}: ReviewCommentResultsPanelProps) {
    const [editingCommentId, setEditingCommentId] = useState<string | null>(null);

    const { sentinelRef, supportsAutoLoad } = useInfiniteScrollSentinel(
        hasMore, isLoadingMore, onLoadMore, reviewCommentCollection.length);

    // A ROW THAT LEAVES THE COLLECTION CLOSES ITS EDITOR. Without this, withdrawing the comment
    // being edited would leave the panel holding an id nothing matches — harmless today, and a
    // stale editor waiting to reopen over whatever row later took that id.
    useEffect(() => {
        if (editingCommentId == null) {
            return;
        }

        const stillPresent = reviewCommentCollection.some(
            (reviewComment) => reviewComment.id === editingCommentId);

        if (stillPresent === false) {
            setEditingCommentId(null);
        }
    }, [reviewCommentCollection, editingCommentId]);

    // The paging foot, rendered by BOTH branches below — an empty collection can still be waiting
    // on its first page, so the sentinel must not be a reward for already having rows.
    const renderPagingFoot = () => (
        <>
            {/* A pixel tall rather than nothing at all: an IntersectionObserver over a zero-area
                target is unreliable — engines disagree on whether an empty intersection rectangle
                counts — and the failure is a thread that quietly stops loading. */}
            {hasMore && (
                <div
                    ref={sentinelRef}
                    className="g2h-review-comment-sentinel"
                    aria-hidden="true"></div>
            )}

            {isLoadingMore && (
                <div className="text-center py-3" role="status">
                    <Spinner />
                    <p className="mt-2 mb-0">{loadingMoreText}</p>
                </div>
            )}

            {/* The way out of a dead end. Without IntersectionObserver nothing would ever ask for
                the next page, and the thread would simply stop with no explanation. */}
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

    if (isLoading) {
        return (
            <div className="text-center py-4">
                <Spinner />
                <p className="mt-2 mb-0">{loadingText}</p>
            </div>
        );
    }

    // THE SENTINEL SURVIVES AN EMPTY PAGE. Returning the empty state alone stranded a consumer
    // holding hasMore over a collection that has not filled yet — a first page that came back
    // empty, or a hasMore set from a server total before any row landed — with neither a sentinel
    // to trip nor a button to press, so the thread said "nothing here" and could never load.
    if (reviewCommentCollection.length === 0) {
        return (
            <>
                <p className="small text-body-secondary mb-0" role="status">{emptyText}</p>
                {renderPagingFoot()}
            </>
        );
    }

    return (
        <>
            {reviewCommentCollection.map((reviewComment) =>
                editingCommentId === reviewComment.id ? (
                    <ReviewCommentEditPanel
                        key={reviewComment.id}
                        reviewComment={reviewComment}
                        isSubmitting={isSubmitting}
                        onModified={(modified) => {
                            // A committed save CLOSES the editor the way Cancel does. What the row
                            // then shows is the CONSUMER's item: the page persists and re-reads, so
                            // the amendment appears; a page that has not re-read yet honestly shows
                            // the row it still holds.
                            setEditingCommentId(null);
                            onModified?.(modified);
                        }}
                        onCancelled={() => setEditingCommentId(null)} />
                ) : (
                    <ReviewCommentViewPanel
                        key={reviewComment.id}
                        reviewComment={reviewComment}
                        showsAmendActions={mayAmend(reviewComment)}
                        showsResolveControl={mayResolve(reviewComment)}
                        isSubmitting={isSubmitting}
                        onEditClick={() => setEditingCommentId(reviewComment.id)}
                        onRemoveRequested={onRemoveRequested}
                        onRemoved={onRemoved}
                        onResolvedChanged={onResolvedChanged} />
                ))}

            {renderPagingFoot()}
        </>
    );
}
