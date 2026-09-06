import { renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useApprovalRoundChanges } from './useApprovalRoundChanges';

// THE FRESHNESS CHANNEL, in isolation: this hook takes only an id, a callback and an interval,
// so its whole contract — poll while visible, refresh at once on refocus and on reconnect, do
// nothing while there is no round to watch — is provable without a query client, a broker or a
// rendered panel.
const setVisibility = (state: DocumentVisibilityState) => {
    Object.defineProperty(document, 'visibilityState', {
        configurable: true,
        get: () => state
    });
};

describe('useApprovalRoundChanges', () => {
    beforeEach(() => {
        vi.useFakeTimers();
        setVisibility('visible');
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('polls refresh on the interval while the tab is visible', () => {
        const refresh = vi.fn();
        renderHook(() => useApprovalRoundChanges('content-item-1', refresh, 1000));

        expect(refresh).not.toHaveBeenCalled();
        vi.advanceTimersByTime(1000);
        expect(refresh).toHaveBeenCalledTimes(1);
        vi.advanceTimersByTime(2000);
        expect(refresh).toHaveBeenCalledTimes(3);
    });

    it('does not poll while the tab is hidden', () => {
        setVisibility('hidden');
        const refresh = vi.fn();
        renderHook(() => useApprovalRoundChanges('content-item-1', refresh, 1000));

        vi.advanceTimersByTime(5000);
        expect(refresh).not.toHaveBeenCalled();
    });

    it('refreshes immediately when the tab becomes visible again', () => {
        setVisibility('hidden');
        const refresh = vi.fn();
        renderHook(() => useApprovalRoundChanges('content-item-1', refresh, 1000));

        setVisibility('visible');
        document.dispatchEvent(new Event('visibilitychange'));
        expect(refresh).toHaveBeenCalledTimes(1);
    });

    it('refreshes on reconnect — a missed message must not leave a stale panel', () => {
        const refresh = vi.fn();
        renderHook(() => useApprovalRoundChanges('content-item-1', refresh, 1000));

        window.dispatchEvent(new Event('online'));
        expect(refresh).toHaveBeenCalledTimes(1);
    });

    it('does nothing when there is no entity to watch', () => {
        const refresh = vi.fn();
        renderHook(() => useApprovalRoundChanges('', refresh, 1000));

        vi.advanceTimersByTime(5000);
        window.dispatchEvent(new Event('online'));
        expect(refresh).not.toHaveBeenCalled();
    });

    it('stops polling and listening once unmounted', () => {
        const refresh = vi.fn();
        const { unmount } = renderHook(
            () => useApprovalRoundChanges('content-item-1', refresh, 1000));

        unmount();
        vi.advanceTimersByTime(5000);
        window.dispatchEvent(new Event('online'));
        expect(refresh).not.toHaveBeenCalled();
    });

    it('always calls the latest refresh, not the one from first render', () => {
        const firstRefresh = vi.fn();
        const secondRefresh = vi.fn();

        const { rerender } = renderHook(
            ({ refresh }) => useApprovalRoundChanges('content-item-1', refresh, 1000),
            { initialProps: { refresh: firstRefresh } });

        rerender({ refresh: secondRefresh });
        vi.advanceTimersByTime(1000);

        expect(firstRefresh).not.toHaveBeenCalled();
        expect(secondRefresh).toHaveBeenCalledTimes(1);
    });
});
