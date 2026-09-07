import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { NETWORK_REACHABLE_EVENT, NETWORK_UNREACHABLE_EVENT } from '../brokers/apiBroker';
import { useOnlineStatus } from './useOnlineStatus';

describe('useOnlineStatus', () => {
    const realOnLine = Object.getOwnPropertyDescriptor(navigator, 'onLine');

    const setOnLine = (value: boolean) =>
        Object.defineProperty(navigator, 'onLine', { configurable: true, value });

    beforeEach(() => setOnLine(true));

    afterEach(() => {
        if (realOnLine) {
            Object.defineProperty(navigator, 'onLine', realOnLine);
        }
    });

    it('should seed from navigator.onLine', () => {
        // given
        setOnLine(false);

        // when
        const { result } = renderHook(() => useOnlineStatus());

        // then
        expect(result.current).toBe(false);
    });

    it('should flip to offline when the browser fires the offline event', () => {
        // given
        const { result } = renderHook(() => useOnlineStatus());
        expect(result.current).toBe(true);

        // when
        act(() => window.dispatchEvent(new Event('offline')));

        // then
        expect(result.current).toBe(false);
    });

    it('should flip back to online when the browser fires the online event', () => {
        // given
        setOnLine(false);
        const { result } = renderHook(() => useOnlineStatus());
        act(() => window.dispatchEvent(new Event('offline')));

        // when
        act(() => window.dispatchEvent(new Event('online')));

        // then
        expect(result.current).toBe(true);
    });

    // The whole reason apiBroker.ts raises these: navigator.onLine can stay stuck at `true`
    // through a captive portal or a downed API host, so the hook has to react to real request
    // outcomes too, not just the browser's own online/offline events.
    it('should flip to offline when apiBroker reports a request found no network', () => {
        // given
        const { result } = renderHook(() => useOnlineStatus());
        expect(result.current).toBe(true);

        // when
        act(() => window.dispatchEvent(new Event(NETWORK_UNREACHABLE_EVENT)));

        // then
        expect(result.current).toBe(false);
    });

    it('should flip back to online when apiBroker reports a request reached the server', () => {
        // given
        setOnLine(false);
        const { result } = renderHook(() => useOnlineStatus());
        act(() => window.dispatchEvent(new Event(NETWORK_UNREACHABLE_EVENT)));

        // when
        act(() => window.dispatchEvent(new Event(NETWORK_REACHABLE_EVENT)));

        // then
        expect(result.current).toBe(true);
    });
});
