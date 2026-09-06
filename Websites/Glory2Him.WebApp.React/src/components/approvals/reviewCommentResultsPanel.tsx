import { useEffect, useRef, useState } from 'react';
import { Spinner } from '../coreUI/spinner';
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
// The sentinel, the fallback button and the observer's dependency list are
// ContentItemResultsPanel's, deliberately: infinite scroll is one behaviour and a second
// implementation of it is a second thing to get wrong.
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
    emptyText?: string;
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
    emptyText = 'Nothing has been said about this submission yet.',
    onModified,
    onRemoved,
    onRemoveRequested,
    onResolvedChanged
}: ReviewCommentResultsPanelProps) {
    const [editingCommentId, setEditingCommentId] = useState<string | null>(null);
    const sentinelRef = useRef<HTMLDivElement | null>(null);

    // Held in a ref so the observer below depends only on the paging state. Without it a consumer
    // passing an inline arrow — the natural thing — would tear the observer down and rebuild it
    // on every render.
    const onLoadMoreRef = useRef(onLoadMore);

    useEffect(() => {
        onLoadMoreRef.current = onLoadMore;
    });

    // Progressive enhancement, read at render rather than at module load so a test (and a browser
    // without it) takes the same path the fallback button is rendered for.
    const supportsAutoLoad = typeof IntersectionObserver === 'function';

    // DEPENDS ON isLoadingMore ON PURPOSE. The observer is torn down while a page is in flight and
    // rebuilt when it lands, and observing fires an immediate callback — so a sentinel still on
    // screen after the new rows arrive asks for the next page. Reading the flag inside the
    // callback instead would stall the list.
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
    }, [hasMore, isLoadingMore, supportsAutoLoad, reviewCommentCollection.length]);

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

    if (isLoading) {
        return (
            <div className="text-center py-4">
                <Spinner />
                <p className="mt-2 mb-0">{loadingText}</p>
            </div>
        );
    }

    if (reviewCommentCollection.length === 0) {
        return <p className="small text-body-secondary mb-0" role="status">{emptyText}</p>;
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
}
