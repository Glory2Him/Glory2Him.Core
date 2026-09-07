import { act, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import OfflineBanner from './offlineBanner';

describe('OfflineBanner', () => {
    const realOnLine = Object.getOwnPropertyDescriptor(navigator, 'onLine');

    const setOnLine = (value: boolean) =>
        Object.defineProperty(navigator, 'onLine', { configurable: true, value });

    afterEach(() => {
        if (realOnLine) {
            Object.defineProperty(navigator, 'onLine', realOnLine);
        }
    });

    it('should render nothing while online', () => {
        // given
        setOnLine(true);

        // when
        render(<OfflineBanner />);

        // then
        expect(screen.queryByRole('status')).not.toBeInTheDocument();
    });

    it('should show a clear offline state once the browser goes offline', () => {
        // given
        setOnLine(true);
        render(<OfflineBanner />);

        // when
        act(() => window.dispatchEvent(new Event('offline')));

        // then
        expect(screen.getByRole('status')).toHaveTextContent(/offline/i);
    });

    // Regression guard: an in-flow banner placed before the header would already be scrolled
    // out of view by the time useStickyHeader.ts fixes the header to the viewport top.
    it('should stay pinned to the viewport rather than flow with the page', () => {
        // given
        setOnLine(false);

        // when
        render(<OfflineBanner />);

        // then
        expect(screen.getByRole('status')).toHaveClass('sticky-top');
    });

    // Regression guard: sticky-top's own default z-index (1020) ties with the header's, and
    // equal z-index paints in DOM order — the banner is earlier in the DOM, so it would lose.
    it('should paint above the sticky header rather than being tied with it', () => {
        // given
        setOnLine(false);

        // when
        render(<OfflineBanner />);

        // then
        expect(screen.getByRole('status')).toHaveStyle({ zIndex: 1030 });
    });
});
