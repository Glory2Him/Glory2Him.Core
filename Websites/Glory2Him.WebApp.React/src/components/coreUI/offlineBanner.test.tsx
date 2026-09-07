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
});
