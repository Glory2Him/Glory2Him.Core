import { useCallback, useEffect, useRef, useState } from 'react';

// INFINITE SCROLL, ONCE. Two results panels — the content item feed and the review thread —
// scroll rather than page, and both had their own copy of the sentinel, the observer, the ref
// dance and the no-observer fallback. Sixty lines twice, with the second copy's own comment
// claiming the duplication existed so there would not be "a second implementation of it".
//
// WHAT THE CALLER STILL OWNS is the sentinel ELEMENT: a panel decides where its foot is and what
// it looks like. This owns only when to watch it and who to tell.
export interface InfiniteScrollSentinel {
    // Attach to a one-pixel element rendered at the foot of the list whenever hasMore is true.
    //
    // A CALLBACK REF, not a ref object, and the difference is load-bearing. A ref object mutates
    // silently: a panel that renders its foot inside a branch — under the empty state, or after a
    // spinner comes down — mounts the node without changing any of the paging values below, so
    // the effect never re-runs and the observer is never attached to it. React calls a callback
    // ref as the node arrives and again with null as it leaves, which is exactly the signal the
    // effect needs.
    sentinelRef: (node: HTMLDivElement | null) => void;

    // Whether IntersectionObserver exists at all. A caller renders its fallback button on false —
    // without one, nothing would ever ask for the next page and the list would simply stop.
    supportsAutoLoad: boolean;
}

export const useInfiniteScrollSentinel = (
    hasMore: boolean,
    isLoadingMore: boolean,
    onLoadMore: (() => void) | undefined,

    // Re-arms the observer as rows arrive. Passed in rather than derived because only the caller
    // knows what "the list changed" means for its own collection.
    itemCount: number
): InfiniteScrollSentinel => {
    // State rather than a ref, so the node's arrival re-renders and re-runs the effect below.
    const [sentinel, setSentinel] = useState<HTMLDivElement | null>(null);

    const sentinelRef = useCallback(
        (node: HTMLDivElement | null) => setSentinel(node),
        []);

    // Held in a ref so the effect below depends only on the paging state. Without it a consumer
    // passing an inline arrow — the natural thing — would tear the observer down and rebuild it
    // on every render.
    const onLoadMoreRef = useRef(onLoadMore);

    useEffect(() => {
        onLoadMoreRef.current = onLoadMore;
    });

    // Read at render rather than at module load, so a test (and a browser without it) takes the
    // same path the fallback button is rendered for.
    const supportsAutoLoad = typeof IntersectionObserver === 'function';

    // DEPENDS ON isLoadingMore ON PURPOSE. The observer is torn down while a page is in flight and
    // rebuilt when it lands, and observing fires an immediate callback — so a sentinel still on
    // screen after the new rows arrive asks for the next page. Reading the flag inside the
    // callback instead would stall the list: the sentinel never moves, so nothing would fire
    // again to un-stick it.
    useEffect(() => {
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
    }, [sentinel, hasMore, isLoadingMore, supportsAutoLoad, itemCount]);

    return { sentinelRef, supportsAutoLoad };
};
