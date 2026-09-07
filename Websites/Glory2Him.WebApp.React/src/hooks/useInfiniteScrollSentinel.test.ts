import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useInfiniteScrollSentinel } from './useInfiniteScrollSentinel';

// THE SHARED SENTINEL, tested where it lives. Two results panels depend on it, and both express
// the same thing through their own markup — so a fault here reads as a paging bug in one panel
// and as nothing at all in the other, depending on which one a test happens to render.
//
// The case that matters most is a sentinel that mounts LATE: a panel renders a spinner first and
// its foot afterwards, and none of the paging values change in between. Watching by ref object
// misses that node entirely, which is why the hook hands back a callback ref.
const observedNodes: Array<Element> = [];
let disconnectCount = 0;
let intersect: () => void = () => { };

class FakeIntersectionObserver {
    constructor(callback: IntersectionObserverCallback) {
        intersect = () => callback(
            [{ isIntersecting: true } as IntersectionObserverEntry],
            this as unknown as IntersectionObserver);
    }

    observe(node: Element): void {
        observedNodes.push(node);
    }

    disconnect(): void {
        disconnectCount += 1;
    }

    unobserve(): void { }

    takeRecords(): Array<IntersectionObserverEntry> {
        return [];
    }
}

describe('useInfiniteScrollSentinel', () => {
    const realObserver = globalThis.IntersectionObserver;

    beforeEach(() => {
        observedNodes.length = 0;
        disconnectCount = 0;
        globalThis.IntersectionObserver =
            FakeIntersectionObserver as unknown as typeof IntersectionObserver;
    });

    afterEach(() => {
        globalThis.IntersectionObserver = realObserver;
    });

    // THE REGRESSION. Nothing about the paging state changes when the node arrives — same
    // hasMore, same isLoadingMore, same count — so an effect keyed only on those never re-runs.
    // With a ref object the observer is built once, against null, and the reader scrolls to a
    // foot nobody is watching.
    it('should observe a sentinel that only mounts after the first render', () => {
        // given: rendered with no sentinel yet, exactly as a panel showing its spinner is
        const { result } = renderHook(
            () => useInfiniteScrollSentinel(true, false, vi.fn(), 0));

        expect(observedNodes).toHaveLength(0);

        // when: the branch carrying the foot finally renders
        const sentinel = document.createElement('div');
        act(() => result.current.sentinelRef(sentinel));

        // then
        expect(observedNodes).toContain(sentinel);
    });

    it('should ask for the next page when the sentinel comes into view', () => {
        // given
        const loadMore = vi.fn();

        const { result } = renderHook(
            () => useInfiniteScrollSentinel(true, false, loadMore, 0));

        act(() => result.current.sentinelRef(document.createElement('div')));

        // when
        act(() => intersect());

        // then
        expect(loadMore).toHaveBeenCalledOnce();
    });

    it('should stop watching when the sentinel leaves', () => {
        // given
        const { result } = renderHook(
            () => useInfiniteScrollSentinel(true, false, vi.fn(), 0));

        act(() => result.current.sentinelRef(document.createElement('div')));
        expect(disconnectCount).toBe(0);

        // when: React hands a callback ref null as the node unmounts
        act(() => result.current.sentinelRef(null));

        // then
        expect(disconnectCount).toBe(1);
    });

    // Watching while a page is in flight would fire immediately on a sentinel still on screen and
    // ask for the same page twice.
    it.each([
        ['there is no more to fetch', false, false],
        ['a page is already in flight', true, true]
    ])('should watch nothing while %s', (_case, hasMore, isLoadingMore) => {
        // given
        const { result } = renderHook(
            () => useInfiniteScrollSentinel(hasMore, isLoadingMore, vi.fn(), 0));

        // when
        act(() => result.current.sentinelRef(document.createElement('div')));

        // then
        expect(observedNodes).toHaveLength(0);
    });

    it('should report that auto-loading is unavailable without the API', () => {
        // given
        const observer = globalThis.IntersectionObserver;

        // @ts-expect-error — deliberately removing the API to take the fallback path
        delete globalThis.IntersectionObserver;

        try {
            // when
            const { result } = renderHook(
                () => useInfiniteScrollSentinel(true, false, vi.fn(), 0));

            act(() => result.current.sentinelRef(document.createElement('div')));

            // then: the caller renders its own button off this, or the list simply stops
            expect(result.current.supportsAutoLoad).toBe(false);
            expect(observedNodes).toHaveLength(0);
        } finally {
            globalThis.IntersectionObserver = observer;
        }
    });
});
